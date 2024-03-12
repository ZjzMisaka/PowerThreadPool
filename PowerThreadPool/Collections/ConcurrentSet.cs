using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

        public bool Add(T item)
        {
            return dictionary.TryAdd(item, DummyValue);
        }

        public bool Remove(T item)
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

        public List<T> ToList()
        {
            return dictionary.Keys.ToList();
        }
    }
}
