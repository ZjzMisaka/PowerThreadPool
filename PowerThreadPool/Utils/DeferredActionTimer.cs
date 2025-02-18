using System;
using System.Threading;

internal class DeferredActionTimer : IDisposable
{
    private Timer _timer;

    internal DeferredActionTimer(Action action)
    {
        _timer = new Timer(_ =>
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            action.Invoke();
        });
    }

    internal void Set(int milliseconds)
    {
        _timer.Change(milliseconds, Timeout.Infinite);
    }

    internal void Cancel()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
