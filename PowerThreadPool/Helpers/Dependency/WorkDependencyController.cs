using System;
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
        internal ConcurrentDictionary<WorkID, WorkBase> _workDict = new ConcurrentDictionary<WorkID, WorkBase>();
        internal ConcurrentDictionary<WorkID, ConcurrentSet<WorkID>> _workChildrenDict = new ConcurrentDictionary<WorkID, ConcurrentSet<WorkID>>();
        private CallbackEndEventHandler _callbackEndHandler;
        private PowerPool _powerPool;
        private int _firstRegister = 0;

        internal WorkDependencyController(PowerPool powerPool)
        {
            _powerPool = powerPool;
        }

        internal bool Register(WorkBase work, ConcurrentSet<WorkID> dependents, out bool workNotSuccessfullyCompleted)
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
                foreach (WorkID dependedId in dependents)
                {
                    if (PrecedingWorkNotSuccessfullyCompleted(dependedId))
                    {
                        work._dependencyStatus.InterlockedValue = DependencyStatus.Failed;
                        _workDict.TryRemove(work.ID, out _);

                        if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
                        {
                            work.QueueDateTime = DateTime.UtcNow;
                        }

                        InvalidOperationException exception = new InvalidOperationException($"Work '{work.ID}' failed because dependency '{dependedId}' did not complete successfully.");
                        ExecuteResultBase executeResult = work.SetExecuteResult(null, exception, Status.Failed);
                        executeResult.ID = work.ID;
                        if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
                        {
                            executeResult.StartDateTime = DateTime.UtcNow;
                        }

                        _powerPool._resultDic[work.ID] = executeResult;

                        _powerPool.InvokeWorkEndedEvent(executeResult, work.BaseAsyncWorkID != null);

                        work.InvokeCallback(executeResult, _powerPool.PowerPoolOption);

                        _powerPool.WorkCallbackEnd(work, Status.Failed);

                        _powerPool.CheckPoolIdle();
                        workNotSuccessfullyCompleted = true;
                        return true;
                    }
                    else
                    {
                        _workChildrenDict.AddOrUpdate(dependedId,
                        (id) =>
                        {
                            ConcurrentSet<WorkID> set = new ConcurrentSet<WorkID>();
                            set.Add(work.ID);
                            return set;
                        },
                        (id, existingSet) =>
                        {
                            existingSet.Add(work.ID);
                            return existingSet;
                        });
                    }
                }

                List<WorkID> toRemove = new List<WorkID>();
                foreach (WorkID depId in dependents)
                {
                    if (IsSucceeded(depId))
                    {
                        toRemove.Add(depId);
                    }
                }
                foreach (WorkID depId in toRemove)
                {
                    dependents.Remove(depId);
                }

                if (dependents.Count == 0 &&
                    work._dependencyStatus.TrySet(DependencyStatus.Solved, DependencyStatus.Normal))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private void SetWorkIfDependencySolved(ConcurrentSet<WorkID> dependents, WorkBase work)
        {
            if (dependents.Count == 0 &&
                work._dependencyStatus.TrySet(DependencyStatus.Solved, DependencyStatus.Normal))
            {
                _powerPool.SetWork(work);
            }
        }

        private bool IsSucceeded(WorkID id)
        {
            if (_powerPool._resultDic.TryGetValue(id, out ExecuteResultBase res))
            {
                return res.Status == Status.Succeed;
            }
            return false;
        }

        internal void Cancel()
        {
            List<WorkID> idList = _workDict.Keys.ToList();
            foreach (WorkID id in idList)
            {
                if (_workDict.TryRemove(id, out _))
                {
                    Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                }
            }
            _powerPool.CheckPoolIdle();
        }

        internal bool Cancel(WorkID id)
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

        private bool CheckHasCycle(WorkID id, ConcurrentSet<WorkID> dependents)
        {
            Dictionary<WorkID, HashSet<WorkID>> dependencyGraph = new Dictionary<WorkID, HashSet<WorkID>>();
            dependencyGraph[id] = new HashSet<WorkID>(dependents);

            foreach (KeyValuePair<WorkID, WorkBase> kvp in _workDict)
            {
                dependencyGraph[kvp.Key] = new HashSet<WorkID>(kvp.Value.Dependents);
            }

            HashSet<WorkID> visited = new HashSet<WorkID>();
            HashSet<WorkID> recursionStack = new HashSet<WorkID>();

            return DetectCycleDFS(id, dependencyGraph, visited, recursionStack);
        }

        private bool DetectCycleDFS(WorkID current, Dictionary<WorkID, HashSet<WorkID>> graph, HashSet<WorkID> visited, HashSet<WorkID> recursionStack)
        {
            if (!graph.ContainsKey(current))
            {
                return false;
            }

            visited.Add(current);
            recursionStack.Add(current);

            foreach (WorkID dependent in graph[current])
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
            WorkID id = endWork.RealWorkID;

            if (status == Status.Failed || status == Status.Canceled)
            {
                Stack<WorkID> stack = new Stack<WorkID>();
                HashSet<WorkID> visited = new HashSet<WorkID>();
                List<WorkBase> newlyFailed = new List<WorkBase>();

                stack.Push(id);
                visited.Add(id);

                while (stack.Count > 0)
                {
                    WorkID failedId = stack.Pop();

                    if (_workChildrenDict.TryGetValue(failedId, out ConcurrentSet<WorkID> failedChildWorkSet))
                    {
                        foreach (WorkID workID in failedChildWorkSet)
                        {
                            if (_workDict.TryGetValue(workID, out WorkBase work) && work._dependencyStatus.TrySet(DependencyStatus.Failed, DependencyStatus.Normal))
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

                foreach (WorkBase work in newlyFailed)
                {
                    if (_powerPool.PowerPoolOption.EnableStatisticsCollection && work.QueueDateTime == default)
                    {
                        work.QueueDateTime = DateTime.UtcNow;
                    }

                    InvalidOperationException exception = new InvalidOperationException($"Work '{work.ID}' failed because dependency '{id}' did not complete successfully.");
                    ExecuteResultBase executeResult = work.SetExecuteResult(null, exception, Status.Failed);
                    executeResult.ID = work.ID;
                    if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
                    {
                        executeResult.StartDateTime = DateTime.UtcNow;
                    }

                    _powerPool._resultDic[work.ID] = executeResult;

                    _powerPool.InvokeWorkEndedEvent(executeResult, work.BaseAsyncWorkID != null);
                    work.InvokeCallback(executeResult, _powerPool.PowerPoolOption);
                    _powerPool.WorkCallbackEnd(work, Status.Failed);
                }

                _powerPool.CheckPoolIdle();
                return;
            }

            if (_workChildrenDict.TryGetValue(id, out ConcurrentSet<WorkID> childWorkSet))
            {
                foreach (WorkID workID in childWorkSet)
                {
                    if (_workDict.TryGetValue(workID, out WorkBase work) && work.Dependents.Remove(id))
                    {
                        SetWorkIfDependencySolved(work.Dependents, work);
                    }
                }
            }
        }

        private bool PrecedingWorkNotSuccessfullyCompleted(WorkID dependedId)
        {
            return _powerPool._failedWorkSet.Contains(dependedId) || _powerPool._canceledWorkSet.Contains(dependedId);
        }
    }
}
