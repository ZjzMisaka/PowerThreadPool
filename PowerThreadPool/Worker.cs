using System.Threading;
using System;
using PowerThreadPool;
using PowerThreadPool.Option;

public class Worker
{
    private Thread thread;

    private AutoResetEvent signal = new AutoResetEvent(false);
    private string guid;
    private ExecuteResultBase executeResult;
    private WorkBase work;

    internal Worker(PowerPool powerPool)
    {
        thread = new Thread(() =>
        {
            while (true)
            {
                signal.WaitOne();


                executeResult = new ExecuteResult<object>();
                try
                {
                    object result = work.Execute();
                    executeResult.SetExecuteResult(result, null, Status.Succeed);
                }
                catch (Exception ex)
                {
                    executeResult.SetExecuteResult(null, ex, Status.Failed);
                }

                powerPool.InvokeThreadEndEvent(executeResult);
                work.InvokeCallback(executeResult, powerPool.ThreadPoolOption);

                powerPool.WorkEnd(guid);
            }
        });
        thread.Start();
    }

    public void Wait()
    {
        thread.Join();
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
        signal.Set();
    }
}