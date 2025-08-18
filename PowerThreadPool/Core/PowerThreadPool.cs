﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Helpers.Dependency;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Helpers.Timers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    /// <summary>
    /// A comprehensive and efficient lock-free thread pool with granular work control, flexible concurrency, robust error handling, and easy work management for both sync and async workloads.
    /// </summary>
    public partial class PowerPool : IDisposable
    {
        internal bool _disposed = false;
        internal bool _disposing = false;

        private WorkDependencyController _workDependencyController;

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
        internal volatile bool _aliveWorkerDicChanged = false;
        internal Worker[] _aliveWorkerList = new List<Worker>().ToArray();
        internal int _aliveWorkerListLoopIndex = 0;

        internal ConcurrentQueue<string> _suspendedWorkQueue = new ConcurrentQueue<string>();
        internal ConcurrentDictionary<string, WorkBase> _suspendedWork = new ConcurrentDictionary<string, WorkBase>();
        internal ConcurrentQueue<string> _stopSuspendedWorkQueue = new ConcurrentQueue<string>();
        internal ConcurrentDictionary<string, WorkBase> _stopSuspendedWork = new ConcurrentDictionary<string, WorkBase>();

        internal ConcurrentDictionary<string, ExecuteResultBase> _resultDic = new ConcurrentDictionary<string, ExecuteResultBase>();

        internal ConcurrentDictionary<string, ConcurrentSet<string>> _asyncWorkIDDict = new ConcurrentDictionary<string, ConcurrentSet<string>>();
        internal ConcurrentDictionary<string, ITaskCompletionSource> _tcsDict = new ConcurrentDictionary<string, ITaskCompletionSource>();

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

                value.PowerPoolList.Add(this);

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
                List<string> list = _aliveWorkDic.Values
                    .Where(x => x.ExecuteCount == 0)
                    .Select(x => x.ID).ToList();
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
        internal int _asyncWorkCount = 0;
        public int AsyncWorkCount => _asyncWorkCount;

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
            _workDependencyController = new WorkDependencyController(this);
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
            DestroyThreadOption destroyThreadOption = PowerPoolOption.DestroyThreadOption;

            if (destroyThreadOption != null)
            {
                minThreads = destroyThreadOption.MinThreads;
            }

            while (AliveWorkerCount < minThreads)
            {
                Worker worker = new Worker(this);
                if (_aliveWorkerDic.TryAdd(worker.ID, worker))
                {
                    Interlocked.Increment(ref _aliveWorkerCount);
                    _aliveWorkerDicChanged = true;
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

            WorkPlacementPolicy workPlacementPolicy = work.WorkPlacementPolicy;
            // In most cases, the loop will not iterate more than once.
            while (true)
            {
                bool rejected = PowerPoolOption.RejectOption != null;
                Worker currentWorker = null;

                if (workPlacementPolicy == WorkPlacementPolicy.PreferLocalWorker && _aliveWorkerDic.TryGetValue(Thread.CurrentThread.ManagedThreadId, out currentWorker))
                {
                    worker = currentWorker;
                    break;
                }

                if ((worker = GetWorker(work.LongRunning, workPlacementPolicy, ref rejected)) != null)
                {
                    break;
                }

                if (workPlacementPolicy == WorkPlacementPolicy.PreferIdleThenLocal)
                {
                    if (_aliveWorkerDic.TryGetValue(Thread.CurrentThread.ManagedThreadId, out currentWorker))
                    {
                        worker = currentWorker;
                        break;
                    }
                    else
                    {
                        workPlacementPolicy = WorkPlacementPolicy.PreferIdleThenLeastLoaded;
                    }
                }

                if (rejected)
                {
                    RejectType rejectType = PowerPoolOption.RejectOption.RejectType;

                    string rejectID = work.RealWorkID;

                    if (WorkRejected != null)
                    {
                        WorkRejectedEventArgs workRejectedEventArgs = new WorkRejectedEventArgs(rejectType)
                        {
                            ID = rejectID,
                        };
                        SafeInvoke(WorkRejected, workRejectedEventArgs, ErrorFrom.WorkRejected, null);
                    }

                    if (rejectType == RejectType.AbortPolicy)
                    {
                        WorkRejectedException workRejectedException = new WorkRejectedException
                        {
                            ID = rejectID,
                        };
                        Interlocked.Decrement(ref _waitingWorkCount);
                        throw workRejectedException;
                    }
                    else if (rejectType == RejectType.CallerRunsPolicy)
                    {
                        worker = new Worker(this, work);
                        Interlocked.Increment(ref _runningWorkerCount);
                        worker.ExecuteWork();
                        Interlocked.Decrement(ref _runningWorkerCount);
                        worker.Dispose();

                        CheckPoolIdle();

                        return;
                    }
                    else if (rejectType == RejectType.DiscardPolicy)
                    {
                        Interlocked.Decrement(ref _waitingWorkCount);
                        OnWorkDiscarded(work, rejectType);

                        CheckPoolIdle();

                        return;
                    }
                    else if (rejectType == RejectType.DiscardOldestPolicy)
                    {
                        foreach (Worker workerDiscard in _aliveWorkerList)
                        {
                            // When ThreadQueueLimit is 0 and the work rejection policy is set to "DiscardOldestPolicy",
                            // since there are no works in the queue, the oldest work cannot be discarded.
                            // This may cause excessive spinning with no progress.
                            // However, this is due to an unreasonable user configuration, so no handling is implemented;
                            // a warning is provided in the documentation instead.
                            if (workerDiscard.DiscardOneWork(out WorkBase discardWork))
                            {
                                OnWorkDiscarded(discardWork, rejectType);
                                Interlocked.Decrement(ref _waitingWorkCount);
                                worker = workerDiscard;
                                break;
                            }
                        }

                        if (worker != null)
                        {
                            break;
                        }
                    }
                }
            }

            work.QueueDateTime = DateTime.UtcNow;
            worker.SetWork(work, false);
        }

        private void OnWorkDiscarded(WorkBase work, RejectType rejectType)
        {
            ExecuteResultBase executeResult = work.SetExecuteResult(null, null, Status.Canceled);
            string idErr = work.ID;
            executeResult.ID = idErr;

            WorkCallbackEnd(work, executeResult.Status);

            if (WorkDiscarded != null)
            {
                WorkDiscardedEventArgs workDiscardedEventArgs = new WorkDiscardedEventArgs(rejectType)
                {
                    ID = work.ID,
                };
                SafeInvoke(WorkDiscarded, workDiscardedEventArgs, ErrorFrom.WorkDiscarded, null);
            }
        }

        /// <summary>
        /// Get a Worker
        /// </summary>
        /// <param name="longRunning"></param>
        /// <returns></returns>
        private Worker GetWorker(bool longRunning, WorkPlacementPolicy workPlacementPolicy, ref bool rejected)
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

            if (workPlacementPolicy == WorkPlacementPolicy.PreferIdleThenLeastLoaded && !longRunning)
            {
                worker = TrySelectExistingWorker(ref rejected);
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
                    Interlocked.Decrement(ref _idleWorkerCount);

                    if (worker.CanGetWork.TrySet(CanGetWork.NotAllowed, CanGetWork.Allowed))
                    {
                        if (longRunning)
                        {
                            Interlocked.Increment(ref _longRunningWorkerCount);
                        }

                        return worker;
                    }
                    else if (_idleWorkerDic.TryAdd(firstWorkerID, worker))
                    {
                        Interlocked.Increment(ref _idleWorkerCount);
                        _idleWorkerQueue.Enqueue(firstWorkerID);
                    }
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

            if (AliveWorkerCount < PowerPoolOption.MaxThreads + LongRunningWorkerCount || longRunning)
            {
                if (_canCreateNewWorker.TrySet(CanCreateNewWorker.NotAllowed, CanCreateNewWorker.Allowed))
                {
                    if (AliveWorkerCount < PowerPoolOption.MaxThreads + LongRunningWorkerCount || longRunning)
                    {
                        worker = new Worker(this);
                        worker.CanGetWork.InterlockedValue = CanGetWork.NotAllowed;

                        if (_aliveWorkerDic.TryAdd(worker.ID, worker))
                        {
                            Interlocked.Increment(ref _aliveWorkerCount);
                            _aliveWorkerDicChanged = true;
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
        private Worker TrySelectExistingWorker(ref bool rejected)
        {
            Worker selectedWorker = null;
            int minWaitingWorkCount = int.MaxValue;

            UpdateAliveWorkerList();
            Worker[] workerList = _aliveWorkerList;
            int step = 0;
            int startIndex = _aliveWorkerListLoopIndex;
            int loopIndex = _aliveWorkerListLoopIndex;

            RejectOption rejectOption = PowerPoolOption.RejectOption;

            // In most cases, the loop will not iterate more than once.
            while (true)
            {
                // WorkStealingLoopMaxStep is automatically calculated from MaxThreads using a logarithmic formula to optimize loop performance for different thread pool sizes.
                // It limits the minimum number of steps for each loop iteration.
                // The number of loop steps will not exceed the length of _aliveWorkerList.
                // _aliveWorkerListLoopIndex is used to ensure that the starting point of each loop iteration varies as much as possible.
                if ((step >= PowerPoolOption.WorkLoopMaxStep && selectedWorker != null) || step >= workerList.Length)
                {
                    if (selectedWorker != null && rejectOption != null)
                    {
                        rejected = false;
                    }
                    break;
                }
                ++step;
                if (loopIndex >= workerList.Length)
                {
                    loopIndex = 0;
                }

                Worker aliveWorker = workerList[loopIndex];

                if (aliveWorker.LongRunning)
                {
                    ++loopIndex;
                    continue;
                }

                int waitingWorkCountTemp = aliveWorker.WaitingWorkCount;

                if (rejectOption != null && waitingWorkCountTemp >= rejectOption.ThreadQueueLimit)
                {
                    ++loopIndex;
                    continue;
                }

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

                ++loopIndex;
            }

            _aliveWorkerListLoopIndex = loopIndex;

            return selectedWorker;
        }

        /// <summary>
        /// Check if it's the start of thread pool
        /// </summary>
        private void CheckPoolStart()
        {
            if (_poolState == PoolStates.NotRunning && _poolState.TrySet(PoolStates.Running, PoolStates.NotRunning))
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

            if (RunningWorkerCount == 0 &&
                WaitingWorkCount == 0 &&
                AsyncWorkCount == 0 &&
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
        /// Update alive worker list when _aliveWorkerDic is already updated
        /// </summary>
        internal void UpdateAliveWorkerList()
        {
            if (_aliveWorkerDicChanged)
            {
                _aliveWorkerDicChanged = false;
                _aliveWorkerList = _aliveWorkerDic.Values.ToArray();
            }
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

        /// <summary>
        /// Try remove async work
        /// </summary>
        /// <param name="baseID"></param>
        /// <param name="started"></param>
        internal void TryRemoveAsyncWork(string baseID, bool started)
        {
            if (_asyncWorkIDDict.TryRemove(baseID, out ConcurrentSet<string> asyncIDList))
            {
                Interlocked.Decrement(ref _asyncWorkCount);
                if (_aliveWorkDic.TryRemove(baseID, out WorkBase baseWork))
                {
                    baseWork.Dispose();
                }
                if (started)
                {
                    foreach (string asyncID in asyncIDList)
                    {
                        if (_aliveWorkDic.TryRemove(asyncID, out WorkBase asyncWork))
                        {
                            asyncWork.Dispose();
                        }
                    }
                }
            }
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
                        foreach (Worker worker in _aliveWorkerDic.Values)
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
