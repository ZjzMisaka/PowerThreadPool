using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Results;

namespace PowerThreadPool.Works
{
    internal abstract class WorkBase
    {
        private string _id;
        internal string ID { get => _id; set => _id = value; }
        private Worker _worker;
        internal Worker Worker { get => _worker; set => _worker = value; }
        internal int _executeCount;
        internal int ExecuteCount { get => _executeCount; set => _executeCount = value; }
        private Status _status;
        internal Status Status { get => _status; set => _status = value; }
        private AutoResetEvent _waitSignal;
        internal AutoResetEvent WaitSignal { get => _waitSignal; set => _waitSignal = value; }
        private bool _shouldStop;
        internal bool ShouldStop { get => _shouldStop; set => _shouldStop = value; }
        private ManualResetEvent _pauseSignal;
        internal ManualResetEvent PauseSignal { get => _pauseSignal; set => _pauseSignal = value; }
        private bool _isPausing;
        internal bool IsPausing { get => _isPausing; set => _isPausing = value; }
        private DateTime _queueDateTime;
        internal DateTime QueueDateTime { get => _queueDateTime; set => _queueDateTime = value; }
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
        internal abstract Worker LockWorker(bool holdWork);
        internal abstract void UnlockWorker(Worker worker, bool holdWork);
        internal abstract void InvokeCallback(PowerPool powerPool, ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
        internal abstract bool ShouldRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldImmediateRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldRequeue(ExecuteResultBase executeResult);
        internal abstract string Group { get; }
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract bool IsBackground { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
        internal abstract RetryOption RetryOption { get; }
        internal abstract bool LongRunning { get; }
        internal abstract ConcurrentSet<string> Dependents { get; }
    }
}
