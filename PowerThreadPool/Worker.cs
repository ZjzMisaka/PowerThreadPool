using System.Threading;
using System;
using PowerThreadPool.Option;
using System.Collections.Concurrent;
using PowerThreadPool.Collections;
using PowerThreadPool.EventArguments;
using System.Linq;
using System.Collections.Generic;

namespace PowerThreadPool
{
    public class Worker
    {
        internal Thread thread;

        private string id;
        internal string ID { get => id; set => id = value; }

        /// <summary>
        /// 0: Idle, 1: Running, 2: ToBeDisposed
        /// </summary>
        internal int workerState = 0;
        /// <summary>
        /// 0: Waiting for lock, 1: Locked, 2: Disabled
        /// </summary>
        internal int gettedLock = 0;

        private PriorityQueue<string> waitingWorkIDQueue = new PriorityQueue<string>();
        private ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

        private System.Timers.Timer timeoutTimer;
        private System.Timers.Timer killTimer;

        private AutoResetEvent runSignal = new AutoResetEvent(false);
        private string workID;
        internal string WorkID { get => workID; set => workID = value; }
        private WorkBase work;
        private bool killFlag = false;
        private int stealingLock = 0;

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
                    Interlocked.Exchange(ref gettedLock, -100);
                    int origWorkState = Interlocked.Exchange(ref workerState, 2);

                    if (work.LongRunning)
                    {
                        Interlocked.Decrement(ref powerPool.longRunningWorkerCount);
                        this.LongRunning = false;
                    }

                    if (origWorkState == 1)
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

