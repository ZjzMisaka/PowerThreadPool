using System;
using PowerThreadPool.Options;

namespace PowerThreadPool.Results
{
    public class ExecuteResult<TResult> : ExecuteResultBase
    {
        private TResult _result;
        /// <summary>
        /// Result of the work.
        /// </summary>
        public TResult Result { get => _result; internal set => _result = value; }

        internal ExecuteResult()
        {

        }

        internal override void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount)
        {
            if (result != null)
            {
                _result = (TResult)result;
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
            return _result;
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
    }
}
