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
        internal int gettedLock = 0;

        private PriorityQueue<string> waitingWorkIDQueue = new PriorityQueue<string>();
        private ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

        private System.Timers.Timer timeoutTimer;
        private System.Timers.Timer killTimer;

        private AutoResetEvent runSignal = new AutoResetEvent(false);
        private ConcurrentDictionary<string, AutoResetEvent> waitSignalDic = new ConcurrentDictionary<string, AutoResetEvent>();
        private string workID;
        internal string WorkID { get => workID; set => workID = value; }
        private WorkBase work;
        private bool killFlag = false;
        private int stealingLock = 0;

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

                        powerPool.WorkCallbackEnd(workID, true);

                        if (waitSignalDic.TryRemove(workID, out AutoResetEvent waitSignal))
                        {
                            waitSignal.Set();
                        }

                        AssignWork(powerPool);
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    Interlocked.Exchange(ref gettedLock, -100);
                    Interlocked.Exchange(ref workerState, 2);

                    powerPool.runningWorkerSet.Remove(ID);
                    if (powerPool.aliveWorkerDic.TryRemove(ID, out _))
                    {
                        Interlocked.Decrement(ref powerPool.aliveWorkerCount);
                        powerPool.aliveWorkerList = powerPool.aliveWorkerDic.Values;
                    }
                    powerPool.idleWorkerDic.TryRemove(ID, out _);

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

                    powerPool.WorkCallbackEnd(workID, false);

                    IEnumerable<WorkBase> waitingWorkList = waitingWorkDic.Values;
                    foreach (WorkBase work in waitingWorkList)
                    {
                        powerPool.SetWork(work);
                    }

                    if (waitSignalDic.TryRemove(workID, out AutoResetEvent waitSignal))
                    {
                        waitSignal.Set();
                    }

                    powerPool.CheckPoolIdle();

                    return;
                }
            });
            thread.Start();
        }

        public bool Wait(string workID)
        {
            if (waitSignalDic.TryGetValue(workID, out AutoResetEvent autoResetEvent))
            {
                autoResetEvent.WaitOne();
                return true;
            }
            return false;
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

        public void ForceStop(string workID)
        {
            if (this.workID == workID)
            {
                thread.Interrupt();
                thread.Join();
            }
            else
            {
                waitingWorkDic.TryRemove(workID, out _);
            }
        }

        internal void SetWork(WorkBase work, PowerPool powerPool, bool stolenWork)
        {
            lock (powerPool)
            {
                waitingWorkIDQueue.Enqueue(work.ID, work.WorkPriority);
                waitingWorkDic[work.ID] = work;
                waitSignalDic[work.ID] = new AutoResetEvent(false);
                Interlocked.Increment(ref waitingWorkCount);
            }

            int originalWorkerState = Interlocked.CompareExchange(ref workerState, 1, 0);
            if (!stolenWork)
            {
                Interlocked.Decrement(ref gettedLock);
            }

            if (originalWorkerState == 0)
            {
                powerPool.runningWorkerSet.Add(ID);
                AssignWork(powerPool);
            }
        }

        internal List<WorkBase> Steal(int count, PowerPool powerPool)
        {
            List<WorkBase> stolenList = new List<WorkBase>();
            for (int i = 0; i < count; ++i)
            {
                string stolenWorkID = null;
                lock (powerPool)
                {
                    stolenWorkID = waitingWorkIDQueue.Dequeue();
                }
                if (stolenWorkID == null)
                {
                    return stolenList;
                }

                if (waitingWorkDic.TryRemove(stolenWorkID, out WorkBase stolenWork))
                {
                    Interlocked.Decrement(ref waitingWorkCount);
                    stolenList.Add(stolenWork);
                }
                else
                {
                    --i;
                }
            }
            return stolenList;
        }

        private void AssignWork(PowerPool powerPool)
        {
            while (true)
            {
                string waitingWorkID = null;
                WorkBase work = null;

                lock (powerPool)
                {
                    waitingWorkID = waitingWorkIDQueue.Dequeue();
                }
                if (waitingWorkID == null)
                {
                    Worker worker = null;
                    int waittingWorkCountTemp = 0;
                    foreach (Worker runningWorker in powerPool.aliveWorkerList)
                    {
                        if (Interlocked.CompareExchange(ref runningWorker.stealingLock, 1, 0) == 1)
                        {
                            continue;
                        }
                        if (worker != null)
                        {
                            Interlocked.Exchange(ref worker.stealingLock, 0);
                        }

                        waittingWorkCountTemp = runningWorker.WaitingWorkCount;
                        worker = runningWorker;

                        break;
                    }
                    if (worker != null)
                    {
                        int count = waittingWorkCountTemp / 2;
                        if (count > 0)
                        {
                            List<WorkBase> stolenWorkList = worker.Steal(count, powerPool);

                            foreach (WorkBase stolenWork in stolenWorkList)
                            {
                                SetWork(stolenWork, powerPool, true);
                            }
                        }

                        Interlocked.Exchange(ref worker.stealingLock, 0);
                    }
                }

                if (waitingWorkID == null)
                {
                    lock (powerPool)
                    {
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

                            powerPool.runningWorkerSet.Remove(ID);
                            PowerPoolOption powerPoolOption = powerPool.PowerPoolOption;
                            powerPool.idleWorkerQueue.Enqueue(this.ID);
                            powerPool.idleWorkerDic[this.ID] = this;

                            powerPool.CheckPoolIdle();

                            if (powerPoolOption.DestroyThreadOption != null && powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads)
                            {
                                this.killTimer = new System.Timers.Timer(powerPoolOption.DestroyThreadOption.KeepAliveTime);
                                try
                                {
                                    killTimer.AutoReset = false;
                                    killTimer.Elapsed += (s, e) =>
                                    {
                                        SpinWait.SpinUntil(() =>
                                        {
                                            int gettedStatus = Interlocked.CompareExchange(ref gettedLock, -100, 0);
                                            return (gettedStatus == 0 || gettedStatus == -100);
                                        });
                                        int originalState = Interlocked.CompareExchange(ref workerState, 2, 0);
                                        if (originalState == 0)
                                        {
                                            if (powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads && powerPool.idleWorkerDic.TryRemove(ID, out _))
                                            {
                                                if (powerPool.aliveWorkerDic.TryRemove(ID, out _))
                                                {
                                                    Interlocked.Decrement(ref powerPool.aliveWorkerCount);
                                                    powerPool.aliveWorkerList = powerPool.aliveWorkerDic.Values;
                                                }
                                                Kill();

                                                killTimer.Enabled = false;
                                            }
                                        }
                                        else
                                        {
                                            killTimer.Enabled = false;
                                        }
                                    };

                                    killTimer.Start();
                                }
                                catch
                                {
                                }
                            }

                            return;
                        }
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
            Interlocked.Exchange(ref waitingWorkCount, 0);
        }

        internal bool Cancel(string id)
        {
            if (waitingWorkDic.TryRemove(id, out _))
            {
                Interlocked.Decrement(ref waitingWorkCount);
                return true;
            }
            return false;
        }
    }
}