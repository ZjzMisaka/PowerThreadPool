using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using static PowerThreadPool.PowerPool;

namespace PowerThreadPool.Helpers
{
    internal class WorkDependencyController
    {
        private ConcurrentDictionary<string, WorkBase> _workDict = new ConcurrentDictionary<string, WorkBase>();
        private CallbackEndEventHandler _callbackEndHandler;
        private PowerPool _powerPool;
        private int _firstRegist = 0;

        internal WorkDependencyController(PowerPool powerPool)
        {
            this._powerPool = powerPool;
        }

        internal void Register(WorkBase work, ConcurrentSet<string> dependents)
        {
            if (dependents != null && dependents.Count != 0)
            {
                if (CheckHasCycle(work.ID, dependents))
                {
                    throw new CycleDetectedException
                    {
                        ID = work.ID,
                    };
                }

                if (Interlocked.CompareExchange(ref _firstRegist, 1, 0) == 0)
                {
                    _callbackEndHandler = OnCallbackEnd;
                    _powerPool.CallbackEnd += _callbackEndHandler;
                }

                _workDict[work.ID] = work;

                foreach (string dependedId in dependents)
                {
                    if (PrecedingWorkNotSuccessfullyCompleted(dependedId))
                    {
                        work.DependencyFailed = true;
                        _workDict.TryRemove(work.ID, out _);
                        _powerPool.WorkCallbackEnd(work, Status.Failed);
                        _powerPool.CheckPoolIdle();
                        return;
                    }
                }
            }
        }

        internal void Cancel()
        {
            List<string> idList = _workDict.Keys.ToList();
            foreach (string id in idList)
            {
                if (_workDict.TryRemove(id, out _))
                {
                    Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                }
            }
            _powerPool.CheckPoolIdle();
        }

        internal bool Cancel(string id)
        {
            bool res = false;
            if (_workDict.TryRemove(id, out _))
            {
                Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                _powerPool.CheckPoolIdle();
                res = true;
            }
            return res;
        }

        private bool CheckHasCycle(string id, ConcurrentSet<string> dependents)
        {
            foreach (string dependent in dependents)
            {
                foreach (WorkBase work in _workDict.Values)
                {
                    if (dependent == work.ID && work.Dependents.Contains(id))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnCallbackEnd(WorkBase endWork, Status status)
        {
            foreach (WorkBase work in _workDict.Values)
            {
                if (work.Dependents.Contains(endWork.ID))
                {
                    if (status == Status.Failed || status == Status.Canceled)
                    {
                        work.DependencyFailed = true;
                        Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                        _powerPool.CheckPoolIdle();
                        return;
                    }

                    if (work.Dependents.Remove(endWork.ID))
                    {
                        if (work.Dependents.Count == 0)
                        {
                            _powerPool.SetWork(work);
                        }
                    }
                }
            }

        }

        private bool PrecedingWorkNotSuccessfullyCompleted(string dependedId)
        {
            return _powerPool._failedWorkSet.Contains(dependedId) || _powerPool._canceledWorkSet.Contains(dependedId);
        }
    }
}
