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

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Add(T item) => _dictionary.TryAdd(item, s_dummyValue);

        /// <summary>
        /// Removes an item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item) => _dictionary.TryRemove(item, out _);

        /// <summary>
        /// Gets the count of items.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Returns an enumerator that iterates through the items.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => _dictionary.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
