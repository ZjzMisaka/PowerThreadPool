using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PowerThreadPool.Collections
{
    public class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, byte> dictionary;

        private static readonly byte DummyValue = default;

        public ConcurrentSet()
        {
            dictionary = new ConcurrentDictionary<T, byte>();
        }

        public ConcurrentSet(IEnumerable<T> items)
        {
            dictionary = new ConcurrentDictionary<T, byte>();
            foreach (var item in items)
            {
                dictionary.TryAdd(item, DummyValue);
            }
        }

        public void Add(T item)
        {
            dictionary.TryAdd(item, DummyValue);
        }

        public bool TryRemove(T item)
        {
            return dictionary.TryRemove(item, out _);
        }

        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
