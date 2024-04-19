using System;
using PowerThreadPool.Options;

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

        internal override void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount)
        {
            if (result != null)
            {
                this.result = (TResult)result;
            }
            Exception = exception;
            Status = status;
            QueueDateTime = queueDateTime;

            if (status == Status.Failed && retryOption != null)
            {
                RetryInfo = new RetryInfo() { CurrentRetryCount = executeCount - 1, MaxRetryCount = retryOption.MaxRetryCount, RetryPolicy = retryOption.RetryPolicy, StopRetry = false };
            }
        }

        internal override object GetResult()
        {
            return result;
        }

        internal override ExecuteResult<object> ToObjResult()
        {
            return new ExecuteResult<object>() { Exception = Exception, Result = Result, ID = ID, QueueDateTime =  QueueDateTime };
        }
    }
}
