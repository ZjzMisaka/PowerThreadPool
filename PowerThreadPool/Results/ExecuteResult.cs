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

        internal override ExecuteResult<object> ToObjResult()
        {
            return new ExecuteResult<object>()
            {
                Exception = Exception,
                Result = Result,
                ID = ID,
                QueueDateTime = QueueDateTime
            };
        }

        internal override ExecuteResult<TRes> ToTypedResult<TRes>()
        {
            return new ExecuteResult<TRes>()
            {
                Exception = Exception,
                Result = (TRes)(object)Result,
                ID = ID,
                QueueDateTime = QueueDateTime
            };
        }
    }
}
