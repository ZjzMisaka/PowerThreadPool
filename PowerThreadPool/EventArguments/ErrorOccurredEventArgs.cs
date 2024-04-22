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
        private Exception exception;
        public Exception Exception { get => exception; internal set => exception = value; }

        private ErrorFrom errorFrom;
        public ErrorFrom ErrorFrom { get => errorFrom; internal set => errorFrom = value; }

        private ExecuteResultBase executeResult;
        public ExecuteResultBase ExecuteResult { get => executeResult; internal set => executeResult = value; }
    }
}
