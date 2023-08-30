using System.Threading;
using System;
using PowerThreadPool;
using PowerThreadPool.Option;

public class Worker
{
    private Thread thread;

    private AutoResetEvent runSignal = new AutoResetEvent(false);
    private AutoResetEvent waitSignal = new AutoResetEvent(false);
    private string guid;
    private WorkBase work;
    private bool killFlag = false;

    internal Worker(PowerPool powerPool)
    {
        thread = new Thread(() =>
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
                catch (Exception ex)
                {
                    executeResult = work.SetExecuteResult(null, ex, Status.Failed);
                }

                powerPool.InvokeThreadEndEvent(executeResult);
                work.InvokeCallback(executeResult, powerPool.ThreadPoolOption);

                powerPool.WorkEnd(guid);

                waitSignal.Set();
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
        this.guid = work.ID;
        runSignal.Set();
    }

    internal void Kill()
    {
        killFlag = true;
        runSignal.Set();
    }
}