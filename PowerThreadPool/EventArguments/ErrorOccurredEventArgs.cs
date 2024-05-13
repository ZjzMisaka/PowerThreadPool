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
        private Exception _exception;
        public Exception Exception { get => _exception; internal set => _exception = value; }

        private ErrorFrom _errorFrom;
        public ErrorFrom ErrorFrom { get => _errorFrom; internal set => _errorFrom = value; }

        private ExecuteResultBase _executeResult;
        public ExecuteResultBase ExecuteResult { get => _executeResult; internal set => _executeResult = value; }

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
