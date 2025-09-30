using System;
using PowerThreadPool.Options;

namespace PowerThreadPool.Results
{
    public class ExecuteResult<TResult> : ExecuteResultBase
    {
        /// <summary>
        /// Result of the work.
        /// </summary>
        public TResult Result { get; internal set; }

        internal ExecuteResult()
        {
        }

        internal override void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount)
        {
            if (result != null)
            {
                Result = (TResult)result;
            }
            Exception = exception;
            Status = status;
            IsNotFound = false;
            QueueDateTime = queueDateTime;

            if (status == Status.Failed && retryOption != null)
            {
                RetryInfo = new RetryInfo()
                {
                    CurrentRetryCount = executeCount - 1,
                    MaxRetryCount = retryOption.MaxRetryCount,
                    RetryPolicy = retryOption.RetryPolicy,
                    StopRetry = false
                };
            }
        }

        internal override object GetResult()
        {
            return Result;
        }

        internal override ExecuteResult<TRes> ToTypedResult<TRes>()
        {
            ExecuteResult<TRes> result = this as ExecuteResult<TRes>;
            if (result == null)
            {
                result = new ExecuteResult<TRes>()
                {
                    Exception = Exception,
                    IsNotFound = false,
                    Result = Result != null ? (TRes)(object)Result : default,
                    ID = ID,
                    QueueDateTime = UtcQueueDateTime
                };
            }
            return result;
        }
    }
}
