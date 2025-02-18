using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    /// <summary>
    /// A comprehensive and efficient lock-free thread pool with granular work control, flexible concurrency, and robust error handling, alongside an easy-to-use API for diverse work submissions.
    /// </summary>
    public partial class PowerPool : IDisposable
    {
        internal bool _disposed = false;
        internal bool _disposing = false;

        private readonly ManualResetEvent _waitAllSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _pauseSignal = new ManualResetEvent(true);
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        internal ConcurrentSet<string> _failedWorkSet = new ConcurrentSet<string>();
        internal ConcurrentSet<string> _canceledWorkSet = new ConcurrentSet<string>();

        internal ConcurrentDictionary<int, Worker> _idleWorkerDic = new ConcurrentDictionary<int, Worker>();
        internal ConcurrentQueue<int> _idleWorkerQueue = new ConcurrentQueue<int>();

        internal ConcurrentDictionary<string, WorkBase> _aliveWorkDic = new ConcurrentDictionary<string, WorkBase>();
        internal ConcurrentDictionary<string, ConcurrentSet<string>> _workGroupDic = new ConcurrentDictionary<string, ConcurrentSet<string>>();
        internal ConcurrentDictionary<string, ConcurrentSet<string>> _groupRelationDic = new ConcurrentDictionary<string, ConcurrentSet<string>>();
        internal ConcurrentDictionary<int, Worker> _aliveWorkerDic = new ConcurrentDictionary<int, Worker>();
        internal IEnumerable<Worker> _aliveWorkerList = new List<Worker>();

        internal ConcurrentQueue<string> _suspendedWorkQueue = new ConcurrentQueue<string>();
        internal ConcurrentDictionary<string, WorkBase> _suspendedWork = new ConcurrentDictionary<string, WorkBase>();
        internal ConcurrentQueue<string> _stopSuspendedWorkQueue = new ConcurrentQueue<string>();
        internal ConcurrentDictionary<string, WorkBase> _stopSuspendedWork = new ConcurrentDictionary<string, WorkBase>();

        internal ConcurrentDictionary<string, ExecuteResultBase> _resultDic = new ConcurrentDictionary<string, ExecuteResultBase>();

        internal long _startCount = 0;
        internal long _endCount = 0;
        internal long _queueTime = 0;
        internal long _executeTime = 0;

        private DateTime _startDateTime;
        private DateTime _endDateTime;

        private readonly InterlockedFlag<CanCreateNewWorker> _canCreateNewWorker = CanCreateNewWorker.Allowed;
        internal readonly InterlockedFlag<CanDeleteRedundantWorker> _canDeleteRedundantWorker = CanDeleteRedundantWorker.Allowed;

        private PowerPoolOption _powerPoolOption;
        public PowerPoolOption PowerPoolOption
        {
            get => _powerPoolOption;
            set
            {
                if (_powerPoolOption != null)
                {
                    _powerPoolOption.PowerPoolList.Remove(this);
                }
                else
                {
                    value.PowerPoolList.Add(this);
                }

                _powerPoolOption = value;
                FillWorkerQueue();
            }
        }

        private DeferredActionTimer _runningTimer;
        private DeferredActionTimer _timeoutTimer;

        private readonly InterlockedFlag<PoolStates> _poolState = PoolStates.NotRunning;

        public bool PoolRunning => _poolState == PoolStates.Running;

        private bool _poolStopping = false;
        public bool PoolStopping { get => _poolStopping; }

        private bool _enablePoolIdleCheck = true;
        /// <summary>
        /// Indicates whether to perform pool idle check.
        /// </summary>
        public bool EnablePoolIdleCheck
        {
            get => _enablePoolIdleCheck;
            set
            {
                _enablePoolIdleCheck = value;
                if (_enablePoolIdleCheck)
                {
                    CheckPoolIdle();
                }
            }
        }

        internal int _idleWorkerCount = 0;
        public int IdleWorkerCount => _idleWorkerCount;

        internal int _waitingWorkCount = 0;
        public int WaitingWorkCount => _waitingWorkCount;

        public IEnumerable<string> WaitingWorkList
        {
            get
            {
                List<string> list = _aliveWorkDic.Keys.ToList();
                IEnumerable<Worker> workers = _aliveWorkerList;
                foreach (Worker worker in workers)
                {
                    if (worker.WorkerState == WorkerStates.Running)
                    {
                        list.Remove(worker.WorkID);
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Failed work count
        /// Will be reset to zero when the thread pool starts again
        /// </summary>
        public int FailedWorkCount => _failedWorkSet.Count;

        /// <summary>
        /// ID list of failed works
        /// Will be cleared when the thread pool starts again
        /// </summary>
        public IEnumerable<string> FailedWorkList => _failedWorkSet;

        internal int _runningWorkerCount = 0;
        public int RunningWorkerCount => _runningWorkerCount;

        internal int _aliveWorkerCount = 0;
        public int AliveWorkerCount => _aliveWorkerCount;

        internal int _longRunningWorkerCount = 0;
        public int LongRunningWorkerCount => _longRunningWorkerCount;

        /// <summary>
        /// The total time spent in the queue (ms).
        /// Will be reset when the thread pool starts again.
        /// </summary>
        public long TotalQueueTime => _queueTime;

        /// <summary>
        /// The total time taken for execution (ms).
        /// Will be reset when the thread pool starts again.
        /// </summary>
        public long TotalExecuteTime => _executeTime;

        /// <summary>
        /// The average time spent in the queue (ms).
        /// Will be reset when the thread pool starts again.
        /// </summary>
        public long AverageQueueTime
        {
            get
            {
                if (_startCount == 0)
                {
                    return 0;
                }
                return _queueTime / _startCount;
            }
        }

        /// <summary>
        /// The average time taken for execution (ms).
        /// Will be reset when the thread pool starts again.
        /// </summary>
        public long AverageExecuteTime
        {
            get
            {
                if (_endCount == 0)
                {
                    return 0;
                }
                return _executeTime / _endCount;
            }
        }

        /// <summary>
        /// The average elapsed time from start queue to finish (ms).
        /// Will be reset when the thread pool starts again.
        /// </summary>
        public long AverageElapsedTime => AverageQueueTime + AverageExecuteTime;

        /// <summary>
        /// The total elapsed time from start queue to finish (ms).
        /// Will be reset when the thread pool starts again.
        /// </summary>
        public long TotalElapsedTime => TotalQueueTime + TotalExecuteTime;

        /// <summary>
        /// Pool runtime duration.
        /// Will be reset when the thread pool starts again.
        /// </summary>
        public TimeSpan RuntimeDuration
        {
            get
            {
                TimeSpan runtimeDuration = TimeSpan.MinValue;
                if (_poolState == PoolStates.Running)
                {
                    runtimeDuration = DateTime.UtcNow - _startDateTime;
                }
                else if (_endDateTime != DateTime.MinValue)
                {
                    runtimeDuration = _endDateTime - _startDateTime;
                }

                if (runtimeDuration.Ticks > 0)
                {
                    return runtimeDuration;
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public PowerPool()
        {
            _timeoutTimer = new DeferredActionTimer(() =>
            {
                if (PoolTimedOut != null)
                {
                    SafeInvoke(PoolTimedOut, new EventArgs(), ErrorFrom.PoolTimedOut, null);
                }
                Stop(PowerPoolOption.TimeoutOption.ForceStop);
            });
            _runningTimer = new DeferredActionTimer(() =>
            {
                RunningTimerElapsedEventArgs runningTimerElapsedEventArgs = new RunningTimerElapsedEventArgs
                {
                    RuntimeDuration = RuntimeDuration,
                    SignalTime = DateTime.UtcNow,
                };
                PowerPoolOption.RunningTimerOption.Elapsed(runningTimerElapsedEventArgs);
            }, true);
        }

        public PowerPool(PowerPoolOption powerPoolOption) : this()
        {
            PowerPoolOption = powerPoolOption;
        }

        /// <summary>
        /// Start the pool, but only if PowerPoolOption.StartSuspended is set to true.
        /// </summary>
        public void Start()
        {
            CheckDisposed();

            while (_suspendedWorkQueue.TryDequeue(out string key))
            {
                if (_suspendedWork.TryGetValue(key, out WorkBase work))
                {
                    ConcurrentSet<string> dependents = work.Dependents;
                    if (dependents == null || dependents.Count == 0)
                    {
                        if (PoolStopping)
                        {
                            _stopSuspendedWork[work.ID] = work;
                            _stopSuspendedWorkQueue.Enqueue(work.ID);
                            Interlocked.Decrement(ref _waitingWorkCount);
                        }
                        else
                        {
                            SetWork(work);
                        }
                    }
                }
            }
            _suspendedWork.Clear();
#if NET5_0_OR_GREATER
            _suspendedWorkQueue.Clear();
#else
            _suspendedWorkQueue = new ConcurrentQueue<string>();
#endif
        }

        /// <summary>
        /// Fill worker queue
        /// </summary>
        internal void FillWorkerQueue()
        {
            int minThreads = PowerPoolOption.MaxThreads;
            if (PowerPoolOption.DestroyThreadOption != null)
            {
                minThreads = PowerPoolOption.DestroyThreadOption.MinThreads;
            }

            while (AliveWorkerCount < minThreads)
            {
                Worker worker = new Worker(this);
                if (_aliveWorkerDic.TryAdd(worker.ID, worker))
                {
                    Interlocked.Increment(ref _aliveWorkerCount);
                    _aliveWorkerList = _aliveWorkerDic.Values;
                }

                if (PoolRunning && WaitingWorkCount > 0 && worker.TryAssignWorkForNewWorker())
                {
                    continue;
                }

                _idleWorkerDic[worker.ID] = worker;
                Interlocked.Increment(ref _idleWorkerCount);
                _idleWorkerQueue.Enqueue(worker.ID);
            }
        }

        /// <summary>
        /// Set a work into a worker's work queue.
        /// </summary>
        /// <param name="work"></param>
        internal void SetWork(WorkBase work)
        {
            CheckPoolStart();

            Worker worker = null;
            // In the GetWorker logic:
            // 1. It will try to get an available Worker.
            // 2. If no free Workers are available, it will attempt to create a new Worker.
            // 3. If creation fails, it will try to select an existing Worker.
            // This logic rarely results in spinning, unless a large number of Work items are added at once.
            // Even if spinning occurs, GetWorker is non-blocking and executes quickly,
            // so spinning will not consume a lot of CPU resources. 
            SpinWait.SpinUntil(() => (worker = GetWorker(work.LongRunning)) != null);
            work.QueueDateTime = DateTime.UtcNow;
            worker.SetWork(work, false);
        }

        /// <summary>
        /// Get a Worker
        /// </summary>
        /// <param name="longRunning"></param>
        /// <returns></returns>
        private Worker GetWorker(bool longRunning)
        {
            Worker worker = TryDequeueIdleWorker(longRunning);
            if (worker != null)
            {
                return worker;
            }

            worker = TryCreateNewWorker(longRunning);

            if (worker != null)
            {
                return worker;
            }

            if (!longRunning)
            {
                worker = TrySelectExistingWorker();
            }

            return worker;
        }

        /// <summary>
        /// Dequeue a worker from the idle worker queue.
        /// </summary>
        /// <param name="longRunning"></param>
        /// <returns>worker</returns>
        private Worker TryDequeueIdleWorker(bool longRunning)
        {
            Worker worker = null;
            while (_idleWorkerQueue.TryDequeue(out int firstWorkerID))
            {
                if (_idleWorkerDic.TryRemove(firstWorkerID, out worker))
                {
                    // Prevent Worker from being disturbed when getting tasks and causing race condition. 
                    // CanGetWork will reset Allowed after tasks are added to the Worker. 
                    // It executes quickly, so spinning will not consume a lot of CPU resources. 
                    SpinWait.SpinUntil(() => worker.CanGetWork.TrySet(CanGetWork.NotAllowed, CanGetWork.Allowed));
                    Interlocked.Decrement(ref _idleWorkerCount);

                    if (longRunning)
                    {
                        Interlocked.Increment(ref _longRunningWorkerCount);
                    }

                    return worker;
                }
            }
            return null;
        }

        /// <summary>
        /// Create a new worker if the current number of alive workers is less than
        /// the maximum allowed threads plus the number of long-running workers.
        /// </summary>
        /// <param name="longRunning"></param>
        /// <returns>worker</returns>
        private Worker TryCreateNewWorker(bool longRunning)
        {
            Worker worker = null;

            if (AliveWorkerCount < PowerPoolOption.MaxThreads + LongRunningWorkerCount)
            {
                if (_canCreateNewWorker.TrySet(CanCreateNewWorker.NotAllowed, CanCreateNewWorker.Allowed))
                {
                    if (AliveWorkerCount < PowerPoolOption.MaxThreads + LongRunningWorkerCount)
                    {
                        worker = new Worker(this);
                        worker.CanGetWork.InterlockedValue = CanGetWork.NotAllowed;

                        if (_aliveWorkerDic.TryAdd(worker.ID, worker))
                        {
                            Interlocked.Increment(ref _aliveWorkerCount);
                            _aliveWorkerList = _aliveWorkerDic.Values;
                        }

                        if (longRunning)
                        {
                            Interlocked.Increment(ref _longRunningWorkerCount);
                        }
                    }

                    _canCreateNewWorker.InterlockedValue = CanCreateNewWorker.Allowed;
                }
            }

            return worker;
        }

        /// <summary>
        /// Select an existing worker from the list of alive workers.
        /// It avoids selecting long-running workers and tries to pick the worker
        /// with the least amount of pending work.
        /// </summary>
        /// <returns>worker</returns>
        private Worker TrySelectExistingWorker()
        {
            Worker selectedWorker = null;
            int minWaitingWorkCount = int.MaxValue;
            IEnumerable<Worker> workers = _aliveWorkerList;

            foreach (Worker aliveWorker in workers)
            {
                if (aliveWorker.LongRunning)
                {
                    continue;
                }

                int waitingWorkCountTemp = aliveWorker.WaitingWorkCount;
                if (waitingWorkCountTemp < minWaitingWorkCount)
                {
                    if (aliveWorker.CanGetWork.TrySet(CanGetWork.NotAllowed, CanGetWork.Allowed))
                    {
                        if (selectedWorker != null)
                        {
                            selectedWorker.CanGetWork.TrySet(CanGetWork.Allowed, CanGetWork.NotAllowed);
                        }

                        selectedWorker = aliveWorker;

                        if (waitingWorkCountTemp == 0)
                        {
                            break;
                        }

                        minWaitingWorkCount = waitingWorkCountTemp;
                    }
                }
            }

            return selectedWorker;
        }

        /// <summary>
        /// Check if it's the start of thread pool
        /// </summary>
        private void CheckPoolStart()
        {
            if (RunningWorkerCount == 0 && _poolState.TrySet(PoolStates.Running, PoolStates.NotRunning))
            {
                if (PoolStarted != null)
                {
                    SafeInvoke(PoolStarted, new EventArgs(), ErrorFrom.PoolStarted, null);
                }

                _startCount = 0;
                _endCount = 0;
                _queueTime = 0;
                _executeTime = 0;

                _startDateTime = DateTime.UtcNow;

                if (PowerPoolOption.ClearResultStorageWhenPoolStart)
                {
                    _resultDic.Clear();
                }
                if (PowerPoolOption.ClearFailedWorkRecordWhenPoolStart)
                {
                    _failedWorkSet.Clear();
                    _canceledWorkSet.Clear();
                }

                _waitAllSignal.Reset();

                if (PowerPoolOption.RunningTimerOption != null)
                {
                    _runningTimer.Set((int)PowerPoolOption.RunningTimerOption.Interval);
                }
                else
                {
                    _runningTimer.Cancel();
                }

                if (PowerPoolOption.TimeoutOption != null)
                {
                    _timeoutTimer.Set(PowerPoolOption.TimeoutOption.Duration);
                }
            }
        }

        /// <summary>
        /// Check if thread pool is idle
        /// </summary>
        internal void CheckPoolIdle()
        {
            if (_disposing || _disposed)
            {
                return;
            }

            if (!EnablePoolIdleCheck)
            {
                return;
            }

            FillWorkerQueue();

            if (RunningWorkerCount == 0 &&
                WaitingWorkCount == 0 &&
                _poolState.TrySet(PoolStates.IdleChecked, PoolStates.Running)
                )
            {
                _endDateTime = DateTime.UtcNow;
                if (PoolIdled != null)
                {
                    PoolIdledEventArgs poolIdledEventArgs = new PoolIdledEventArgs
                    {
                        StartDateTime = _startDateTime,
                        EndDateTime = DateTime.UtcNow,
                    };
                    SafeInvoke(PoolIdled, poolIdledEventArgs, ErrorFrom.PoolIdled, null);
                }
                IdleSetting();
            }
        }

        /// <summary>
        /// Reset some flags
        /// </summary>
        private void IdleSetting()
        {
            _runningTimer.Cancel();
            _timeoutTimer.Cancel();

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _poolState.InterlockedValue = PoolStates.NotRunning;
            if (_poolStopping)
            {
                _poolStopping = false;

                while (_stopSuspendedWorkQueue.TryDequeue(out string key))
                {
                    if (_stopSuspendedWork.TryGetValue(key, out WorkBase work))
                    {
                        SetWork(work);
                    }
                }
            }

            _waitAllSignal.Set();
        }

        /// <summary>
        /// Add worker into _aliveWorkDic
        /// </summary>
        /// <param name="work"></param>
        internal void SetWorkOwner(WorkBase work)
        {
            _aliveWorkDic[work.ID] = work;
        }

        /// <summary>
        /// Clear result storage
        /// </summary>
        public void ClearResultStorage()
        {
            _resultDic.Clear();
        }

        /// <summary>
        /// Clear result storage
        /// </summary>
        /// <param name="workID">work ID</param>
        public void ClearResultStorage(string workID)
        {
            _resultDic.TryRemove(workID, out _);
        }

        /// <summary>
        /// Clear result storage
        /// </summary>
        /// <param name="workIDList">work ID list</param>
        public void ClearResultStorage(IEnumerable<string> workIDList)
        {
            foreach (string workID in workIDList)
            {
                _resultDic.TryRemove(workID, out _);
            }
        }

        /// <summary>
        /// Clear failed work record
        /// </summary>
        public void ClearFailedWorkRecord()
        {
            _failedWorkSet.Clear();
            _canceledWorkSet.Clear();
        }

        private void CheckDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(_disposing || _disposed, this);
#else
            if (_disposing || _disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#endif
        }

        /// <summary>
        /// Will try stop, force stop and kill all of the workers. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the instance
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _disposing = true;
                    Stop();
                    while (AliveWorkerCount > 0 || IdleWorkerCount > 0)
                    {
                        IEnumerable<Worker> workers = _aliveWorkerList;
                        foreach (Worker worker in workers)
                        {
                            worker.CanForceStop.TrySet(CanForceStop.NotAllowed, CanForceStop.Allowed, out CanForceStop origCanForceStop);
                            if (worker.CanForceStop == CanForceStop.NotAllowed)
                            {
                                if (origCanForceStop == CanForceStop.Allowed)
                                {
                                    worker.ForceStop(true);
                                }
                                worker.DisposeWithJoin();
                            }
                        }
                        Thread.Yield();
                    }
                    _runningWorkerCount = 0;
                    _cancellationTokenSource.Dispose();
                    _pauseSignal.Dispose();
                    _waitAllSignal.Dispose();
                    _runningTimer.Dispose();
                    _timeoutTimer.Dispose();
                }

                _disposed = true;
            }
        }

        ~PowerPool()
        {
            Dispose(false);
        }
    }
}
