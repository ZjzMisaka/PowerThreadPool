using PowerThreadPool.Options;
using PowerThreadPool.Collections;
using System;
using System.Linq;
using System.Threading;
using static PowerThreadPool.PowerPool;
using PowerThreadPool.Results;

namespace PowerThreadPool.Works
{
    internal class Work<TResult> : WorkBase
    {
        private Func<object[], TResult> function;
        private object[] param;
        private WorkOption<TResult> workOption;
        private CallbackEndEventHandler callbackEndHandler;

        internal override int WorkPriority { get => workOption.WorkPriority; }
        internal override string Group { get => workOption.Group; }
        internal override ThreadPriority ThreadPriority { get => workOption.ThreadPriority; }
        internal override TimeoutOption WorkTimeoutOption { get => workOption.TimeoutOption; }
        internal override RetryOption RetryOption { get => workOption.RetryOption; }
        internal override bool LongRunning { get => workOption.LongRunning; }
        internal override ConcurrentSet<string> Dependents { get => workOption.Dependents; }

        public Work(PowerPool powerPool, string id, Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
        {
            ID = id;
            ExecuteCount = 0;
            this.function = function;
            this.param = param;
            workOption = option;
            ShouldStop = false;
            IsPausing = false;

            callbackEndHandler = (workId) =>
            {
                foreach (string dependedId in workOption.Dependents)
                {
                    if (powerPool.failedWorkSet.Contains(dependedId))
                    {
                        powerPool.CallbackEnd -= callbackEndHandler;
                        Interlocked.Decrement(ref powerPool.waitingWorkCount);
                        powerPool.CheckPoolIdle();
                        return;
                    }
                }

                if (workOption.Dependents.Remove(workId))
                {
                    if (workOption.Dependents.Count == 0)
                    {
                        powerPool.CallbackEnd -= callbackEndHandler;
                        powerPool.SetWork(this);
                    }
                }
            };

            if (workOption != null && workOption.Dependents != null && workOption.Dependents.Count != 0)
            {
                powerPool.CallbackEnd += callbackEndHandler;

                foreach (string dependedId in workOption.Dependents)
                {
                    if (!powerPool.settedWorkDic.ContainsKey(dependedId) && !powerPool.suspendedWork.ContainsKey(dependedId))
                    {
                        if (powerPool.failedWorkSet.Contains(dependedId))
                        {
                            powerPool.CallbackEnd -= callbackEndHandler;
                            Interlocked.Decrement(ref powerPool.waitingWorkCount);
                            powerPool.CheckPoolIdle();
                            return;
                        }
                        else if (workOption.Dependents.Remove(dependedId))
                        {
                            if (workOption.Dependents.Count == 0)
                            {
                                powerPool.CallbackEnd -= callbackEndHandler;
                                // No need to call powerPool.SetWork here
                            }
                        }
                    }
                }
            }
        }

        public override object Execute()
        {
            Interlocked.Increment(ref executeCount);
            return function(param);
        }

        public override void InvokeCallback(PowerPool powerPool, ExecuteResultBase executeResult, PowerPoolOption powerPoolOption)
        {
            if (workOption.Callback != null)
            {
                powerPool.SafeCallback(workOption.Callback, EventArguments.ErrorFrom.Callback, executeResult);
            }
            else if (powerPoolOption.DefaultCallback != null)
            {
                powerPool.SafeCallback(powerPoolOption.DefaultCallback, EventArguments.ErrorFrom.DefaultCallback, executeResult.ToObjResult());
            }
        }

        internal override ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status)
        {
            Status = status;
            ExecuteResult<TResult> executeResult = new ExecuteResult<TResult>();
            executeResult.SetExecuteResult(result, exception, status, QueueDateTime, RetryOption, ExecuteCount);
            return executeResult;
        }

        internal override bool ShouldImmediateRetry(ExecuteResultBase executeResult)
        {
            if (executeResult != null && executeResult.RetryInfo != null && executeResult.RetryInfo.StopRetry)
            {
                return false;
            }
            else if (workOption.RetryOption != null && Status == Status.Failed && ((workOption.RetryOption.RetryBehavior == RetryBehavior.ImmediateRetry && workOption.RetryOption.RetryPolicy == RetryPolicy.Limited && ExecuteCount - 1 < workOption.RetryOption.MaxRetryCount) || workOption.RetryOption.RetryBehavior == RetryBehavior.ImmediateRetry && workOption.RetryOption.RetryPolicy == RetryPolicy.Unlimited))
            {
                return true;
            }
            return false;
        }

        internal override bool ShouldRequeue(ExecuteResultBase executeResult)
        {
            if (executeResult != null && executeResult.RetryInfo != null && executeResult.RetryInfo.StopRetry)
            {
                return false;
            }
            if (workOption.RetryOption != null && Status == Status.Failed && ((workOption.RetryOption.RetryBehavior == RetryBehavior.Requeue && workOption.RetryOption.RetryPolicy == RetryPolicy.Limited && ExecuteCount - 1 < workOption.RetryOption.MaxRetryCount) || workOption.RetryOption.RetryPolicy == RetryPolicy.Unlimited))
            {
                return true;
            }
            return false;
        }
    }
}
