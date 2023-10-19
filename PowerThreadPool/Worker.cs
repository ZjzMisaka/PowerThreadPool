using System.Threading;
using System;
using PowerThreadPool;
using PowerThreadPool.Option;
using System.Collections.Concurrent;
using PowerThreadPool.Collections;
using PowerThreadPool.EventArguments;
using System.Linq;
using System.Collections.Generic;

public class Worker
{
    private Thread thread;

    private string id;
    internal string ID { get => id; set => id = value; }

    internal PriorityQueue<string> waitingWorkIdQueue = new PriorityQueue<string>();
    internal ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
    internal int waittingWorkCount = 0;
    internal object waittingWorkCountLockObj = new object();

    private System.Timers.Timer timer;
    private System.Timers.Timer killTimer;

    private AutoResetEvent runSignal = new AutoResetEvent(false);
    private ConcurrentDictionary<string, AutoResetEvent> waitSignalDic = new ConcurrentDictionary<string, AutoResetEvent>();
    private string workID;
    internal string WorkID { get => workID; set => workID = value; }
    private WorkBase work;
    internal bool killFlag = false;

    private bool running = false;
    private bool alive = false;

    private AutoResetEvent stealSignal = new AutoResetEvent(true);

    internal int WaittingWorkCount
    {
        get 
        { 
            return waittingWorkCount;
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

                    running = true;
                    powerPool.runningWorkerDic[ID] = this;

                    thread.Name = work.ID;
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
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        executeResult = work.SetExecuteResult(null, ex, Status.Failed);
                    }
                    executeResult.ID = work.ID;

                    powerPool.OneWorkEnd(executeResult);
                    work.InvokeCallback(executeResult, powerPool.PowerPoolOption);

                    powerPool.WorkCallbackEnd(workID);

                    if (waitSignalDic.TryRemove(workID, out AutoResetEvent waitSignal))
                    {
                        waitSignal.Set();
                    }

                    running = false;

                    AssignWork(powerPool);

                    powerPool.CheckIdle();
                }
            }
            catch (ThreadInterruptedException ex)
            {
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

                powerPool.WorkCallbackEnd(workID);

                if (waitSignalDic.TryRemove(workID, out AutoResetEvent waitSignal))
                {
                    waitSignal.Set();
                }

                powerPool.aliveWorkerDic.TryRemove(ID, out _);
                powerPool.runningWorkerDic.TryRemove(ID, out _);

                powerPool.CheckIdle();

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
        thread.Interrupt();
        thread.Join();
    }

    internal void SetWork(WorkBase work, PowerPool powerPool)
    {
        waitingWorkIdQueue.Enqueue(work.ID, work.WorkPriority);

        waitingWorkDic[work.ID] = work;
        lock (waittingWorkCountLockObj)
        {
            ++waittingWorkCount;
        }

        waitSignalDic[work.ID] = new AutoResetEvent(false);

        if (!alive)
        {
            alive = true;
            AssignWork(powerPool);
        }
    }

    private void AssignWork(PowerPool powerPool)
    {
        stealSignal.WaitOne();
        stealSignal.Set();
        waitingWorkIdQueue.assignSignal.Reset();
        string waitingWorkId = waitingWorkIdQueue.Dequeue();
        waitingWorkIdQueue.assignSignal.Set();

        if (waitingWorkId == null)
        {
            // Try work stealing
            Worker worker = null;
            List<Worker> workerList = powerPool.aliveWorkerDic.Values.ToList();
            int max = 0;
            foreach (Worker runningWorker in workerList)
            {
                if (runningWorker.WaittingWorkCount > max)
                {
                    max = runningWorker.WaittingWorkCount;
                    worker = runningWorker;
                }
            }
            if (worker != null)
            {
                int count = max / 2;
                count = count < 1 ? 1 : count;
                List<string> stolenWorkIDList = worker.waitingWorkIdQueue.Steal(worker.stealSignal, count);
                count = stolenWorkIDList.Count;
                lock (waittingWorkCountLockObj)
                {
                    waittingWorkCount += count;
                }
                lock (worker.waittingWorkCountLockObj)
                {
                    worker.waittingWorkCount -= count;
                }
                foreach (string stolenWorkID in stolenWorkIDList)
                {
                    if (worker.waitingWorkDic.TryRemove(stolenWorkID, out WorkBase stolenWork))
                    {
                        SetWork(stolenWork, powerPool);
                    }
                }
            }
        }

        WorkBase work = null;
        if (waitingWorkId != null)
        {
            if (waitingWorkDic.TryRemove(waitingWorkId, out work))
            {
                lock (waittingWorkCountLockObj)
                {
                    --waittingWorkCount;
                }
            }
        }

        if (waitingWorkId == null || work == null)
        {
            powerPool.runningWorkerDic.TryRemove(ID, out _);
            alive = false;
            PowerPoolOption powerPoolOption = powerPool.PowerPoolOption;
            powerPool.idleWorkerQueue.Enqueue(this.ID);
            powerPool.idleWorkerDic[this.ID] = this;
            if (powerPoolOption.DestroyThreadOption != null && powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads)
            {
                System.Timers.Timer timer = new System.Timers.Timer(powerPoolOption.DestroyThreadOption.KeepAliveTime);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    if (powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads && powerPool.idleWorkerDic.TryRemove(ID, out _))
                    {
                        powerPool.aliveWorkerDic.TryRemove(ID, out _);
                        Kill();

                        timer.Stop();
                    }
                };
                this.killTimer = timer;
                timer.Start();
            }

            return;
        }

        if (killTimer != null)
        {
            killTimer.Stop();
            killTimer = null;
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

            this.timer = timer;
        }

        this.work = work;
        this.workID = work.ID;
        ThreadPriority threadPriority = work.ThreadPriority;
        if (thread.Priority != threadPriority)
        {
            thread.Priority = threadPriority;
        }
        runSignal.Set();
    }

    internal void Kill()
    {
        killFlag = true;
        runSignal.Set();
    }

    internal void PauseTimer()
    {
        if (timer != null)
        {
            timer.Stop();
        }
    }

    internal void ResumeTimer()
    {
        if (timer != null)
        {
            timer.Start();
        }
    }
}