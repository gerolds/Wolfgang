using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Wolfgang
{
    public abstract class TransitionBase : ITransition, IObservable<ITransition>, IDisposable
    {
        protected TransitionBase()
        {
        }

        protected TransitionBase(in INet net)
        {
            _net = net;
        }

        private readonly HashSet<IObserver<ITransition>> _observers = new HashSet<IObserver<ITransition>>();
        private readonly INet _net;
        private long _tick;

        // Todo: these don't need to be thread-safe (no two threads will ever trigger the same transition)
        // need ordering to prevent deadlock from request behaviour 
        private List<IArc> _consumers = new List<IArc>();
        private List<IArc> _producers = new List<IArc>();

        private readonly Queue<Place.ITokenRequest> _consumerRequests =
            new Queue<Place.ITokenRequest>();

        private readonly Queue<Place.ITokenRequest> _producerRequests =
            new Queue<Place.ITokenRequest>();

        public string Description { get; set; }

        public IReadOnlyCollection<IArc> Consumers => _consumers;

        public IReadOnlyCollection<IArc> Producers => _producers;

        INet ITransition.Net { get; set; }

        public INet Net => _net;

        public int Period { get; set; } = 0;

        private void AddProducer(IArc arc)
            => AddToCopyAndSwap(ref _producers, arc);

        private void AddConsumer(IArc arc)
            => AddToCopyAndSwap(ref _consumers, arc);

        private void AddToCopyAndSwap<T>(ref List<T> source, T item)
        {
            // reference swap on write operations so readers don't need a lock

            List<T> initial;
            List<T> modified;

            // this impl. assumes that mutation of 'source' is much less frequent than read access.
            // todo: maybe add strategy lock that redirects multiple CAS misses into a thread sleep
            do
            {
                initial = source;
                modified = new List<T>(initial);
                modified.Add(item);
            } while (initial != Interlocked.CompareExchange(ref source, modified, initial));
        }

        private void FilterCopyAndSwap<T>(ref List<T> source, Func<T, bool> predicate)
        {
            // reference swap on write operations so readers don't need a lock

            List<T> initial;
            List<T> modified;

            // this impl. assumes that mutation of 'source' is much less frequent than read access.
            // todo: maybe add strategy lock that redirects multiple CAS misses into a thread sleep
            do
            {
                initial = source;
                modified = initial.Where(predicate).ToList();
            } while (initial != Interlocked.CompareExchange(ref source, modified, initial));
        }

        /// <summary>
        /// Todo: schedule ticks in such a way that they are only called once they are actually up.
        /// </summary>
        public void Tick()
        {
            // we assume that ticks are scheduled such that no two threads tick a given instance concurrently  

            if (_tick >= Period)
            {
                var canComplete = MakeRequests();

                if (canComplete) 
                {
                    CompleteRequests();
                    _tick = 0;
                }
                else
                {
                    CancelRequests();
                    _tick++;
                }

                foreach (var observer in _observers)
                    observer.OnNext(this);
            }
            else
            {
                _tick++;
            }

            bool MakeRequests() => MakeConsumerRequests() && MakeProducerRequests();

            bool MakeConsumerRequests()
            {
                // no synchronisation of _consumers is needed, due to CAS'd write operations and _consumerRequest
                // access being part of the Tick() closure that is only shared with the IDisposable interface. 

                var isValid = true;
                foreach (IArc consumer in _consumers)
                {
                    isValid = consumer.TryGetConsumeRequest(out Place.ITokenRequest req) && isValid;
                    _consumerRequests.Enqueue(req);
                    if (!isValid)
                        break;
                }

                return isValid;
            }

            bool MakeProducerRequests()
            {
                // no synchronisation of _consumers is needed, due to CAS'd write operations and _producerRequests
                // access being part of the Tick() closure that is only shared with the IDisposable interface.

                var isValid = true;
                foreach (IArc producer in _producers)
                {
                    isValid = producer.TryGetProduceRequest(out Place.ITokenRequest req) && isValid;
                    _producerRequests.Enqueue(req);
                    if (!isValid)
                        break;
                }

                return isValid;
            }

            void CompleteRequests()
            {
                while (_consumerRequests.Count > 0)
                    _consumerRequests.Dequeue().Complete();
                while (_producerRequests.Count > 0)
                    _producerRequests.Dequeue().Complete();
            }

            void CancelRequests()
            {
                while (_consumerRequests.Count > 0)
                    _consumerRequests.Dequeue()?.Dispose();
                while (_producerRequests.Count > 0)
                    _producerRequests.Dequeue()?.Dispose();
            }
        }

        protected virtual IArc CreateArc(
            INet net,
            IPlace place,
            ITransition transition,
            IToken token,
            int quantity,
            string description
        )
        {
            var arc = new Arc(net, place, transition, token, quantity, description);
            return arc;
        }

        public ITransition AddConsumer(
            in IPlace place,
            in IToken token,
            int quantity,
            string description = null
        )
        {
            var arc = CreateArc(Net, place, this, token, quantity, description);
            AddConsumer(arc);
            OnConsumerAdded();
            return this;
        }

        public ITransition AddProducer(
            in IPlace place,
            in IToken token,
            int quantity,
            string description = null
        )
        {
            var arc = CreateArc(Net, place, this, token, quantity, description);
            AddProducer(arc);
            OnProducerAdded();
            return this;
        }

        /*
        public bool RemoveArc(IArc arc)
        {
            bool consumerRemoved;
            bool producerRemoved;
            consumerRemoved = _consumers.Remove(arc);
            producerRemoved = _producers.Remove(arc);
            return consumerRemoved || producerRemoved;
        }
        */

        public void RemoveAllArcs(in IPlace place)
        {
            IPlace placeVar = place;
            FilterCopyAndSwap(ref _consumers, arc => arc.Place != placeVar);
            FilterCopyAndSwap(ref _producers, arc => arc.Place != placeVar);
        }

        public IDisposable Subscribe(IObserver<ITransition> observer)
        {
            _observers.Add(observer);
            return new Unsubscriber<ITransition>(_observers, observer);
        }

        public void Dispose()
        {
            foreach (Place.ITokenRequest request in _consumerRequests)
                request.Dispose();

            _consumers.Clear();
            _producers.Clear();

            foreach (var observer in _observers)
                observer.OnCompleted();
        }

        protected abstract void OnStart();

        protected abstract void OnNext(int tick);

        protected abstract void OnConsumerAdded();

        protected abstract void OnProducerAdded();
    }
}