using System.Threading;
using System;
using PowerThreadPool.Options;
using System.Collections.Concurrent;
using PowerThreadPool.Collections;
using PowerThreadPool.EventArguments;
using System.Collections.Generic;
using PowerThreadPool.Constants;
using System.Timers;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public class Worker : IDisposable
    {
        internal Thread thread;

        private string id;
        internal string ID { get => id; set => id = value; }

        internal int workerState = WorkerStates.Idle;
        internal int gettedLock = WorkerGettedFlags.Unlocked;

        private ConcurrentPriorityQueue<string> waitingWorkIDQueue = new ConcurrentPriorityQueue<string>();
        private ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

        private System.Timers.Timer timeoutTimer;
        private System.Timers.Timer killTimer;

        private ManualResetEvent runSignal = new ManualResetEvent(false);
        private string workID;
        internal string WorkID { get => workID; set => workID = value; }
        private WorkBase work;
        private bool killFlag = false;
        private int stealingLock = WorkerStealingFlags.Unlocked;

        private PowerPool powerPool;

        private bool disposed = false;

        private bool longRunning = true;
        internal bool LongRunning { get => longRunning; set => longRunning = value; }

        private int waitingWorkCount = 0;
        internal int WaitingWorkCount
        {
            get
            {
                return waitingWorkCount;
            }
        }

        internal Worker(PowerPool powerPool)
        {
            InitKillTimer(powerPool);

            this.powerPool = powerPool;
            this.ID = Guid.NewGuid().ToString();
            thread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        runSignal.WaitOne();

                        if (killFlag)
                        {
                            return;
                        }

                        powerPool.OnWorkStart(work.ID);

                        ExecuteResultBase executeResult = ExecuteWork();

                        powerPool.OneWorkEnd(executeResult);
                        work.InvokeCallback(executeResult, powerPool.PowerPoolOption);

                        powerPool.WorkCallbackEnd(workID, executeResult.Status);

                        if (work.WaitSignal != null)
                        {
                            work.WaitSignal.Set();
                        }

                        if (work.LongRunning)
                        {
                            Interlocked.Decrement(ref powerPool.longRunningWorkerCount);
                            this.LongRunning = false;
                        }

                        AssignWork();
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    Interlocked.Exchange(ref gettedLock, WorkerGettedFlags.Disabled);
                    int origWorkState = Interlocked.Exchange(ref workerState, WorkerStates.ToBeDisposed);

                    if (work.LongRunning)
                    {
                        Interlocked.Decrement(ref powerPool.longRunningWorkerCount);
                        this.LongRunning = false;
                    }

                    if (origWorkState == WorkerStates.Running)
                    {
                        Interlocked.Decrement(ref powerPool.runningWorkerCount);
                    }

                    if (powerPool.aliveWorkerDic.TryRemove(ID, out _))
                    {
                        Interlocked.Decrement(ref powerPool.aliveWorkerCount);
                        powerPool.aliveWorkerList = powerPool.aliveWorkerDic.Values;
                    }
                    if (powerPool.idleWorkerDic.TryRemove(ID, out _))
                    {
                        Interlocked.Decrement(ref powerPool.idleWorkerCount);
                    }

                    powerPool.OneThreadEndByForceStop(work.ID);

                    if (!ex.Data.Contains("ThrowedWhenExecuting"))
                    {
                        ex.Data.Add("ThrowedWhenExecuting", false);
                    }
                    else
                    {
                        ExecuteResultBase executeResult = work.SetExecuteResult(null, ex, Status.Failed);
                        executeResult.ID = work.ID;
                        work.InvokeCallback(executeResult, powerPool.PowerPoolOption);
                    }

                    powerPool.WorkCallbackEnd(workID, Status.Failed);

                    bool hasWaitingWork = false;
                    IEnumerable<WorkBase> waitingWorkList = waitingWorkDic.Values;
                    foreach (WorkBase work in waitingWorkList)
                    {
                        powerPool.SetWork(work);
                        hasWaitingWork = true;
                    }

                    if (work.WaitSignal != null)
                    {
                        work.WaitSignal.Set();
                    }

                    if (!hasWaitingWork)
                    {
                        powerPool.CheckPoolIdle();
                    }
                }
            });
            thread.Start();
        }

        private void InitKillTimer(PowerPool powerPool)
        {
            if (powerPool.PowerPoolOption.DestroyThreadOption != null)
            {
                this.killTimer = new System.Timers.Timer(powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime);
                this.killTimer.AutoReset = false;
                this.killTimer.Elapsed += OnKillTimerElapsed;
            }
        }

        private ExecuteResultBase ExecuteWork()
        {
            ExecuteResultBase executeResult;
            try
            {
                object result = work.Execute();
                executeResult = work.SetExecuteResult(result, null, Status.Succeed);
            }
            catch (ThreadInterruptedException ex)
            {
                ex.Data.Add("ThrowedWhenExecuting", true);
                throw;
            }
            catch (Exception ex)
            {
                executeResult = work.SetExecuteResult(null, ex, Status.Failed);
            }
            executeResult.ID = work.ID;

            return executeResult;
        }

        public bool Wait(string workID)
        {
            bool res = false;
            if (waitingWorkDic.TryGetValue(workID, out WorkBase needWaitWork))
            {
                if (needWaitWork.WaitSignal == null)
                {
                    needWaitWork.WaitSignal = new AutoResetEvent(false);
                }
                needWaitWork.WaitSignal.WaitOne();
                res = true;
            }
            else
            {
                needWaitWork = work;
                if (workID == needWaitWork.ID)
                {
                    if (needWaitWork.WaitSignal == null)
                    {
                        needWaitWork.WaitSignal = new AutoResetEvent(false);
                    }
                    needWaitWork.WaitSignal.WaitOne();
                    res = true;
                }
            }

            return res;
        }

        public void ForceStop()
        {
            if (workerState == WorkerStates.Running)
            {
                Cancel();
                thread.Interrupt();
                thread.Join();
            }
        }

        public void WaitForResume()
        {
            work.PauseSignal.WaitOne();
        }

        public bool Pause(string workID)
        {
            bool res = false;

            WorkBase workToPause;
            if (!waitingWorkDic.TryGetValue(workID, out workToPause))
            {
                if (workID == WorkID)
                {
                    workToPause = work;
                }
            }

            if (workToPause != null)
            {
                if (workToPause.PauseSignal == null)
                {
                    workToPause.PauseSignal = new ManualResetEvent(true);
                }

                workToPause.IsPausing = true;
                workToPause.PauseSignal.Reset();
                res = true;
            }

            return res;
        }

        public bool Resume(string workID)
        {
            bool res = false;

            WorkBase workToResume;
            if (!waitingWorkDic.TryGetValue(workID, out workToResume))
            {
                if (workID == WorkID)
                {
                    workToResume = work;
                }
            }

            if (workToResume != null)
            {
                if (workToResume.IsPausing)
                {
                    workToResume.IsPausing = false;
                    workToResume.PauseSignal.Set();
                    res = true;
                }
            }

            return res;
        }

        public void Resume()
        {
            foreach (WorkBase workToResume in waitingWorkDic.Values)
            {
                if (workToResume.IsPausing)
                {
                    workToResume.IsPausing = false;
                    workToResume.PauseSignal.Set();
                }
            }
            if (work.IsPausing)
            {
                work.IsPausing = false;
                work.PauseSignal.Set();
            }
        }

        public bool Stop(string workID, bool forceStop)
        {
            bool res = false;
            if (forceStop)
            {
                if (waitingWorkDic.TryRemove(workID, out _))
                {
                    Interlocked.Decrement(ref waitingWorkCount);
                    Interlocked.Decrement(ref powerPool.waitingWorkCount);
                }
                else
                {
                    thread.Interrupt();
                    thread.Join();
                }
                res = true;
            }
            else
            {
                if (!Cancel(workID))
                {
                    if (workID == WorkID)
                    {
                        work.ShouldStop = true;
                        res = true;
                    }
                }
                else
                {
                    res = true;
                }
            }
            return res;
        }

        internal void SetWork(WorkBase work, bool resetted)
        {
            int originalWorkerState;
            waitingWorkDic[work.ID] = work;
            powerPool.SetWorkOwner(work.ID, this);
            waitingWorkIDQueue.Enqueue(work.ID, work.WorkPriority);
            Interlocked.Increment(ref waitingWorkCount);
            originalWorkerState = Interlocked.CompareExchange(ref workerState, WorkerStates.Running, WorkerStates.Idle);

            if (killTimer != null)
            {
                killTimer.Stop();
            }

            if (!resetted)
            {
                Interlocked.Exchange(ref gettedLock, WorkerGettedFlags.Unlocked);
            }

            if (originalWorkerState == WorkerStates.Idle)
            {
                Interlocked.Increment(ref powerPool.runningWorkerCount);
                AssignWork();
            }
        }

        internal List<WorkBase> Steal(int count)
        {
            List<WorkBase> stolenList = new List<WorkBase>();

            bool isContinue = true;
            while (stolenList.Count < count && isContinue)
            {
                isContinue = false;

                string stolenWorkID;
                stolenWorkID = waitingWorkIDQueue.Dequeue();

                if (stolenWorkID != null)
                {
                    if (waitingWorkDic.TryRemove(stolenWorkID, out WorkBase stolenWork))
                    {
                        Interlocked.Decrement(ref waitingWorkCount);
                        stolenList.Add(stolenWork);

                        isContinue = true;
                    }
                }
            }

            Interlocked.Exchange(ref stealingLock, WorkerStealingFlags.Unlocked);

            return stolenList;
        }

        private void AssignWork()
        {
            while (true)
            {
                WorkBase work = null;
                string waitingWorkID = waitingWorkIDQueue.Dequeue();

                if (waitingWorkID == null && powerPool.aliveWorkerCount <= powerPool.PowerPoolOption.MaxThreads)
                {
                    StealWorksFromOtherWorker(ref waitingWorkID, ref work);
                }

                if (waitingWorkID == null)
                {
                    if (TurnToIdle(ref waitingWorkID, ref work))
                    {
                        return;
                    }
                }

                if (work == null && waitingWorkID != null)
                {
                    if (waitingWorkDic.TryRemove(waitingWorkID, out work))
                    {
                        Interlocked.Decrement(ref waitingWorkCount);
                    }
                }

                if (work == null)
                {
                    continue;
                }

                if (killTimer != null)
                {
                    killTimer.Stop();
                }

                Interlocked.Decrement(ref powerPool.waitingWorkCount);

                SetWorkToRun(work);
                
                runSignal.Set();
                break;
            }
        }

        private void StealWorksFromOtherWorker(ref string waitingWorkID, ref WorkBase work)
        {
            Worker worker = null;
            int max = 0;
            foreach (Worker runningWorker in powerPool.aliveWorkerList)
            {
                if (runningWorker.workerState != WorkerStates.Running || runningWorker.ID == ID)
                {
                    continue;
                }

                int waitingWorkCountTemp = runningWorker.WaitingWorkCount;
                if (waitingWorkCountTemp > max)
                {
                    if (Interlocked.CompareExchange(ref runningWorker.stealingLock, WorkerStealingFlags.Locked, WorkerStealingFlags.Unlocked) == WorkerStealingFlags.Locked)
                    {
                        continue;
                    }
                    if (worker != null)
                    {
                        Interlocked.Exchange(ref worker.stealingLock, WorkerStealingFlags.Unlocked);
                    }
                    max = waitingWorkCountTemp;
                    worker = runningWorker;
                }
            }
            if (worker != null)
            {
                int count = max / 2;
                if (count > 0)
                {
                    List<WorkBase> stolenWorkList = worker.Steal(count);
                    foreach (WorkBase stolenWork in stolenWorkList)
                    {
                        if (waitingWorkID == null)
                        {
                            waitingWorkID = stolenWork.ID;
                            work = stolenWork;
                        }
                        else
                        {
                            SetWork(stolenWork, true);
                        }
                    }
                }
            }
        }

        private bool TurnToIdle(ref string waitingWorkID, ref WorkBase work)
        {
            SpinWait.SpinUntil(() =>
            {
                int gettedLockOrig = Interlocked.CompareExchange(ref gettedLock, WorkerGettedFlags.ToBeDisabled, WorkerGettedFlags.Unlocked);
                return (gettedLockOrig == WorkerGettedFlags.Unlocked);
            });

            if (!waitingWorkDic.IsEmpty)
            {
                waitingWorkID = waitingWorkIDQueue.Dequeue();
                if (waitingWorkID != null)
                {
                    if (waitingWorkDic.TryRemove(waitingWorkID, out work))
                    {
                        Interlocked.Decrement(ref waitingWorkCount);
                    }
                }

                Interlocked.CompareExchange(ref gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.ToBeDisabled);

                return false;
            }
            else
            {
                runSignal.Reset();

                PowerPoolOption powerPoolOption = powerPool.PowerPoolOption;
                if (powerPoolOption.DestroyThreadOption != null && powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads)
                {
                    killTimer.Interval = powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime;
                    killTimer.Start();
                }

                Interlocked.Decrement(ref powerPool.runningWorkerCount);

                Interlocked.Exchange(ref workerState, WorkerStates.Idle);
                Interlocked.CompareExchange(ref gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.ToBeDisabled);

                if (powerPool.idleWorkerDic.TryAdd(this.ID, this))
                {
                    Interlocked.Increment(ref powerPool.idleWorkerCount);
                    powerPool.idleWorkerQueue.Enqueue(this.ID);
                }

                powerPool.CheckPoolIdle();

                return true;
            }
        }

        private void SetWorkToRun(WorkBase work)
        {
            TimeoutOption workTimeoutOption = work.WorkTimeoutOption;
            if (workTimeoutOption != null)
            {
                System.Timers.Timer timer = new System.Timers.Timer(workTimeoutOption.Duration);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    powerPool.OnWorkTimeout(powerPool, new TimeoutEventArgs() { ID = workID });
                    powerPool.Stop(workID, workTimeoutOption.ForceStop);
                };
                timer.Start();

                this.timeoutTimer = timer;
            }

            this.work = work;
            this.workID = work.ID;
            this.longRunning = work.LongRunning;
            ThreadPriority threadPriority = work.ThreadPriority;
            if (thread.Priority != threadPriority)
            {
                thread.Priority = threadPriority;
            }
        }

        private void OnKillTimerElapsed(object s, ElapsedEventArgs e)
        {
            if (powerPool.IdleWorkerCount > powerPool.PowerPoolOption.DestroyThreadOption.MinThreads)
            {
                SpinWait.SpinUntil(() =>
                {
                    int gettedStatus = Interlocked.CompareExchange(ref gettedLock, WorkerGettedFlags.Disabled, WorkerGettedFlags.Unlocked);
                    return (gettedStatus == WorkerGettedFlags.Unlocked || gettedStatus == WorkerGettedFlags.Disabled);
                });

                if (Interlocked.CompareExchange(ref workerState, WorkerStates.ToBeDisposed, WorkerStates.Idle) == WorkerStates.Idle)
                {
                    if (powerPool.idleWorkerDic.TryRemove(ID, out _))
                    {
                        Interlocked.Decrement(ref powerPool.idleWorkerCount);
                    }
                    if (powerPool.aliveWorkerDic.TryRemove(ID, out _))
                    {
                        Interlocked.Decrement(ref powerPool.aliveWorkerCount);
                        powerPool.aliveWorkerList = powerPool.aliveWorkerDic.Values;
                    }
                    Kill();
                }
                else
                {
                    Interlocked.CompareExchange(ref gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.Disabled);
                }
            }

            killTimer.Stop();
        }

        internal void Kill()
        {
            killFlag = true;
            runSignal.Set();
        }

        internal void PauseTimer()
        {
            if (timeoutTimer != null)
            {
                timeoutTimer.Stop();
            }
        }

        internal void ResumeTimer()
        {
            if (timeoutTimer != null)
            {
                timeoutTimer.Start();
            }
        }

        internal void Cancel()
        {
            IEnumerable<string> waitingWorkIDList = waitingWorkDic.Keys;
            foreach (string id in waitingWorkIDList)
            {
                Cancel(id);
            }
        }

        internal bool Cancel(string id)
        {
            if (waitingWorkDic.TryRemove(id, out _))
            {
                ExecuteResultBase executeResult = work.SetExecuteResult(null, null, Status.Canceled);
                executeResult.ID = id;

                powerPool.OneWorkEnd(executeResult);

                work.InvokeCallback(executeResult, powerPool.PowerPoolOption);

                Interlocked.Decrement(ref waitingWorkCount);
                Interlocked.Decrement(ref powerPool.waitingWorkCount);
                return true;
            }
            return false;
        }

        internal bool IsCancellationRequested()
        {
            return work.ShouldStop;
        }

        internal bool IsPausing()
        {
            return work.IsPausing;
        }

        /// <summary>
        /// Dispose the instance. 
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
            if (!disposed)
            {
                if (disposing)
                {
                    runSignal.Dispose();
                }

                disposed = true;
            }
        }

        ~Worker()
        {
            Dispose(false);
        }
    }
}
