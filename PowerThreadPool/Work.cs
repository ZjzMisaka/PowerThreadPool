using PowerThreadPool.Option;
using PowerThreadPool.Collections;
using System;
using System.Linq;
using System.Threading;
using static PowerThreadPool.PowerPool;

namespace PowerThreadPool
{
    internal abstract class WorkBase
    {
        private string id;
        public string ID { get => id; set => id = value; }
        private AutoResetEvent waitSignal;
        public AutoResetEvent WaitSignal { get => waitSignal; set => waitSignal = value; }
        private bool shouldStop;
        public bool ShouldStop { get => shouldStop; set => shouldStop = value; }
        private ManualResetEvent pauseSignal;
        public ManualResetEvent PauseSignal { get => pauseSignal; set => pauseSignal = value; }
        private bool isPausing;
        public bool IsPausing { get => isPausing; set => isPausing = value; }
        public abstract object Execute();
        public abstract void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
        internal abstract bool LongRunning { get; }
        internal abstract ConcurrentSet<string> Dependents { get; }
    }
    internal class Work<TResult> : WorkBase
    {
        private Func<object[], TResult> function;
        private object[] param;
        private WorkOption<TResult> workOption;
        private bool succeed = true;
        private CallbackEndEventHandler callbackEndHandler;

        internal override int WorkPriority { get => workOption.WorkPriority; }
        internal override ThreadPriority ThreadPriority { get => workOption.ThreadPriority; }
        internal override TimeoutOption WorkTimeoutOption { get => workOption.Timeout; }
        internal override bool LongRunning { get => workOption.LongRunning; }
        internal override ConcurrentSet<string> Dependents { get => workOption.Dependents; }

        public Work(PowerPool powerPool, string id, Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
        {
            this.ID = id;
            this.function = function;
            this.param = param;
            this.workOption = option;
            this.ShouldStop = false;
            this.IsPausing = false;

            this.callbackEndHandler = (workId) =>
            {
                if (!this.succeed)
                {
                    return;
                }

                foreach (string dependedId in this.workOption.Dependents)
                {
                    if (powerPool.failedWorkSet.Contains(dependedId))
                    {
                        this.succeed = false;
                        Interlocked.Decrement(ref powerPool.waitingWorkCount);
                        powerPool.CheckPoolIdle();
                        powerPool.CallbackEnd -= callbackEndHandler;
                        return;
                    }
                }

                if (this.workOption.Dependents.Remove(workId))
                {
                    if (this.workOption.Dependents.Count == 0)
                    {
                        powerPool.CallbackEnd -= callbackEndHandler;
                        powerPool.SetWork(this);
                    }
                }
            };

            if (this.workOption != null && this.workOption.Dependents != null && this.workOption.Dependents.Count != 0)
            {
                powerPool.CallbackEnd += callbackEndHandler;

                foreach (string dependedId in this.workOption.Dependents)
                {
                    if (!powerPool.settedWorkDic.ContainsKey(dependedId) && !powerPool.suspendedWork.ContainsKey(dependedId))
                    {
                        if (powerPool.failedWorkSet.Contains(dependedId))
                        {
                            this.succeed = false;
                            Interlocked.Decrement(ref powerPool.waitingWorkCount);
                            powerPool.CheckPoolIdle();
                            powerPool.CallbackEnd -= callbackEndHandler;
                            return;
                        }
                        else if (this.workOption.Dependents.Remove(dependedId))
                        {
                            if (this.workOption.Dependents.Count == 0)
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
