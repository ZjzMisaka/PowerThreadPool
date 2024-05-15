using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PowerThreadPool.Collections
{
    public class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, byte> _dictionary;

        private static readonly byte s_dummyValue = default;

        public ConcurrentSet()
        {
            _dictionary = new ConcurrentDictionary<T, byte>();
        }

        public ConcurrentSet(IEnumerable<T> items)
        {
            _dictionary = new ConcurrentDictionary<T, byte>();
            foreach (T item in items)
            {
                _dictionary.TryAdd(item, s_dummyValue);
            }
        }

        public bool Add(T item)
        {
            return _dictionary.TryAdd(item, s_dummyValue);
        }

        public bool Remove(T item)
        {
            return _dictionary.TryRemove(item, out _);
        }

        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
