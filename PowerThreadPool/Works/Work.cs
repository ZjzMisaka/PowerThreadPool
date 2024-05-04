using PowerThreadPool.Options;
using PowerThreadPool.Collections;
using System;
using System.Linq;
using System.Threading;
using static PowerThreadPool.PowerPool;
using PowerThreadPool.Results;
using PowerThreadPool.Constants;

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

        public override bool Stop(bool forceStop)
        {
            Worker workerTemp = LockWorker();

            bool res;
            if (forceStop)
            {
                if (Worker.WorkID == ID)
                { 
                    Worker.ForceStop(false);
                    res = true;
                }
                else
                {
                    res = Cancel(false);
                }
            }
            else
            {
                ShouldStop = true;
                Cancel(false);
                res = true;
            }

            UnlockWorker(workerTemp);

            return res;
        }

        public override bool Wait()
        {
            if (WaitSignal == null)
            {
                WaitSignal = new AutoResetEvent(false);
            }
            WaitSignal.WaitOne();
            return true;
        }

        public override bool Pause()
        {
            if (PauseSignal == null)
            {
                PauseSignal = new ManualResetEvent(true);
            }

            IsPausing = true;
            PauseSignal.Reset();
            return true;
        }

        public override bool Resume()
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

        public override bool Cancel(bool lockWorker)
        {
            Worker workerTemp = null;
            if (lockWorker)
            {
                workerTemp = LockWorker();
            }
            bool res = Worker.Cancel(ID);
            if (lockWorker)
            {
                UnlockWorker(workerTemp);
            }
            return res;
        }

        public override Worker LockWorker()
        {
            Worker workerTemp = null;
            do
            {
                if (workerTemp != null)
                {
                    SpinWait.SpinUntil(() =>
                    {
                        int stealingLockOrig = Interlocked.CompareExchange(ref workerTemp.stealingLock, WorkerStealingFlags.Unlocked, WorkerStealingFlags.Locked);
                        return (stealingLockOrig == WorkerStealingFlags.Locked);
                    });
                    SpinWait.SpinUntil(() =>
                    {
                        int doneSpinOrig = Interlocked.CompareExchange(ref workerTemp.workHeld, WorkHeldFlags.NotHeld, WorkHeldFlags.Held);
                        return (doneSpinOrig == 1);
                    });
                }
                SpinWait.SpinUntil(() =>
                {
                    workerTemp = Worker;
                    return (workerTemp != null);
                });
                SpinWait.SpinUntil(() =>
                {
                    int stealingLockOrig = Interlocked.CompareExchange(ref workerTemp.stealingLock, WorkerStealingFlags.Locked, WorkerStealingFlags.Unlocked);
                    return (stealingLockOrig == WorkerStealingFlags.Unlocked);
                });
                SpinWait.SpinUntil(() =>
                {
                    int doneSpinOrig = Interlocked.CompareExchange(ref workerTemp.workHeld, WorkHeldFlags.Held, WorkHeldFlags.NotHeld);
                    return (doneSpinOrig == 0);
                });
            }
            while (Worker == null || (Worker != null && Worker.ID != workerTemp.ID));

            return workerTemp;
        }

        public override void UnlockWorker(Worker worker)
        {
            SpinWait.SpinUntil(() =>
            {
                int stealingLockOrig = Interlocked.CompareExchange(ref worker.stealingLock, WorkerStealingFlags.Unlocked, WorkerStealingFlags.Locked);
                return (stealingLockOrig == WorkerStealingFlags.Locked);
            });
            SpinWait.SpinUntil(() =>
            {
                int doneSpinOrig = Interlocked.CompareExchange(ref worker.workHeld, WorkHeldFlags.NotHeld, WorkHeldFlags.Held);
                return (doneSpinOrig == 1);
            });
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
