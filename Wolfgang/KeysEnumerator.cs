using System.Collections;
using System.Collections.Generic;

namespace Wolfgang
{
    internal class KeysEnumerator<TKey, TValue> : IEnumerator<TKey>
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

        public KeysEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> sourceEnumerator) 
            => _enumerator = sourceEnumerator;

        public bool MoveNext() 
            => _enumerator.MoveNext();

        public void Reset() 
            => _enumerator.Reset();

        public TKey Current => _enumerator.Current.Key;

        object? IEnumerator.Current => Current;

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}