                    return;
                }
            });
            thread.Start();
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
            if (workerState == 1)
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

            WorkBase workerToPause;
            if (!waitingWorkDic.TryGetValue(workID, out workerToPause))
            {
                if (workID == WorkID)
                {
                    workerToPause = work;
                }
            }

            if (workerToPause != null)
            {
                if (workerToPause.PauseSignal == null)
                {
                    workerToPause.PauseSignal = new ManualResetEvent(true);
                }

                workerToPause.IsPausing = true;
                workerToPause.PauseSignal.Reset();
                res = true;
            }

            return res;
        }

        public bool Resume(string workID)
        {
            bool res = false;

            WorkBase workerToResume;
            if (!waitingWorkDic.TryGetValue(workID, out workerToResume))
            {
                if (workID == WorkID)
                {
                    workerToResume = work;
                }
            }

            if (workerToResume != null)
            {
                workerToResume.IsPausing = false;
                workerToResume.PauseSignal.Set();
                res = true;
            }

            return res;
        }

        public bool Resume()
        {
            bool res = false;

            foreach (WorkBase workerToResume in waitingWorkDic.Values)
            {
                if (workerToResume.IsPausing)
                {
                    workerToResume.IsPausing = false;
                    workerToResume.PauseSignal.Set();
                }
            }
            if (work.IsPausing)
            {
                work.IsPausing = false;
                work.PauseSignal.Set();
            }

            return res;
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

        internal void SetWork(WorkBase work, bool stolenWork)
        {
            int originalWorkerState;
            waitingWorkDic[work.ID] = work;
            powerPool.SetWorkOwner(work.ID, this);
            waitingWorkIDQueue.Enqueue(work.ID, work.WorkPriority);
            Interlocked.Increment(ref waitingWorkCount);
            originalWorkerState = Interlocked.CompareExchange(ref workerState, 1, 0);

            if (!stolenWork)
            {
                Interlocked.Exchange(ref gettedLock, 0);
            }

            if (originalWorkerState == 0)
            {
                Interlocked.Increment(ref powerPool.runningWorkerCount);
                AssignWork();
            }
        }

        internal List<WorkBase> Steal(int count)
        {
            List<WorkBase> stolenList = new List<WorkBase>();

            while (stolenList.Count < count)
            {
                string stolenWorkID;
                stolenWorkID = waitingWorkIDQueue.Dequeue();

                if (stolenWorkID == null)
                {
                    return stolenList;
                }

                if (waitingWorkDic.TryRemove(stolenWorkID, out WorkBase stolenWork))
                {
                    Interlocked.Decrement(ref waitingWorkCount);
                    stolenList.Add(stolenWork);
                }
            }

            return stolenList;
        }

        private void AssignWork()
        {
            while (true)
            {
                string waitingWorkID = null;
                WorkBase work = null;

                waitingWorkID = waitingWorkIDQueue.Dequeue();

                if (waitingWorkID == null && powerPool.aliveWorkerCount <= powerPool.PowerPoolOption.MaxThreads)
                {
                    Worker worker = null;
                    int max = 0;
                    foreach (Worker runningWorker in powerPool.aliveWorkerList)
                    {
                        if (runningWorker.workerState != 1)
                        {
                            continue;
                        }

                        int waitingWorkCountTemp = runningWorker.WaitingWorkCount;
                        if (waitingWorkCountTemp > max)
                        {
                            if (Interlocked.CompareExchange(ref runningWorker.stealingLock, 1, 0) == 1)
                            {
                                continue;
                            }
                            if (worker != null)
                            {
                                Interlocked.Exchange(ref worker.stealingLock, 0);
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
                                SetWork(stolenWork, true);
                            }
                        }

                        Interlocked.Exchange(ref worker.stealingLock, 0);
                    }
                }

                if (waitingWorkID == null)
                {
                    SpinWait.SpinUntil(() =>
                    {
                        int gettedLockOrig = Interlocked.CompareExchange(ref gettedLock, 374, 0);
                        return (gettedLockOrig == 0);
                    });

                    waitingWorkID = waitingWorkIDQueue.Dequeue();

                    if (waitingWorkID != null)
                    {
                        if (waitingWorkDic.TryRemove(waitingWorkID, out work))
                        {
                            Interlocked.Decrement(ref waitingWorkCount);
                        }
                    }

                    if (waitingWorkID == null || work == null)
                    {
                        Interlocked.Exchange(ref workerState, 0);

                        Interlocked.CompareExchange(ref gettedLock, 0, 374);

                        Interlocked.Decrement(ref powerPool.runningWorkerCount);
                        PowerPoolOption powerPoolOption = powerPool.PowerPoolOption;

                        powerPool.idleWorkerDic[this.ID] = this;
                        Interlocked.Increment(ref powerPool.idleWorkerCount);
                        powerPool.idleWorkerQueue.Enqueue(this.ID);

                        powerPool.CheckPoolIdle();

                        if (powerPoolOption.DestroyThreadOption != null && powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads)
                        {
                            this.killTimer = new System.Timers.Timer(powerPoolOption.DestroyThreadOption.KeepAliveTime);
                            try
                            {
                                killTimer.AutoReset = false;
                                killTimer.Elapsed += (s, e) =>
                                {
                                    if (powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads)
                                    {
                                        SpinWait.SpinUntil(() =>
                                        {
                                            int gettedStatus = Interlocked.CompareExchange(ref gettedLock, -100, 0);
                                            return (gettedStatus == 0 || gettedStatus == -100);
                                        });
                                        int originalState = Interlocked.CompareExchange(ref workerState, 2, 0);
                                        if (originalState == 0)
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
                                            Interlocked.Exchange(ref gettedLock, 0);
                                        }
                                    }
                                    killTimer.Enabled = false;
                                };

                                killTimer.Start();
                            }
                            catch
                            {
                            }
                        }

                        return;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref gettedLock, 0, 374);
                    }
                }

                if (killTimer != null)
                {
                    killTimer.Stop();
                    killTimer = null;
                }

                if (work == null)
                {
                    if (waitingWorkDic.TryRemove(waitingWorkID, out work))
                    {
                        Interlocked.Decrement(ref waitingWorkCount);
                    }
                    else
                    {
                        continue;
                    }
                }

                Interlocked.Decrement(ref powerPool.waitingWorkCount);

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
                runSignal.Set();
                break;
            }
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
            waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
            int count = Interlocked.Exchange(ref waitingWorkCount, 0);
            Interlocked.Exchange(ref powerPool.waitingWorkCount, powerPool.waitingWorkCount - count);
        }

        internal bool Cancel(string id)
        {
            if (waitingWorkDic.TryRemove(id, out _))
            {
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
    }
}