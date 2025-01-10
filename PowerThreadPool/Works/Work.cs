using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using static PowerThreadPool.PowerPool;

namespace PowerThreadPool.Works
{
    internal class Work<TResult> : WorkBase
    {
        private Func<object[], TResult> _function;
        private object[] _param;
        private WorkOption<TResult> _workOption;
        private CallbackEndEventHandler _callbackEndHandler;

        internal ExecuteResult<TResult> _executeResult;
        internal ExecuteResult<TResult> ExecuteResult
        {
            get => _executeResult;
            set => _executeResult = value;
        }
        internal override object[] Parameter
        {
            get => Parameter;
        }

        internal override string Group
        {
            get => _workOption.Group;
            set => _workOption.Group = value;
        }
        internal override ThreadPriority ThreadPriority => _workOption.ThreadPriority;
        internal override bool IsBackground => _workOption.IsBackground;
        internal override int WorkPriority => _workOption.WorkPriority;
        internal override TimeoutOption WorkTimeoutOption => _workOption.TimeoutOption;
        internal override RetryOption RetryOption => _workOption.RetryOption;
        internal override bool LongRunning => _workOption.LongRunning;
        internal override bool ShouldStoreResult => _workOption.ShouldStoreResult;
        internal override ConcurrentSet<string> Dependents => _workOption.Dependents;

