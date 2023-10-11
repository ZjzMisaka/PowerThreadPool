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

    private System.Timers.Timer timer;

    private AutoResetEvent runSignal = new AutoResetEvent(false);
    private ConcurrentDictionary<string, AutoResetEvent> waitSignalDic = new ConcurrentDictionary<string, AutoResetEvent>();
    private string workID;
    internal string WorkID { get => workID; set => workID = value; }
    private WorkBase work;
    private bool killFlag = false;

    private bool running = false;

    private AutoResetEvent stealSignal = new AutoResetEvent(true);

    internal int WaittingWorkCount
    {
        get 
        { 
            return waitingWorkIdQueue.Count;
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

                    powerPool.WorkCallbackEnd(workID, false);

                    if (waitSignalDic.TryRemove(workID, out AutoResetEvent waitSignal))
                    {
                        waitSignal.Set();
                    }

                    running = false;

                    AssignWork(powerPool);
                    powerPool.runningWorkerDic.TryRemove(ID, out _);

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

                powerPool.WorkCallbackEnd(workID, true);

                if (waitSignalDic.TryRemove(workID, out AutoResetEvent waitSignal))
                {
                    waitSignal.Set();
                }

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

        waitSignalDic[work.ID] = new AutoResetEvent(false);

        if (!running)
        {
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
            Worker worker = null;
            List<Worker> workerList = powerPool.runningWorkerDic.Values.ToList();
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
                string stolenWorkID = worker.waitingWorkIdQueue.Steal(worker.stealSignal, 1);
                if (worker.waitingWorkDic.TryRemove(workID, out WorkBase stolenWork))
                {
                    SetWork(stolenWork, powerPool);
                }
            }
        }

        if (waitingWorkId == null)
        {
            PowerPoolOption powerPoolOption = powerPool.PowerPoolOption;
            powerPool.idleWorkerDic[this.ID] = this;
            if (powerPoolOption.DestroyThreadOption != null && powerPool.IdleThreadCount > powerPoolOption.DestroyThreadOption.MinThreads)
            {
                System.Timers.Timer timer = new System.Timers.Timer(powerPoolOption.DestroyThreadOption.KeepAliveTime);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    if (powerPool.IdleThreadCount > powerPoolOption.DestroyThreadOption.MinThreads && powerPool.idleWorkerDic.TryRemove(ID, out _))
                    {
                        Kill();

                        timer.Stop();
                    }
                };
                timer.Start();
            }

            return;
        }

        WorkBase work = waitingWorkDic[waitingWorkId];

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