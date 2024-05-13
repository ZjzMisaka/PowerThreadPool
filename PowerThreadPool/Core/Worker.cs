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
using PowerThreadPool.Exceptions;

namespace PowerThreadPool
{
    internal class Worker : IDisposable
    {
        internal bool disposed = false;

        internal Thread thread;

        private string id;
        internal string ID { get => id; set => id = value; }

        internal int workerState = WorkerStates.Idle;
        internal int gettedLock = WorkerGettedFlags.Unlocked;
        internal int workHeld = WorkHeldFlags.NotHeld;

        private IConcurrentPriorityCollection<string> waitingWorkIDPriorityCollection;
        private ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

        private System.Timers.Timer timeoutTimer;
        private System.Timers.Timer killTimer;

        private ManualResetEvent runSignal = new ManualResetEvent(false);
        private string workID;
        internal string WorkID { get => workID; set => workID = value; }
        private WorkBase work;
        internal WorkBase Work { get => work; set => work = value; }
        private bool killFlag = false;
        internal int stealingLock = WorkerStealingFlags.Unlocked;

        private PowerPool powerPool;

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

            if (powerPool.PowerPoolOption.QueueType == QueueType.FIFO)
            {
                waitingWorkIDPriorityCollection = new ConcurrentPriorityQueue<string>();
            }
            else
            {
                waitingWorkIDPriorityCollection = new ConcurrentPriorityStack<string>();
            }

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

                        powerPool.OnWorkStarted(work.ID);

                        ExecuteResultBase executeResult = null;
                        do
                        {
                            executeResult = ExecuteWork();

                            if (executeResult.Status == Status.Stopped)
                            {
                                powerPool.InvokeWorkStoppedEvent(executeResult);
                            }
                            else
                            {
                                powerPool.InvokeWorkEndedEvent(executeResult);
                            }
                            work.InvokeCallback(powerPool, executeResult, powerPool.PowerPoolOption);
                        } while (work.ShouldImmediateRetry(executeResult));

                        if (work.ShouldRequeue(executeResult))
                        {
                            Interlocked.Increment(ref powerPool.waitingWorkCount);
                            powerPool.SetWork(work);
                        }
                        else
                        {
                            powerPool.WorkCallbackEnd(work, executeResult.Status);

                            if (work.WaitSignal != null)
                            {
                                work.WaitSignal.Set();
                            }
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

                    ExecuteResultBase executeResult = work.SetExecuteResult(null, ex, Status.ForceStopped);
                    executeResult.ID = work.ID;
                    powerPool.InvokeWorkStoppedEvent(executeResult);

                    if (!ex.Data.Contains("ThrowedWhenExecuting"))
                    {
                        ex.Data.Add("ThrowedWhenExecuting", false);
                    }
                    work.InvokeCallback(powerPool, executeResult, powerPool.PowerPoolOption);

                    powerPool.WorkCallbackEnd(work, Status.Failed);

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
            DateTime runDateTime = DateTime.Now;
            try
            {
                object result = work.Execute();
                executeResult = work.SetExecuteResult(result, null, Status.Succeed);
                executeResult.StartDateTime = runDateTime;
            }
            catch (ThreadInterruptedException ex)
            {
                ex.Data.Add("ThrowedWhenExecuting", true);
                throw;
            }
            catch (WorkStopException ex)
            {
                executeResult = work.SetExecuteResult(null, ex, Status.Stopped);
            }
            catch (Exception ex)
            {
                executeResult = work.SetExecuteResult(null, ex, Status.Failed);
                powerPool.OnWorkErrorOccurred(ex, EventArguments.ErrorFrom.WorkLogic, executeResult);
            }
            SpinWait.SpinUntil(() =>
            {
                return workHeld == WorkHeldFlags.NotHeld;
            });
            work.Worker = null;
            executeResult.ID = work.ID;

            return executeResult;
        }

        internal void ForceStop(bool cancelOtherWorks)
        {
            if (workerState == WorkerStates.Running)
            {
                if (cancelOtherWorks)
                {
                    Cancel();
                }
                thread.Interrupt();
            }
        }

        internal void WaitForResume()
        {
            work.PauseSignal.WaitOne();
        }

        internal void Resume()
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

        internal void SetWork(WorkBase work, bool resetted)
        {
            int originalWorkerState;
            waitingWorkDic[work.ID] = work;
            powerPool.SetWorkOwner(work);
            waitingWorkIDPriorityCollection.Set(work.ID, work.WorkPriority);
            work.Worker = this;
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
                stolenWorkID = waitingWorkIDPriorityCollection.Get();

                if (stolenWorkID != null)
                {
                    if (waitingWorkDic.TryRemove(stolenWorkID, out WorkBase stolenWork))
                    {
                        Interlocked.Decrement(ref waitingWorkCount);
                        stolenWork.Worker = null;
                        stolenList.Add(stolenWork);

                        isContinue = true;
                    }
                }
            }

            return stolenList;
        }

        private void AssignWork()
        {
            while (true)
            {
                WorkBase work = null;
                string waitingWorkID = waitingWorkIDPriorityCollection.Get();

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
                if (waitingWorkCountTemp >= 2 && waitingWorkCountTemp > max)
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
                List<WorkBase> stolenWorkList = null;
                if (count > 0)
                {
                    stolenWorkList = worker.Steal(count);
                }
                Interlocked.Exchange(ref worker.stealingLock, WorkerStealingFlags.Unlocked);
                if (stolenWorkList != null)
                {
                    foreach (WorkBase stolenWork in stolenWorkList)
                    {
                        if (waitingWorkID == null)
                        {
                            waitingWorkID = stolenWork.ID;
                            work = stolenWork;
                            work.Worker = this;
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
                waitingWorkID = waitingWorkIDPriorityCollection.Get();
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
                    powerPool.OnWorkTimedOut(powerPool, new WorkTimedOutEventArgs() { ID = workID });
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

            thread.IsBackground = work.IsBackground;
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
                    RemoveSelf();
                }
                else
                {
                    Interlocked.CompareExchange(ref gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.Disabled);
                }
            }

            killTimer.Stop();
        }

        private void RemoveSelf()
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

                powerPool.InvokeWorkCanceledEvent(executeResult);
                work.InvokeCallback(powerPool, executeResult, powerPool.PowerPoolOption);

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
                    RemoveSelf();

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
