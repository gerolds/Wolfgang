using System.Collections;
using System.Collections.Generic;

namespace Wolfgang
{
    /// <summary>
    /// Iterate the keys of a dictionary based on the key-value pairs returned by the dictionary's GetEnumerator.  
    /// </summary>
    /// <remarks>
    /// The enumerator returned is safe to use concurrently with reads and writes, however it does not represent
    /// a moment-in-time snapshot of the dictionary. The contents exposed through the enumerator may contain
    /// modifications made to the dictionary after GetEnumerator was called.
    /// </remarks>
    internal class KeysEnumerable<TKey, TValue> : IEnumerable<TKey>
    {
        private readonly IDictionary<TKey, TValue> _source;

        public KeysEnumerable(in IDictionary<TKey, TValue> source)
            => _source = source;

        public IEnumerator<TKey> GetEnumerator()
            => new KeysEnumerator<TKey, TValue>(_source.GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}