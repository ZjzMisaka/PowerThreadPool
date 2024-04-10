using PowerThreadPool.Options;
using PowerThreadPool.Collections;
using System;
using System.Linq;
using System.Threading;
using static PowerThreadPool.PowerPool;

/* プロジェクト 'PowerThreadPool (net5.0)' からのマージされていない変更
前:
using PowerThreadPool.Results;
後:
using PowerThreadPool.Results;
using PowerThreadPool;
using PowerThreadPool.Works;
*/
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
        internal override ThreadPriority ThreadPriority { get => workOption.ThreadPriority; }
        internal override TimeoutOption WorkTimeoutOption { get => workOption.Timeout; }
        internal override bool LongRunning { get => workOption.LongRunning; }
        internal override ConcurrentSet<string> Dependents { get => workOption.Dependents; }

        public Work(PowerPool powerPool, string id, Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
        {
            ID = id;
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
            return function(param);
        }

        public override void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption)
        {
            if (workOption.Callback != null)
            {
                workOption.Callback((ExecuteResult<TResult>)executeResult);
            }
            else if (powerPoolOption.DefaultCallback != null)
            {
                powerPoolOption.DefaultCallback(executeResult.ToObjResult());
            }
        }

        internal override ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status)
        {
            ExecuteResult<TResult> executeResult = new ExecuteResult<TResult>();
            executeResult.SetExecuteResult(result, exception, status);
            return executeResult;
        }
    }
}
