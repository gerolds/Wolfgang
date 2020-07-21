using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Wolfgang
{
    public class Net : INet
    {
        public Net()
        {
            Transitions = new KeysEnumerable<ITransition, byte>(_transitions);
            Places = new KeysEnumerable<IPlace, byte>(_places);
        }

        public const int MinTickPeriod = 16;

        // Todo: maybe wrap ConcurrentDictionary into a ConcurrentHashSet type to make semantics clearer?
        private readonly ConcurrentDictionary<IPlace, byte> _places
            = new ConcurrentDictionary<IPlace, byte>();

        private readonly ConcurrentDictionary<ITransition, byte> _transitions
            = new ConcurrentDictionary<ITransition, byte>();

        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private Task _tickTask;

        /// <inheritdoc />
        public int PlaceCount => _places.Count;

        /// <inheritdoc />
        public int TransitionCount => _transitions.Count;

        /// <inheritdoc />
        public IEnumerable<IPlace> Places { get; }

        /// <inheritdoc />
        public IEnumerable<ITransition> Transitions { get; }

        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        public INet CreatePlace(out Place place, string description = null)
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(CreateTransition)} called during tick.");

            INet net = CreatePlace<Place>(out Place instance, description);
            place = (Place) instance;
            return net;
        }

        /// <inheritdoc />
        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        public INet CreatePlace<TPlace>(out TPlace place, string description = null) where TPlace : IPlace, new()
        {
            place = new TPlace
            {
                Net = this,
                Description = description
            };
            RegisterPlace(place);
            return this;
        }

        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        public INet CreateTransition(out Transition transition, int period = 0, string description = null)
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(CreateTransition)} called during tick.");

            CreateTransition<Transition>(out Transition instance, period, description);
            transition = (Transition) instance;
            return this;
        }

        /// <inheritdoc />
        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        public INet CreateTransition<TTransition>(out TTransition transition, int period = 0, string description = null)
            where TTransition : ITransition, new()
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(CreateTransition)} called during tick.");

            transition = new TTransition
            {
                Net = this,
                Description = description,
                Period = period
            };
            RegisterTransition(transition);
            return this;
        }

        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        private void RegisterPlace(IPlace place)
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(RegisterPlace)} called during tick.");

            _places[place] = default;
        }

        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        private void RegisterTransition(ITransition transition)
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(RegisterTransition)} called during tick.");
            _transitions[transition] = default;
        }

        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        public bool TryRemove(IPlace place)
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(TryRemove)} called during tick.");

            if (!place.IsEmpty)
                throw new InvalidOperationException(
                    $"Calling {nameof(TryRemove)} on non-empty {nameof(Place)} is not allowed."
                );
            var removed = _places.TryRemove(place, out _);
            if (removed)
                place.Dispose();

            // Todo: this can/should(?) be optimized by caching ITransition references in Places
            foreach (var transition in _transitions)
                transition.Key.RemoveAllArcs(place);

            return removed;
        }

        /// <inheritdoc />
        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        public bool TryRemove(ITransition transition)
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(TryRemove)} called during tick.");

            return _transitions.TryRemove(transition, out _);
        }

        /// <inheritdoc />
        /// <exception cref="Exception">When called concurrently with <see cref="TickAsync"/>.</exception>
        public bool CanRemove(IPlace place)
        {
            if (_tickTask != null && !_tickTask.IsCompleted)
                throw new Exception($"{nameof(CanRemove)} called during tick.");

            return place.IsEmpty;
        }

        /// <inheritdoc />
        public Task TickAsync(CancellationToken cancellationToken)
        {
            // awaitable wrapper around ParallelLoop
            _tickTask = Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ParallelLoopResult result = TickWorker(Transitions, cancellationToken);
                    if (!result.IsCompleted)
                        throw new Exception("Tick incomplete");
                }
            );
            return _tickTask;
        }

        public void Tick()
        {
            foreach (ITransition transition in Transitions)
                transition.Tick();
        }

        private static ParallelLoopResult TickWorker(
            [NotNull] IEnumerable<ITransition> transitions,
            [NotNull] CancellationToken cancellationToken
        )
        {
            var result = Parallel.ForEach(
                transitions,
                (transition) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    transition.Tick();
                }
            );
            return result;
        }

        /// <inheritdoc />
        public IDictionary<IToken, int> GetAggregateTokenCounts()
        {
            var tokens = new Dictionary<IToken, int>();
            foreach (IPlace place in Places)
            {
                foreach (var pair in place.Tokens)
                {
                    if (tokens.ContainsKey(pair.Key))
                        tokens[pair.Key] += pair.Value;
                    else
                        tokens[pair.Key] = pair.Value;
                }
            }

            return tokens;
        }

        /// <inheritdoc />
        public ICollection<IArc> GetProducers()
        {
            var arcs = new List<IArc>();
            foreach (ITransition place in Transitions)
                arcs.AddRange(place.Producers);
            return arcs;
        }

        /// <inheritdoc />
        public ICollection<IArc> GetConsumers()
        {
            var arcs = new List<IArc>();
            foreach (ITransition place in Transitions)
                arcs.AddRange(place.Consumers);
            return arcs;
        }

        /// <inheritdoc />
        public ICollection<IPlace> GetPlaces() => _places.Keys;

        /// <inheritdoc />
        public ICollection<ITransition> GetTransitions() => _transitions.Keys;

        /// <inheritdoc />
        public bool Contains(IPlace place) => _places.ContainsKey(place);

        /// <inheritdoc />
        public bool Contains(ITransition transition) => _transitions.ContainsKey(transition);
    }
}