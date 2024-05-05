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
        internal string ID { get => id; set => id = value; }
        private Worker worker;
        internal Worker Worker { get => worker; set => worker = value; }
        internal int executeCount;
        internal int ExecuteCount { get => executeCount; set => executeCount = value; }
        private Status status;
        internal Status Status { get => status; set => status = value; }
        private AutoResetEvent waitSignal;
        internal AutoResetEvent WaitSignal { get => waitSignal; set => waitSignal = value; }
        private bool shouldStop;
        internal bool ShouldStop { get => shouldStop; set => shouldStop = value; }
        private ManualResetEvent pauseSignal;
        internal ManualResetEvent PauseSignal { get => pauseSignal; set => pauseSignal = value; }
        private bool isPausing;
        internal bool IsPausing { get => isPausing; set => isPausing = value; }
        private DateTime queueDateTime;
        internal DateTime QueueDateTime { get => queueDateTime; set => queueDateTime = value; }
        internal abstract object Execute();
        internal abstract bool Stop(bool forceStop);
        internal abstract bool Wait();
        internal abstract bool Pause();
        internal abstract bool Resume();
        internal abstract bool Cancel(bool lockWorker);

        /// <summary>
        /// Prevent work theft by other threads after acquiring a Worker.
        /// Prevent the forced termination of works that should not end, caused by the target work ending right after acquiring a Worker.
        /// </summary>
        /// <returns></returns>
        internal abstract Worker LockWorker();
        internal abstract void UnlockWorker(Worker worker);
        internal abstract void InvokeCallback(PowerPool powerPool, ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
        internal abstract bool ShouldRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldImmediateRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldRequeue(ExecuteResultBase executeResult);
        internal abstract string Group { get; }
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
        internal abstract RetryOption RetryOption { get; }
        internal abstract bool LongRunning { get; }
        internal abstract ConcurrentSet<string> Dependents { get; }
    }
}
