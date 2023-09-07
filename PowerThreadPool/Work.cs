using PowerThreadPool.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    internal abstract class WorkBase
    {
        private string id;
        public string ID { get => id; set => id = value; }
        public abstract object Execute();
        public abstract void InvokeCallback(ExecuteResultBase executeResult, ThreadPoolOption threadPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
    }
    internal class Work<TResult> : WorkBase
    {
        private Func<object[], TResult> function;
        private object[] param;
        private WorkOption<TResult> option;
        public Func<object[], TResult> Function { get => function; set => function = value; }
        public object[] Param { get => param; set => param = value; }
        public WorkOption<TResult> Option { get => option; set => option = value; }

        private object lockObj = new object();

        public Work(PowerPool powerPool, string id, Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
        {
            this.ID = id;
            this.function = function;
            this.param = param;
            this.option = option;

            if (this.option != null && this.option.Dependents != null && this.option.Dependents.Count != 0)
            {
                powerPool.CallbackEnd += (workId) =>
                {
                    lock (lockObj)
                    {
                        if (this.option.Dependents.TryRemove(workId))
                        {
                            if (this.option.Dependents.Count == 0)
                            {
                                powerPool.SetWorkIntoWaitingQueue<TResult>(id);
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

        public override void InvokeCallback(ExecuteResultBase executeResult, ThreadPoolOption threadPoolOption)
        {
            if (Option.Callback != null)
            {
                Option.Callback((ExecuteResult<TResult>)executeResult);
            }
            else if (threadPoolOption.DefaultCallback != null)
            {
                threadPoolOption.DefaultCallback(executeResult.ToObjResult());
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
