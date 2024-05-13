using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.EventArguments
{
    public enum ErrorFrom
    {
        Callback,
        DefaultCallback,
        PoolStarted,
        PoolIdled,
        WorkStarted,
        WorkEnded,
        PoolTimedOut,
        WorkTimedOut,
        WorkStopped,
        WorkCanceled,
        WorkLogic,
    }

    public class ErrorOccurredEventArgs : WorkEventArgsBase
    {
        public Exception Exception { get; internal set; }

        public ErrorFrom ErrorFrom { get; internal set; }

        public ExecuteResultBase ExecuteResult { get; internal set; }

        public ErrorOccurredEventArgs(Exception exception, ErrorFrom errorFrom, ExecuteResultBase executeResult)
        {
            if (executeResult != null)
            {
                ID = executeResult.ID;
                QueueDateTime = executeResult.QueueDateTime;
                StartDateTime = executeResult.StartDateTime;
                EndDateTime = executeResult.EndDateTime;
            }
            Exception = exception;
            ErrorFrom = errorFrom;
            ExecuteResult = executeResult;
        }
    }
}
