using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.EventArguments
{
    public enum ErrorSource
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
        WorkDiscarded,
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
        public ErrorSource ErrorSource { get; internal set; }

        /// <summary>
        /// The result of the work's execution.
        /// </summary>
        public ExecuteResultBase ExecuteResult { get; internal set; }

        public ErrorOccurredEventArgs(Exception exception, ErrorSource errorSource, ExecuteResultBase executeResult)
        {
            if (executeResult != null)
            {
                ID = executeResult.ID;
                QueueDateTime = executeResult.UtcQueueDateTime;
                StartDateTime = executeResult.UtcStartDateTime;
                EndDateTime = executeResult.UtcEndDateTime;
                Duration = executeResult.Duration;
            }
            Exception = exception;
            ErrorSource = errorSource;
            ExecuteResult = executeResult;
        }
    }
}
