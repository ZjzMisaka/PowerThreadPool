using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Results;

namespace PowerThreadPool.Works
{
    internal abstract class WorkBase
    {
        internal string ID { get; set; }
        internal Worker Worker { get; set; }
        internal volatile int _executeCount;
        internal int ExecuteCount
        {
            get => _executeCount;
            set => _executeCount = value;
        }
        internal volatile bool _isDone;
        internal bool IsDone
        {
            get => _isDone;
            set => _isDone = value;
        }
        internal volatile bool _isPausing;
        internal bool IsPausing
        {
            get => _isPausing;
            set => _isPausing = value;
        }
        internal Status Status { get; set; }
        internal AutoResetEvent WaitSignal { get; set; }
        internal bool ShouldStop { get; set; }
        internal ManualResetEvent PauseSignal { get; set; }
        internal DateTime QueueDateTime { get; set; }
        internal abstract object Execute();
        internal abstract bool Stop(bool forceStop);
        internal abstract bool Wait();
        internal abstract ExecuteResultBase Fetch();
        internal abstract bool Pause();
        internal abstract bool Resume();
        internal abstract bool Cancel(bool needFreeze);
        internal abstract void InvokeCallback(PowerPool powerPool, ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(PowerPool powerPool, object result, Exception exception, Status status);
        internal abstract bool ShouldRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldImmediateRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldRequeue(ExecuteResultBase executeResult);
        internal abstract string Group { get; set; }
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract bool IsBackground { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
        internal abstract RetryOption RetryOption { get; }
        internal abstract bool LongRunning { get; }
        internal abstract ConcurrentSet<string> Dependents { get; }
    }
}
