using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.EventArguments
{
    public class WorkEndedEventArgs : WorkEventArgsBase
    {
        public WorkEndedEventArgs() { }

        private object _result;
        public object Result { get => _result; internal set => _result = value; }

        private bool _succeed;
        public bool Succeed { get => _succeed; internal set => _succeed = value; }

        private Exception _exception;
        public Exception Exception { get => _exception; internal set => _exception = value; }

        private RetryInfo _retryInfo;
        public RetryInfo RetryInfo { get => _retryInfo; internal set => _retryInfo = value; }
    }
}
