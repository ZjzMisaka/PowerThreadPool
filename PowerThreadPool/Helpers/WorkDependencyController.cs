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
        private int _firstRegister = 0;

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

                if (Interlocked.CompareExchange(ref _firstRegister, 1, 0) == 0)
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
            Dictionary<string, HashSet<string>> dependencyGraph = new Dictionary<string, HashSet<string>>();
            dependencyGraph[id] = new HashSet<string>(dependents);

            foreach (KeyValuePair<string, WorkBase> kvp in _workDict)
            {
                dependencyGraph[kvp.Key] = new HashSet<string>(kvp.Value.Dependents);
            }

            HashSet<string> visited = new HashSet<string>();
            HashSet<string> recursionStack = new HashSet<string>();

            return DetectCycleDFS(id, dependencyGraph, visited, recursionStack);
        }

        private bool DetectCycleDFS(string current, Dictionary<string, HashSet<string>> graph, HashSet<string> visited, HashSet<string> recursionStack)
        {
            if (!graph.ContainsKey(current))
            {
                return false;
            }

            visited.Add(current);
            recursionStack.Add(current);

            foreach (string dependent in graph[current])
            {
                if (recursionStack.Contains(dependent))
                {
                    return true;
                }

                if (!visited.Contains(dependent) && DetectCycleDFS(dependent, graph, visited, recursionStack))
                {
                    return true;
                }
            }

            recursionStack.Remove(current);
            return false;
        }

        private void OnCallbackEnd(WorkBase endWork, Status status)
        {
            foreach (WorkBase work in _workDict.Values)
            {
                string id = endWork.ID;
                if (endWork.BaseAsyncWorkID != null)
                {
                    id = endWork.BaseAsyncWorkID;
                }
                if (work.Dependents.Contains(id))
                {
                    if (status == Status.Failed || status == Status.Canceled)
                    {
                        work.DependencyFailed = true;
                        Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                        _powerPool.CheckPoolIdle();
                        return;
                    }

                    if (work.Dependents.Remove(id))
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