        internal Work(PowerPool powerPool, string id, Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
        {
            ID = id;
            ExecuteCount = 0;
            _function = function;
            _param = param;
            _workOption = option;
            ShouldStop = false;
            IsPausing = false;

            _callbackEndHandler = (workId) =>
            {
                foreach (string dependedId in _workOption.Dependents)
                {
                    if (PrecedingWorkNotSuccessfullyCompleted(powerPool, dependedId))
                    {
                        powerPool.CallbackEnd -= _callbackEndHandler;
                        DependencyFailed = true;
                        Interlocked.Decrement(ref powerPool._waitingWorkCount);
                        powerPool.CheckPoolIdle();
                        return;
                    }
                }

                if (_workOption.Dependents.Remove(workId))
                {
                    if (_workOption.Dependents.Count == 0)
                    {
                        powerPool.CallbackEnd -= _callbackEndHandler;
                        powerPool.SetWork(this);
                    }
                }
            };

            if (_workOption != null && _workOption.Dependents != null && _workOption.Dependents.Count != 0)
            {
                powerPool.CallbackEnd += _callbackEndHandler;

                foreach (string dependedId in _workOption.Dependents)
                {
                    if (!powerPool._aliveWorkDic.ContainsKey(dependedId) && !powerPool._suspendedWork.ContainsKey(dependedId))
                    {
                        if (PrecedingWorkNotSuccessfullyCompleted(powerPool, dependedId))
                        {
                            powerPool.CallbackEnd -= _callbackEndHandler;
                            DependencyFailed = true;
                            powerPool.CheckPoolIdle();
                            return;
                        }
                        else if (_workOption.Dependents.Remove(dependedId))
                        {
                            if (_workOption.Dependents.Count == 0)
                            {
                                powerPool.CallbackEnd -= _callbackEndHandler;
                                // No need to call powerPool.SetWork here
                            }
                        }
                    }
                }
            }
        }

        private bool PrecedingWorkNotSuccessfullyCompleted(PowerPool powerPool, string dependedId)
        {
            return powerPool._failedWorkSet.Contains(dependedId) || powerPool._canceledWorkSet.Contains(dependedId);
        }

        internal override object Execute()
        {
            ++_executeCount;
            return _function(_param);
        }

        internal override bool Stop(bool forceStop, bool cancel = true)
        {
            bool res = false;

            if (forceStop)
            {
                // Ensure that the executing Work is not switched and the target Work is not stolen during the operation of the Worker
                using (new WorkGuard(this, true))
                {
                    if (Worker != null)
                    {
                        if (Worker.WorkID == ID)
                        {
                            if (Worker.CanForceStop.TrySet(CanForceStop.NotAllowed, CanForceStop.Allowed))
                            {
                                Worker.ForceStop(false);
                            }
                            res = true;
                        }
                        else
                        {
                            if (cancel)
                            {
                                res = Cancel(false);
                            }
                        }
                    }
                }
            }
            else
            {
                ShouldStop = true;
                if (cancel)
                {
                    Cancel(true);
                }
                res = true;
            }

            return res;
        }

        internal override bool Wait()
        {
            if (WaitSignal == null)
            {
                WaitSignal = new AutoResetEvent(false);
            }

            if (!IsDone)
            {
                WaitSignal.WaitOne();
            }

            return true;
        }

        internal override ExecuteResult<T> Fetch<T>()
        {
            Wait();

            return ExecuteResult.ToTypedResult<T>();
        }

        internal override bool Pause()
        {
            if (PauseSignal == null)
            {
                PauseSignal = new ManualResetEvent(true);
            }

            IsPausing = true;
            PauseSignal.Reset();
            return true;
        }

        internal override bool Resume()
        {
            bool res = false;
            if (IsPausing)
            {
                IsPausing = false;
                PauseSignal.Set();
                res = true;
            }
            return res;
        }

        internal override bool Cancel(bool needFreeze)
        {
            // Ensure that the target Work is not stolen during the operation of the Worker
            using (new WorkGuard(this, needFreeze))
            {
                bool res = false;
                if (Worker != null)
                {
                    res = Worker.Cancel(ID);
                }
                return res;
            }
        }

        internal override void InvokeCallback(PowerPool powerPool, ExecuteResultBase executeResult, PowerPoolOption powerPoolOption)
        {
            if (_workOption.Callback != null)
            {
                powerPool.SafeCallback(_workOption.Callback, EventArguments.ErrorFrom.Callback, executeResult, Parameter);
            }
            else if (powerPoolOption.DefaultCallback != null)
            {
                powerPool.SafeCallback(powerPoolOption.DefaultCallback, EventArguments.ErrorFrom.DefaultCallback, executeResult, Parameter);
            }
        }

        internal override ExecuteResultBase SetExecuteResult(PowerPool powerPool, object result, Exception exception, Status status)
        {
            Status = status;
            ExecuteResult<TResult> executeResult = new ExecuteResult<TResult>();
            executeResult.SetExecuteResult(result, exception, status, QueueDateTime, RetryOption, ExecuteCount);
            ExecuteResult = executeResult;
            if (_workOption.ShouldStoreResult)
            {
                powerPool._resultDic[ID] = ExecuteResult;
            }
            return executeResult;
        }

        internal override bool ShouldRetry(ExecuteResultBase executeResult)
        {
            if (executeResult != null && executeResult.RetryInfo != null && executeResult.RetryInfo.StopRetry)
            {
                return false;
            }
            else if (_workOption.RetryOption != null && Status == Status.Failed && ((_workOption.RetryOption.RetryPolicy == RetryPolicy.Limited && ExecuteCount - 1 < _workOption.RetryOption.MaxRetryCount) || _workOption.RetryOption.RetryPolicy == RetryPolicy.Unlimited))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal override bool ShouldImmediateRetry(ExecuteResultBase executeResult)
        {
            return ShouldRetry(executeResult) && _workOption.RetryOption.RetryBehavior == RetryBehavior.ImmediateRetry;
        }

        internal override bool ShouldRequeue(ExecuteResultBase executeResult)
        {
            return ShouldRetry(executeResult) && _workOption.RetryOption.RetryBehavior == RetryBehavior.Requeue;
        }

        public override void Dispose()
        {
            if (PauseSignal != null)
            {
                PauseSignal.Dispose();
            }
            if (WaitSignal != null)
            {
                WaitSignal.Dispose();
            }
        }
    }
}
