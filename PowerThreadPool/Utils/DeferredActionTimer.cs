using System;
using System.Diagnostics;
using System.Threading;

internal class DeferredActionTimer : IDisposable
{
    private Timer _timer;
    private Action _callback;
    private int _remainingMilliseconds;
    private Stopwatch _stopwatch;
    private bool _isPaused;
    private bool _isRecurring;

    internal int DueTime { get; set; }

    internal DeferredActionTimer(Action action = null, bool isRecurring = false)
    {
        _callback = action;
        _isRecurring = isRecurring;
        _timer = new Timer(_ =>
        {
            if (!isRecurring)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            
            _callback.Invoke();
        });
        _stopwatch = new Stopwatch();
        _isPaused = false;
        _remainingMilliseconds = Timeout.Infinite;
    }

    internal void Set(int milliseconds)
    {
        _remainingMilliseconds = milliseconds;
        _isPaused = false;
        _stopwatch.Reset();
        _stopwatch.Start();
        DueTime = milliseconds;
        _timer.Change(milliseconds, _isRecurring ? DueTime : Timeout.Infinite);
    }

    internal void Set(int milliseconds, Action action)
    {
        _callback = action;
        Set(milliseconds);
    }

    internal void Cancel()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        _remainingMilliseconds = Timeout.Infinite;
        _stopwatch.Reset();
        _isPaused = false;
    }

    internal void Pause()
    {
        if (_isPaused || _remainingMilliseconds == Timeout.Infinite)
        {
            return;
        }

        _stopwatch.Stop();
        _timer.Change(Timeout.Infinite, Timeout.Infinite);

        _remainingMilliseconds -= (int)_stopwatch.ElapsedMilliseconds;
        if (_remainingMilliseconds < 0)
        {
            _remainingMilliseconds = 0;
        }

        _isPaused = true;
    }

    internal void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        _stopwatch.Restart();
        _timer.Change(_remainingMilliseconds, _isRecurring ? DueTime : Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer.Dispose();
        _stopwatch.Stop();
    }
}
