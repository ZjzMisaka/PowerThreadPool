using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using static PowerThreadPool.PowerPool;

namespace PowerThreadPool.Helpers.Dependency
{
    internal class WorkDependencyController
    {
        internal ConcurrentDictionary<string, WorkBase> _workDict = new ConcurrentDictionary<string, WorkBase>();
        private CallbackEndEventHandler _callbackEndHandler;
        private PowerPool _powerPool;
        private int _firstRegister = 0;

        internal WorkDependencyController(PowerPool powerPool)
        {
            _powerPool = powerPool;
        }

        internal bool Register(WorkBase work, ConcurrentSet<string> dependents, out bool workNotSuccessfullyCompleted)
        {
            workNotSuccessfullyCompleted = false;

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
                        work._dependencyStatus.InterlockedValue = DependencyStatus.Failed;
                        _workDict.TryRemove(work.ID, out _);
                        _powerPool.WorkCallbackEnd(work, Status.Failed);
                        _powerPool.CheckPoolIdle();
                        workNotSuccessfullyCompleted = true;
                        return true;
                    }
                }

                List<string> toRemove = new List<string>();
                foreach (string depId in dependents)
                {
                    if (IsSucceeded(depId))
                    {
                        toRemove.Add(depId);
                    }
                }
                foreach (var depId in toRemove)
                {
                    dependents.Remove(depId);
                }

                if (dependents.Count == 0 && work._dependencyStatus.TrySet(DependencyStatus.Solved, DependencyStatus.Normal))
                {
                    _powerPool.SetWork(work);
                }

                return true;
            }

            return false;
        }

        private bool IsSucceeded(string id)
        {
            if (_powerPool._resultDic.TryGetValue(id, out ExecuteResultBase res))
            {
                return res.Status == Status.Succeed;
            }
            return false;
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
            string id = endWork.RealWorkID;

            if (status == Status.Failed || status == Status.Canceled)
            {
                Stack<string> stack = new Stack<string>();
                HashSet<string> visited = new HashSet<string>();
                List<WorkBase> newlyFailed = new List<WorkBase>();

                stack.Push(id);
                visited.Add(id);

                while (stack.Count > 0)
                {
                    string failedId = stack.Pop();

                    foreach (WorkBase work in _workDict.Values)
                    {
                        if (work._dependencyStatus.InterlockedValue == DependencyStatus.Normal &&
                            work.Dependents.Contains(failedId))
                        {
                            if (work._dependencyStatus.TrySet(DependencyStatus.Failed, DependencyStatus.Normal))
                            {
                                Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                                newlyFailed.Add(work);

                                if (visited.Add(work.RealWorkID))
                                {
                                    stack.Push(work.RealWorkID);
                                }
                            }
                        }
                    }
                }

                _powerPool.CheckPoolIdle();
                return;
            }

            List<WorkBase> readyList = new List<WorkBase>();
            foreach (WorkBase work in _workDict.Values)
            {
                if (work.Dependents.Remove(id))
                {
                    if (work.Dependents.Count == 0 &&
                        work._dependencyStatus.TrySet(DependencyStatus.Solved, DependencyStatus.Normal))
                    {
                        readyList.Add(work);
                    }
                }
            }

            foreach (WorkBase work in readyList)
            {
                _powerPool.SetWork(work);
            }
        }

        private bool PrecedingWorkNotSuccessfullyCompleted(string dependedId)
        {
            return _powerPool._failedWorkSet.Contains(dependedId) || _powerPool._canceledWorkSet.Contains(dependedId);
        }
    }
}
