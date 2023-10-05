using System.Threading;
using System;
using PowerThreadPool;
using PowerThreadPool.Option;
using System.Collections.Concurrent;
using PowerThreadPool.Collections;
using PowerThreadPool.EventArguments;
using System.Linq;

public class Worker
{
    private Thread thread;
    object lockObj = new object();

    private string id;
    internal string ID { get => id; set => id = value; }

    private PriorityQueue<string> waitingWorkIdQueue = new PriorityQueue<string>();
    private ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

    private System.Timers.Timer timer;

    private AutoResetEvent runSignal = new AutoResetEvent(false);
    private ConcurrentDictionary<string, AutoResetEvent> waitSignalDic = new ConcurrentDictionary<string, AutoResetEvent>();
    private string workID;
    internal string WorkID { get => workID; set => workID = value; }
    private WorkBase work;
    private bool killFlag = false;

    private bool running = false;

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

                    waitSignalDic[workID].Set();

                    running = false;

                    AssignWork(powerPool);
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

                waitSignalDic[workID].Set();
                return;
            }
        });
        thread.Start();
    }

    public void Wait()
    {
        while (true)
        {
            AutoResetEvent autoResetEvent = waitSignalDic.Values.FirstOrDefault();

            if (autoResetEvent != null)
            {
                autoResetEvent.WaitOne();
            }
            else
            {
                break;
            }
        }
    }

    public bool Wait(string workID)
    {
        lock (lockObj)
        {
            if (this.workID == workID)
            {
                waitSignalDic[workID].WaitOne();
                return true;
            }
            return false;
        }
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

        if (!running)
        {
            AssignWork(powerPool);
        }
    }

    private void AssignWork(PowerPool powerPool)
    {
        WorkBase work = waitingWorkDic[waitingWorkIdQueue.Dequeue()];

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
        timer.Stop();
    }

    internal void ResumeTimer()
    {
        timer.Start();
    }
}