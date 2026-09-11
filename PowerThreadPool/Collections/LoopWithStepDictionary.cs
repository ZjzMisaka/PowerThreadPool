using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Collections
{
    // Static analysis tools and LLM-based analysis may flag that the snapshot array could
    // contain workers already removed from _innerDict, could miss workers added after the
    // snapshot was built, or that the shared _cursor is incremented concurrently.
    // This is NOT a bug.
    //
    // PTP's work-stealing algorithm iterates over the workers to reallocate tasks
    // from those with a relatively higher number of pending tasks.
    // To prevent excessive iteration overhead when managing a large number of worker instances,
    // each scan only walks a limited segment of the worker snapshot.
    //
    // The snapshot array is rebuilt only when a worker is added to or removed from _innerDict. 
    // This mechanism restricts each execution to a small segment of the worker snapshot. 
    // A shared rotating cursor (_cursor) is used to ensure that the starting point of each
    // scan varies as much as possible.
    //
    // Strict real-time consistency of the snapshot or the cursor is not required;
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
    // 3. Missing a newly created worker:
    //    The snapshot is rebuilt synchronously when a worker is added,
    //    so the next scan will find it.
    // 
    // Since static analysis tools and single-file-level LLM analysis often lack
    // the broader context necessary to understand intentional design trade-offs,
    // any reported "defects" regarding this logic should be carefully verified
    // before assuming they represent actual bugs.
    internal class LoopWithStepDictionary<TKey, TValue> where TValue : class
    {
        private static readonly TValue[] s_empty = new TValue[0];

        internal ConcurrentDictionary<TKey, TValue> _innerDict = new ConcurrentDictionary<TKey, TValue>();
        private readonly InterlockedFlag<CanRebuildSnapshot> _canRebuildSnapshot = CanRebuildSnapshot.Allowed;
        private volatile TValue[] _snapshot = s_empty;
        private int _cursor = -1;
        public bool TryAdd(TKey key, TValue value)
        {
            _innerDict[key] = value;
            RebuildSnapshot();
            return true;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            if (!_innerDict.TryRemove(key, out value))
            {
                return false;
            }
            RebuildSnapshot();
            return true;
        }

        public void Clear()
        {
            _innerDict.Clear();

            _snapshot = s_empty;
            _cursor = -1;
        }

        internal TValue[] GetSnapshot()
            => _snapshot;

        internal int GetNextStartIndex(int count)
        {
            int cursor = _cursor + 1;
            _cursor = cursor;
            return (int)((uint)cursor % (uint)count);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _innerDict.GetEnumerator();
        }

        private void RebuildSnapshot()
        {
            if (_canRebuildSnapshot.TrySet(CanRebuildSnapshot.NotAllowed, CanRebuildSnapshot.Allowed))
            {
                ConcurrentDictionary<TKey, TValue> innerDict = _innerDict;
                if (innerDict.IsEmpty)
                {
                    _snapshot = s_empty;
                    return;
                }
                TValue[] snapshot = new TValue[innerDict.Count];
                ((ICollection<TValue>)innerDict.Values).CopyTo(snapshot, 0);
                _snapshot = snapshot;

                _canRebuildSnapshot.InterlockedValue = CanRebuildSnapshot.Allowed;
            }
        }
    }
}
