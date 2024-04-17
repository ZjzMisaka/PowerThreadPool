using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Results;

namespace PowerThreadPool.Works
{
    internal abstract class WorkBase
    {
        private string id;
        public string ID { get => id; set => id = value; }
        private AutoResetEvent waitSignal;
        public AutoResetEvent WaitSignal { get => waitSignal; set => waitSignal = value; }
        private bool shouldStop;
        public bool ShouldStop { get => shouldStop; set => shouldStop = value; }
        private ManualResetEvent pauseSignal;
        public ManualResetEvent PauseSignal { get => pauseSignal; set => pauseSignal = value; }
        private bool isPausing;
        public bool IsPausing { get => isPausing; set => isPausing = value; }
        private DateTime queueDateTime;
        public DateTime QueueDateTime { get => queueDateTime; internal set => queueDateTime = value; }
        public abstract object Execute();
        public abstract void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
        internal abstract string Group { get; }
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
        internal abstract bool LongRunning { get; }
        internal abstract ConcurrentSet<string> Dependents { get; }
    }
}
