using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Collections
{
    internal class LoopWithStepDictionary<TKey, TValue> where TValue : class
    {
        internal ConcurrentDictionary<TKey, TValue> _innerDict = new ConcurrentDictionary<TKey, TValue>();
        internal IEnumerator<KeyValuePair<TKey, TValue>> _enumerator = null;
        internal readonly InterlockedFlag<CanEnumeratorMoveNext> _canEnumeratorMoveNext = CanEnumeratorMoveNext.Allowed;
        internal TValue _current = null;

        public bool TryAdd(TKey key, TValue value)
            => _innerDict.TryAdd(key, value);

        public bool TryRemove(TKey key, out TValue value)
            => _innerDict.TryRemove(key, out value);

        public void Clear()
        {
            _innerDict.Clear();
            _enumerator = null;
            _current = null;
        }

        public TValue InitEnumerator()
            => InitEnumerator(true);

        private TValue InitEnumerator(bool checkNull)
        {
            TValue currentElement = null;

            if (!checkNull || _enumerator == null)
            {
                _enumerator = _innerDict.GetEnumerator();
                currentElement = _enumerator.MoveNext()
                    ? _enumerator.Current.Value
                    : null;
            }

            while (currentElement == null)
            {
                if (_innerDict.IsEmpty)
                {
                    _current = null;
                    return null;
                }

                currentElement = GetNext();
            }

            _current = currentElement;
            return currentElement;
        }

        public TValue GetNext()
        {
            while (true)
            {
                if (_canEnumeratorMoveNext.TrySet(CanEnumeratorMoveNext.NotAllowed, CanEnumeratorMoveNext.Allowed))
                {
                    TValue element;

                    if (!_enumerator.MoveNext())
                    {
                        element = InitEnumerator(false);
                        _canEnumeratorMoveNext.InterlockedValue = CanEnumeratorMoveNext.Allowed;
                        return element;
                    }

                    element = _enumerator.Current.Value;

                    if (element != null)
                    {
                        _current = element;
                    }
                    _canEnumeratorMoveNext.InterlockedValue = CanEnumeratorMoveNext.Allowed;

                    return element;
                }
                else
                {
                    TValue cached = _current;
                    if (cached != null)
                    {
                        return cached;
                    }

                    if (_innerDict.IsEmpty)
                    {
                        return null;
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _innerDict.GetEnumerator();
        }
    }
}
