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
    }
    internal class Work<TResult> : WorkBase
    {
        private Func<object[], TResult> function;
        private object[] param;
        private ThreadOption<TResult> option;
        public Func<object[], TResult> Function { get => function; set => function = value; }
        public object[] Param { get => param; set => param = value; }
        public ThreadOption<TResult> Option { get => option; set => option = value; }

        public Work(string id, Func<object[], TResult> function, object[] param, ThreadOption<TResult> option)
        {
            this.ID = id;
            this.function = function;
            this.param = param;
            this.option = option;
        }
        public override object Execute()
        {
            return function(param);
        }
        public override void InvokeCallback(ExecuteResultBase executeResult, ThreadPoolOption threadPoolOption)
        {
            if (Option.Callback != null)
            {
                Option.Callback(ExecuteResult<TResult>.FromBase(executeResult));
            }
            else if (threadPoolOption.DefaultCallback != null)
            {
                threadPoolOption.DefaultCallback(executeResult as ExecuteResult<object>);
            }
        }
    }
}
