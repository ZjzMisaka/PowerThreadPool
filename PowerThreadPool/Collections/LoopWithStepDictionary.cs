using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Collections
{
    // Static analysis tools and LLM-based analysis may flag that direct assignments
    // could cause _enumerator, _current or result of GetNext() to read stale data.
    // This is NOT a bug.
    //
    // PTP's work-stealing algorithm iterates over the workers to reallocate tasks
    // from those with a relatively higher number of pending tasks.
    // To prevent excessive iteration overhead when managing a large number of worker instances,
    // the _enumerator was introduced.
    // This mechanism restricts each execution to a small segment of the worker dict. 
    //
    // Strict real-time consistency of the WorkerList or the loop index is not required;
    // reasonably up-to-date values are sufficient.
    // However, it is critical to guarantee that the algorithm does not steal tasks from workers in an Idle state,
    // nor attempt to steal tasks that are currently executing or already completed [①].
    // To enforce this constraint, a strict atomic state machine is utilized.
    //
    // Regarding the potential implications of reading stale values:
    // 1. Repeatedly targeting the same worker:
    //    Stealing from a worker that was already targeted during the previous trigger is acceptable,
    //    as the work-stealing logic primarily cares about ensuring that
    //    the overall distribution of stolen tasks remains approximately even across the worker pool.
    // 2. Targeting a terminated or terminating worker:
    //    If the algorithm attempts to steal from a worker whose lifecycle has ended or is ending,
    //    this worker will be safely bypassed.
    //    The atomic state machine's Compare-And-Swap (CAS) operation will simply fail,
    //    preventing any invalid state transitions [①].
    // 
    // Since static analysis tools and single-file-level LLM analysis often lack
    // the broader context necessary to understand intentional design trade-offs,
    // any reported "defects" regarding this logic should be carefully verified
    // before assuming they represent actual bugs.
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
            TValue currentItem = null;

            if (!checkNull || _enumerator == null)
            {
                _enumerator = _innerDict.GetEnumerator();
                currentItem = _enumerator.MoveNext()
                    ? _enumerator.Current.Value
                    : null;
            }

            while (currentItem == null)
            {
                if (_innerDict.IsEmpty)
                {
                    _current = null;
                    return null;
                }

                currentItem = GetNext();
            }

            _current = currentItem;
            return currentItem;
        }

        public TValue GetNext()
        {
            while (true)
            {
                if (_canEnumeratorMoveNext.TrySet(CanEnumeratorMoveNext.NotAllowed, CanEnumeratorMoveNext.Allowed))
                {
                    TValue item;

                    if (!_enumerator.MoveNext())
                    {
                        item = InitEnumerator(false);
                        _canEnumeratorMoveNext.InterlockedValue = CanEnumeratorMoveNext.Allowed;
                        return item;
                    }

                    item = _enumerator.Current.Value;

                    if (item != null)
                    {
                        _current = item;
                    }
                    _canEnumeratorMoveNext.InterlockedValue = CanEnumeratorMoveNext.Allowed;

                    return item;
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
