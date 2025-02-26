#if NET9_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Faster.Map.Concurrent;
#else
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
#endif

namespace PowerThreadPool.Collections
{
    internal class FasterConcurrentDictionary<TKey, TValue>
    {
#if NET9_0_OR_GREATER
        public TValue this[TKey key]
        {
            get => _cMap[key];
            set => TryAdd(key, value);
        }
#else
        public TValue this[TKey key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }
#endif

#if NET9_0_OR_GREATER
        private CMap<TKey, TValue> _cMap;
#else
        private ConcurrentDictionary<TKey, TValue> _dict;
#endif

#if NET9_0_OR_GREATER
        public int Count { get => _cMap.Count; }
#else
        public int Count { get => _dict.Count; }
#endif

#if NET9_0_OR_GREATER
        public IEnumerable<TKey> Keys { get => _cMap.Keys; }
#else
        public IEnumerable<TKey> Keys { get => _dict.Keys; }
#endif

#if NET9_0_OR_GREATER
        public IEnumerable<TValue> Values { get => _cMap.Values; }
#else
        public IEnumerable<TValue> Values { get => _dict.Values; }
#endif

        public FasterConcurrentDictionary()
        {
#if NET9_0_OR_GREATER
            _cMap = new CMap<TKey, TValue>();
#else
            _dict = new ConcurrentDictionary<TKey, TValue>();
#endif
        }

        public bool TryAdd(TKey key, TValue value)
        {
#if NET9_0_OR_GREATER
            return _cMap.Emplace(key, value);
# else
            return _dict.TryAdd(key, value);
#endif
        }

        public bool TryRemove(TKey key, out TValue value)
        {
#if NET9_0_OR_GREATER
            return _cMap.Remove(key, out value);
#else
            return _dict.TryRemove(key, out value);
#endif
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
#if NET9_0_OR_GREATER
            return _cMap.Get(key, out value);
#else
            return _dict.TryGetValue(key, out value);
#endif
        }

        public void Clear()
        {
#if NET9_0_OR_GREATER
            _cMap.Clear();
#else
            _dict.Clear();
#endif
        }


        public bool ContainsKey(TKey key)
        {
#if NET9_0_OR_GREATER
            return _cMap.Keys.ToList().Contains(key);
#else
            return _dict.ContainsKey(key);
#endif
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
#if NET9_0_OR_GREATER
            return _cMap.Entries.GetEnumerator();
#else
            return _dict.GetEnumerator();
#endif
        }

        public IEnumerator<TKey> GetKeyEnumerator()
        {
#if NET9_0_OR_GREATER
            return _cMap.Keys.GetEnumerator();
#else
            return _dict.Keys.GetEnumerator();
#endif
        }
    }
}
