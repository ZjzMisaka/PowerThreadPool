using System;

namespace PowerThreadPool.Results
{
    public class ExecuteResult<TResult> : ExecuteResultBase
    {
        private TResult result;
        /// <summary>
        /// Result of the work.
        /// </summary>
        public TResult Result { get => result; internal set => result = value; }

        internal ExecuteResult()
        {

        }

        internal override void SetExecuteResult(object result, Exception exception, Status status)
        {
            if (result != null)
            {
                this.result = (TResult)result;
            }
            Exception = exception;
            Status = status;
        }

        internal override object GetResult()
        {
            return result;
        }

        internal override ExecuteResult<object> ToObjResult()
        {
            return new ExecuteResult<object>() { Exception = Exception, Result = Result, ID = ID };
        }
    }
}
