using PowerThreadPool.Option;
using System;
using System.Linq;
using System.Threading;

namespace PowerThreadPool
{
    internal abstract class WorkBase
    {
        private string id;
        public string ID { get => id; set => id = value; }
        private bool longRunning;
        public bool LongRunning { get => longRunning; set => longRunning = value; }
        public abstract object Execute();
        public abstract void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
    }
    internal class Work<TResult> : WorkBase
    {
        private Func<object[], TResult> function;
        private object[] param;
        private WorkOption<TResult> workOption;
        private bool succeed = true;

        private object lockObj = new object();

        internal override int WorkPriority { get => workOption.WorkPriority; }
        internal override ThreadPriority ThreadPriority { get => workOption.ThreadPriority; }
        internal override TimeoutOption WorkTimeoutOption { get => workOption.Timeout; }

        public Work(PowerPool powerPool, string id, Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
        {
            this.ID = id;
            this.function = function;
            this.param = param;
            this.workOption = option;

            if (this.workOption != null && this.workOption.Dependents != null && this.workOption.Dependents.Count != 0)
            {
                powerPool.CallbackEnd += (workId) =>
                {
                    lock (lockObj)
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
                                return;
                            }
                        }

                        if (this.workOption.Dependents.Remove(workId))
                        {
                            if (this.workOption.Dependents.Count == 0)
                            {
                                powerPool.SetWork(this);
                            }
                        }
                    }
                };
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
