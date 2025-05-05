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
        RunningWorkerCountChanged,
        WorkStarted,
        WorkEnded,
        PoolTimedOut,
        WorkTimedOut,
        WorkStopped,
        WorkCanceled,
        WorkRejected,
        WorkLogic,
    }

    public class ErrorOccurredEventArgs : WorkEventArgsBase
    {
        /// <summary>
        /// The uncaught exception that occurred.
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// The location where the error occurred.
        /// </summary>
        public ErrorFrom ErrorFrom { get; internal set; }

        /// <summary>
        /// The result of the work's execution.
        /// </summary>
        public ExecuteResultBase ExecuteResult { get; internal set; }

        public ErrorOccurredEventArgs(Exception exception, ErrorFrom errorFrom, ExecuteResultBase executeResult)
        {
            if (executeResult != null)
            {
                ID = executeResult.ID;
                QueueDateTime = executeResult.UtcQueueDateTime;
                StartDateTime = executeResult.UtcStartDateTime;
                EndDateTime = executeResult.UtcEndDateTime;
            }
            Exception = exception;
            ErrorFrom = errorFrom;
            ExecuteResult = executeResult;
        }
    }
}
