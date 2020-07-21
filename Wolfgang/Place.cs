using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Wolfgang
{
    public class Place :
        IPlace,
        IEnumerable<KeyValuePair<IToken, int>>,
        IObservable<KeyValuePair<IToken, int>>,
        IDisposable
    {
        private readonly INet _net;
        private readonly int _capacity = int.MaxValue;
        private readonly ConcurrentDictionary<IToken, int> _tokens = new ConcurrentDictionary<IToken, int>();
        private readonly ConcurrentDictionary<IToken, int> _reserved = new ConcurrentDictionary<IToken, int>();
        private TokenAccount[] _tokenAccounts = new TokenAccount[0];
        private readonly HashSet<IObserver<KeyValuePair<IToken, int>>> _observers = new HashSet<IObserver<KeyValuePair<IToken, int>>>();
        private readonly HashSet<IArc> _arcs = new HashSet<IArc>();
        private string _description;
        private object _collectionChangeLock = new object();
        private int _requestCounter;
        private bool _blocked;

        private struct TokenAccount
        {
            public IToken Token;
            public int Count;
        }

        public Place()
        {
        }

        internal Place(INet net, string description)
        {
            _net = net;
            _description = description;
        }

        public int this[IToken key] => GetTokenCount(key);

        #region IPlace

        public INet Net => _net;

        public IReadOnlyDictionary<IToken, int> Tokens => _tokens;

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public void AddToken(in IToken token, int count)
        {
            var isValidRequest = TryCreateTokenChangeRequest(in token, count, out ITokenRequest request);
            if (isValidRequest)
                request?.Complete();
        }

        public bool TryCreateTokenChangeRequest(in IToken token, int delta, out ITokenRequest request)
        {
            request = null;
            if (_blocked)
                return false;
            
            int availableInitial;
            int reservedInitial;
            int reservedNew;
            
            do // CAS guard
            {
                // TODO: maybe _tokens has to be synchronised with _reserved?
                // Current thinking why not: _tokens changes only through _reserved, so if we detect all changes
                // to _reserved we will also detect all changes to _tokens.

                availableInitial = _tokens.GetOrAdd(token, 0);
                reservedInitial = _reserved.GetOrAdd(token, 0);

                if (delta >= 0)
                {
                    // add
                    reservedNew = reservedInitial;
                    var volumeAfterCompletion = (reservedInitial + delta) * token.Volume;
                    if (Capacity < volumeAfterCompletion)
                        return false;
                }
                else
                {
                    // remove
                    reservedNew = reservedInitial - delta;
                    if (availableInitial < reservedNew)
                        return false;
                }
            } while (!_reserved.TryUpdate(token, reservedNew, reservedInitial));

            Place placeVariable = this;
            request = new TokenRequest(in _reserved, in placeVariable, in token, delta);
            return true;
        }

        public int GetTokenCount(in IToken token)
        {
            return _tokens.TryGetValue(token, out var value) ? value : 0;
        }

        private bool TryUpdate(in IToken token, int delta)
        {
            int initialCount;
            int newCount;
            
            do // CAS guard
            {
                initialCount = _tokens.GetOrAdd(token, 0);
                newCount = initialCount + delta;
                // guard capacity
                if (!IsValidTokenCount(token, newCount))
                    return false;
                
            } while (!_tokens.TryUpdate(token, newCount, initialCount));

            return true;
        }
        
        private bool IsValidTokenCount(in IToken token, int count) => count >= 0 && count * token.Volume <= Capacity;

        public bool IsEmpty => _tokens.Sum(i=>i.Value) == 0 && _reserved.Sum(i=>i.Value) == 0;

        INet IPlace.Net { get; set; }

        public int Capacity => _capacity;

        #endregion


        #region IEnumerable

        public IEnumerator<KeyValuePair<IToken, int>> GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IObservable

        private void NotifyObserversChanged(KeyValuePair<IToken, int> keyValuePair)
        {
            foreach (IObserver<KeyValuePair<IToken, int>> observer in _observers)
                observer.OnNext(keyValuePair);
        }

        private void NotifyObserversComplete()
        {
            foreach (IObserver<KeyValuePair<IToken, int>> observer in _observers)
                observer.OnCompleted();
        }

        public IDisposable Subscribe(IObserver<KeyValuePair<IToken, int>> observer)
        {
            _observers.Add(observer);
            return new Unsubscriber<KeyValuePair<IToken, int>>(_observers, observer);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            NotifyObserversComplete();
        }

        #endregion

        internal class TokenRequest : ITokenRequest
        {
            public TokenRequest(
                in ConcurrentDictionary<IToken, int> requests,
                in Place place,
                in IToken token,
                int quantity
            )
            {
                Place = place;
                Token = token;
                Quantity = quantity;
                Requests = requests;
            }

            private Place Place { get; }
            private int Quantity { get; }
            private IToken Token { get; }
            private ConcurrentDictionary<IToken, int> Requests { get; }

            public void Complete()
            {
                if (!Place.TryUpdate(Token, Quantity))
                    throw new Exception("Invalid synchronisation state.");
                Dispose();
            }

            public void Dispose()
            {
                Requests[Token] -= Quantity;
            }
        }

        public interface ITokenRequest : IDisposable
        {
            public void Complete();
        }

        public void BlockRequests() => _blocked = true;
    }
}