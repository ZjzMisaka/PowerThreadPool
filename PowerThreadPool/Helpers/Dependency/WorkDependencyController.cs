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

        internal bool Register(WorkBase work, ConcurrentSet<WorkID> dependencies)
        {
            if (dependencies != null && dependencies.Count != 0)
            {
                if (!_powerPool.PowerPoolOption.ShouldStoreResult)
                {
                    throw new InvalidOperationException($"WorkOption.Dependencies requires PowerPoolOption.ShouldStoreResult to be enabled, otherwise a dependency whose preceding work has already finished can never be resolved.");
                }

                if (CheckHasCycle(work.ID, dependencies))
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
                foreach (WorkID dependedId in dependencies)
                {
                    if (PrecedingWorkNotSuccessfullyCompleted(dependedId))
                    {
                        OnPrecedingWorkNotSuccessfullyCompletedWhenRegister(work, dependedId);
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
                foreach (WorkID depId in dependencies)
                {
                    if (IsSucceeded(depId))
                    {
                        toRemove.Add(depId);
                    }
                }
                foreach (WorkID depId in toRemove)
                {
                    dependencies.Remove(depId);
                }

                if (dependencies.Count == 0 &&
                    work._dependencyStatus.TrySet(DependencyStatus.Solved, DependencyStatus.Normal))
                {
                    _workDict.TryRemove(work.ID, out _);
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private void OnPrecedingWorkNotSuccessfullyCompletedWhenRegister(WorkBase work, WorkID dependedId)
        {
            work._dependencyStatus.InterlockedValue = DependencyStatus.Failed;
            _workDict.TryRemove(work.ID, out _);

            if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
            {
                work.QueueDateTime = DateTime.UtcNow;
            }

            InvalidOperationException exception = new InvalidOperationException($"Work '{work.ID}' failed because dependency '{dependedId}' did not complete successfully.");
            ExecuteResultBase executeResult = work.SetExecuteResult(null, exception, WorkStatus.Failed);
            executeResult.ID = work.ID;
            if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
            {
                executeResult.StartDateTime = DateTime.UtcNow;
            }

            _powerPool._resultDic[work.ID] = executeResult;

            _powerPool.InvokeWorkEndedEvent(executeResult);

            work.InvokeCallback(executeResult, _powerPool.PowerPoolOption);

            _powerPool.WorkCallbackEnd(work, WorkStatus.Failed);

            _powerPool.CheckPoolIdle();
        }

        private void SetWorkIfDependencySolved(ConcurrentSet<WorkID> dependencies, WorkBase work)
        {
            if (dependencies.Count == 0 &&
                work._dependencyStatus.TrySet(DependencyStatus.Solved, DependencyStatus.Normal))
            {
                _workDict.TryRemove(work.ID, out _);
                _powerPool.SetWork(work);
            }
        }

        private bool IsSucceeded(WorkID id)
        {
            if (_powerPool._resultDic.TryGetValue(id, out ExecuteResultBase res))
            {
                return res.Status == WorkStatus.Succeeded;
            }
            return false;
        }

        internal void Cancel()
        {
            List<WorkID> idList = _workDict.Keys.ToList();
            foreach (WorkID id in idList)
            {
                _workDict.TryRemove(id, out _);
            }
            _powerPool.CheckPoolIdle();
        }

        internal bool Cancel(WorkID id, out WorkBase work)
        {
            bool res = false;
            if (_workDict.TryRemove(id, out work))
            {
                _powerPool.CheckPoolIdle();
                res = true;
            }
            return res;
        }

        private bool CheckHasCycle(WorkID id, ConcurrentSet<WorkID> dependencies)
        {
            foreach (WorkID dep in dependencies)
            {
                if (dep.Equals(id))
                {
                    return true;
                }
            }

            HashSet<WorkID> dependentsSet = new HashSet<WorkID>(dependencies);

            HashSet<WorkID> visited = new HashSet<WorkID>();
            Stack<WorkID> stack = new Stack<WorkID>();

            visited.Add(id);
            stack.Push(id);

            while (stack.Count > 0)
            {
                WorkID current = stack.Pop();

                if (!_workChildrenDict.TryGetValue(current, out ConcurrentSet<WorkID> children))
                {
                    continue;
                }

                foreach (WorkID childId in children)
                {
                    if (dependentsSet.Contains(childId))
                    {
                        return true;
                    }

                    visited.Add(childId);
                    stack.Push(childId);
                }
            }

            return false;
        }

        private void OnCallbackEnd(WorkBase endWork, WorkStatus status)
        {
            WorkID id = endWork.ID;

            if (status != WorkStatus.Succeeded)
            {
                OnCallbackFailed(id);
                return;
            }

            if (_workChildrenDict.TryGetValue(id, out ConcurrentSet<WorkID> childWorkSet))
            {
                foreach (WorkID workID in childWorkSet)
                {
                    if (_workDict.TryGetValue(workID, out WorkBase work) && work.Dependencies.Remove(id))
                    {
                        SetWorkIfDependencySolved(work.Dependencies, work);
                    }
                }
            }

            _workChildrenDict.TryRemove(id, out _);
        }

        private void OnCallbackFailed(WorkID id)
        {
            Stack<WorkID> stack = new Stack<WorkID>();
            HashSet<WorkID> visited = new HashSet<WorkID>();
            List<WorkBase> newlyFailed = new List<WorkBase>();

            stack.Push(id);
            visited.Add(id);

            GetAllFailedChildren(stack, visited, newlyFailed);
            CauseAcquiredWorksToFail(id, newlyFailed);

            _workChildrenDict.TryRemove(id, out _);
            foreach (var failedWork in newlyFailed)
            {
                _workChildrenDict.TryRemove(failedWork.ID, out _);
            }

            _powerPool.CheckPoolIdle();
        }

        private void CauseAcquiredWorksToFail(WorkID id, List<WorkBase> newlyFailed)
        {
            foreach (WorkBase work in newlyFailed)
            {
                if (_powerPool.PowerPoolOption.EnableStatisticsCollection && work.QueueDateTime == default)
                {
                    work.QueueDateTime = DateTime.UtcNow;
                }

                InvalidOperationException exception = new InvalidOperationException($"Work '{work.ID}' failed because dependency '{id}' did not complete successfully.");
                ExecuteResultBase executeResult = work.SetExecuteResult(null, exception, WorkStatus.Failed);
                executeResult.ID = work.ID;
                if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
                {
                    executeResult.StartDateTime = DateTime.UtcNow;
                }

                _powerPool._resultDic[work.ID] = executeResult;

                _powerPool.InvokeWorkEndedEvent(executeResult);
                work.InvokeCallback(executeResult, _powerPool.PowerPoolOption);
                _powerPool.WorkCallbackEnd(work, WorkStatus.Failed);
            }
        }

        private void GetAllFailedChildren(Stack<WorkID> stack, HashSet<WorkID> visited, List<WorkBase> newlyFailed)
        {
            while (stack.Count > 0)
            {
                WorkID failedId = stack.Pop();

                if (_workChildrenDict.TryGetValue(failedId, out ConcurrentSet<WorkID> failedChildWorkSet))
                {
                    foreach (WorkID workID in failedChildWorkSet)
                    {
                        if (_workDict.TryGetValue(workID, out WorkBase work) && work._dependencyStatus.TrySet(DependencyStatus.Failed, DependencyStatus.Normal))
                        {
                            _workDict.TryRemove(work.ID, out _);
                            newlyFailed.Add(work);

                            if (visited.Add(work.ID))
                            {
                                stack.Push(work.ID);
                            }
                        }
                    }
                }
            }
        }

        private bool PrecedingWorkNotSuccessfullyCompleted(WorkID dependedId)
        {
            return _powerPool._resultDic.TryGetValue(dependedId, out ExecuteResultBase result)
                && result.Status != WorkStatus.Succeeded;
        }
    }
}
