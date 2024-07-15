using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Groups;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public partial class PowerPool : IDisposable
    {
        private bool _disposed = false;
        private bool _disposing = false;

        private ManualResetEvent _waitAllSignal = new ManualResetEvent(false);
        private ManualResetEvent _pauseSignal = new ManualResetEvent(true);
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        internal ConcurrentSet<string> _failedWorkSet = new ConcurrentSet<string>();

        internal ConcurrentDictionary<Guid, Worker> _idleWorkerDic = new ConcurrentDictionary<Guid, Worker>();
        internal ConcurrentQueue<Guid> _idleWorkerQueue = new ConcurrentQueue<Guid>();

        internal ConcurrentDictionary<string, WorkBase> _settedWorkDic = new ConcurrentDictionary<string, WorkBase>();
        internal ConcurrentDictionary<string, ConcurrentSet<string>> _workGroupDic = new ConcurrentDictionary<string, ConcurrentSet<string>>();
        internal ConcurrentDictionary<Guid, Worker> _aliveWorkerDic = new ConcurrentDictionary<Guid, Worker>();
        internal IEnumerable<Worker> _aliveWorkerList = new List<Worker>();

        internal ConcurrentQueue<string> _suspendedWorkQueue = new ConcurrentQueue<string>();
        internal ConcurrentDictionary<string, WorkBase> _suspendedWork = new ConcurrentDictionary<string, WorkBase>();

        internal ConcurrentDictionary<string, ExecuteResultBase> _resultDic = new ConcurrentDictionary<string, ExecuteResultBase>();

        internal long _startCount = 0;
        internal long _endCount = 0;
        internal long _queueTime = 0;
        internal long _executeTime = 0;

        private bool _suspended;

        private InterlockedFlag<WorkerCreationFlags> _createWorkerLock = WorkerCreationFlags.Unlocked;

        private PowerPoolOption _powerPoolOption;
        public PowerPoolOption PowerPoolOption
        {
            get => _powerPoolOption;
            set
            {
                _powerPoolOption = value;
                _suspended = value.StartSuspended;
                InitWorkerQueue();
            }
        }

        private System.Timers.Timer _poolTimer;

        private InterlockedFlag<PoolRunningFlags> _poolRunning = PoolRunningFlags.NotRunning;

        public bool PoolRunning => _poolRunning == PoolRunningFlags.Running;

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
                List<string> list = _settedWorkDic.Keys.ToList();
                foreach (Worker worker in _aliveWorkerList)
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

        public PowerPool()
        {

        }

        public PowerPool(PowerPoolOption powerPoolOption)
        {
            PowerPoolOption = powerPoolOption;
        }

        /// <summary>
        /// Start the pool, but only if PowerPoolOption.StartSuspended is set to true.
        /// </summary>
        public void Start()
        {
            if (_disposing || _disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!_suspended)
            {
                return;
            }

            _suspended = false;
            while (_suspendedWorkQueue.TryDequeue(out string key))
            {
                if (_suspendedWork.TryGetValue(key, out WorkBase work))
                {
                    ConcurrentSet<string> dependents = work.Dependents;
                    if (dependents == null || dependents.Count == 0)
                    {
                        SetWork(work);
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
        /// Init worker queue
        /// </summary>
        private void InitWorkerQueue()
        {
            if (PowerPoolOption.DestroyThreadOption != null)
            {
                if (PowerPoolOption.DestroyThreadOption.MinThreads > PowerPoolOption.MaxThreads)
                {
                    throw new ArgumentException("The minimum number of threads cannot be greater than the maximum number of threads.");
                }
            }

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
                _idleWorkerDic[worker.ID] = worker;
                Interlocked.Increment(ref _idleWorkerCount);
                _idleWorkerQueue.Enqueue(worker.ID);
            }
        }

        /// <summary>
        /// Set a work into a worker's work queue.
        /// </summary>
        internal void SetWork(WorkBase work)
        {
            CheckPoolStart();

            Worker worker = null;
            SpinWait.SpinUntil(() => (worker = GetWorker(work.LongRunning)) != null);
            work.QueueDateTime = DateTime.UtcNow;
            worker.SetWork(work, false);
        }

        /// <summary>
        /// Get a Worker
        /// </summary>
        /// <returns>worker</returns>
        private Worker GetWorker(bool longRunning)
        {
            Worker worker = null;
            while (_idleWorkerQueue.TryDequeue(out Guid firstWorkerID))
            {
                if (_idleWorkerDic.TryRemove(firstWorkerID, out worker))
                {
                    SpinWait.SpinUntil(() => worker.GettedLock.TrySet(WorkerGettedFlags.Locked, WorkerGettedFlags.Unlocked));
                    Interlocked.Decrement(ref _idleWorkerCount);
                    if (longRunning)
                    {
                        Interlocked.Increment(ref _longRunningWorkerCount);
                    }
                    return worker;
                }
            }

            if (AliveWorkerCount < PowerPoolOption.MaxThreads + LongRunningWorkerCount)
            {
                if (_createWorkerLock.TrySet(WorkerCreationFlags.Locked, WorkerCreationFlags.Unlocked))
                {
                    if (AliveWorkerCount < PowerPoolOption.MaxThreads + LongRunningWorkerCount)
                    {
                        worker = new Worker(this);

                        worker.GettedLock.InterlockedValue = WorkerGettedFlags.Locked;

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

                    _createWorkerLock.InterlockedValue = WorkerCreationFlags.Unlocked;
                }
            }

            if (worker == null && !longRunning)
            {
                int min = int.MaxValue;
                foreach (Worker aliveWorker in _aliveWorkerList)
                {
                    if (aliveWorker.LongRunning)
                    {
                        continue;
                    }

                    int waitingWorkCountTemp = aliveWorker.WaitingWorkCount;
                    if (waitingWorkCountTemp < min)
                    {
                        if (aliveWorker.GettedLock.TrySet(WorkerGettedFlags.Locked, WorkerGettedFlags.Unlocked))
                        {
                            if (worker != null)
                            {
                                worker.GettedLock.TrySet(WorkerGettedFlags.Unlocked, WorkerGettedFlags.Locked);
                            }
                            
                            worker = aliveWorker;
                            if (waitingWorkCountTemp == 0)
                            {
                                break;
                            }

                            min = waitingWorkCountTemp;
                        }
                    }
                }
            }

            return worker;
        }

        /// <summary>
        /// Check if it's the start of thread pool
        /// </summary>
        private void CheckPoolStart()
        {
            if (_poolRunning.TrySet(PoolRunningFlags.Running, PoolRunningFlags.NotRunning))
            {
                if (PoolStarted != null)
                {
                    SafeInvoke(PoolStarted, new EventArgs(), ErrorFrom.PoolStarted, null);
                }

                _startCount = 0;
                _endCount = 0;
                _queueTime = 0;
                _executeTime = 0;

                if (PowerPoolOption.ClearResultStorageWhenPoolStart)
                {
                    _resultDic.Clear();
                }
                if (PowerPoolOption.ClearFailedWorkRecordWhenPoolStart)
                {
                    _failedWorkSet.Clear();
                }

                _waitAllSignal.Reset();

                if (PowerPoolOption.TimeoutOption != null)
                {
                    _poolTimer = new System.Timers.Timer(PowerPoolOption.TimeoutOption.Duration);
                    _poolTimer.AutoReset = false;
                    _poolTimer.Elapsed += (s, e) =>
                    {
                        if (PoolTimedOut != null)
                        {
                            SafeInvoke(PoolTimedOut, new EventArgs(), ErrorFrom.PoolTimedOut, null);
                        }
                        Stop(PowerPoolOption.TimeoutOption.ForceStop);
                    };
                    _poolTimer.Start();
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

            InitWorkerQueue();

            if (RunningWorkerCount == 0 &&
                WaitingWorkCount == 0 &&
                _poolRunning.TrySet(PoolRunningFlags.IdleChecked, PoolRunningFlags.Running)
                )
            {
                if (PoolIdled != null)
                {
                    try
                    {
                        SafeInvoke(PoolIdled, new EventArgs(), ErrorFrom.PoolIdled, null);
                    }
                    finally
                    {
                        IdleSetting();
                    }
                }
                else
                {
                    IdleSetting();
                }
            }
        }

        /// <summary>
        /// Reset some flags
        /// </summary>
        private void IdleSetting()
        {
            if (_poolTimer != null)
            {
                _poolTimer.Stop();
                _poolTimer.Enabled = false;
            }

            _suspended = PowerPoolOption.StartSuspended;

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _poolRunning.InterlockedValue = PoolRunningFlags.NotRunning;
            if (_poolStopping)
            {
                _poolStopping = false;
            }

            _waitAllSignal.Set();
        }

        /// <summary>
        /// Add worker into settedWorkDic
        /// </summary>
        /// <param name="workId"></param>
        /// <param name="worker"></param>
        internal void SetWorkOwner(WorkBase work)
        {
            _settedWorkDic[work.ID] = work;
        }

        /// <summary>
        /// Get group object
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>Group object</returns>
        public Group GetGroup(string groupName)
        {
            return new Group(this, groupName);
        }

        /// <summary>
        /// Get all members of a group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>Work id list</returns>
        public IEnumerable<string> GetGroupMemberList(string groupName)
        {
            if (_workGroupDic.TryGetValue(groupName, out ConcurrentSet<string> groupMemberList))
            {
                return groupMemberList;
            }
            return new ConcurrentSet<string>();
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
        /// <param name="workID">work ID list</param>
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
                    Stop(true);
                    while (AliveWorkerCount > 0 || IdleWorkerCount > 0)
                    {
                        foreach (Worker worker in _aliveWorkerList)
                        {
                            if (!worker._disposed)
                            {
                                worker.ForceStop(true);
                                worker.Kill();
                                worker.Dispose();
                            }
                        }
                        Thread.Yield();
                    }
                    _runningWorkerCount = 0;
                    _cancellationTokenSource.Dispose();
                    _pauseSignal.Dispose();
                    _waitAllSignal.Dispose();
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
