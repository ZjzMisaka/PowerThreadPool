using System.Threading;
using System;
using PowerThreadPool;
using PowerThreadPool.Option;

public class Worker
{
    private Thread thread;

    private string id;
    public string Id { get => id; set => id = value; }

    private AutoResetEvent runSignal = new AutoResetEvent(false);
    private AutoResetEvent waitSignal = new AutoResetEvent(false);
    private string workID;
    private WorkBase work;
    private bool killFlag = false;

    internal Worker(PowerPool powerPool)
    {
        this.Id = Guid.NewGuid().ToString();
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

                    powerPool.OneThreadEnd(executeResult);
                    work.InvokeCallback(executeResult, powerPool.ThreadPoolOption);

                    powerPool.WorkEnd(workID, false);

                    waitSignal.Set();
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
                    work.InvokeCallback(executeResult, powerPool.ThreadPoolOption);
                }

                powerPool.WorkEnd(workID, true);

                waitSignal.Set();
                return;
            }
        });
        thread.Start();
    }

    public void Wait()
    {
        waitSignal.WaitOne();
    }

    public void ForceStop()
    {
        thread.Interrupt();
        thread.Join();
    }

    internal void AssignTask(WorkBase work)
    {
        this.work = work;
        this.workID = work.ID;
        ThreadPriority threadPriority = work.GetThreadPriority();
        thread.Priority = threadPriority;
        runSignal.Set();
    }

    internal void Kill()
    {
        killFlag = true;
        runSignal.Set();
    }
}