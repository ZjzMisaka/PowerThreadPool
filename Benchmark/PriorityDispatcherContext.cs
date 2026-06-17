namespace Benchmark
{
    public sealed class PriorityDispatcherContext : IDisposable
    {
        private readonly PriorityQueue<Action, int> _queue = new();
        private readonly object _lock = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly Task[] _workers;
        private readonly CancellationTokenSource _shutdown = new();

        public PriorityDispatcherContext(int workerCount)
        {
            _workers = new Task[workerCount];
            for (int i = 0; i < workerCount; i++)
                _workers[i] = Task.Run(WorkLoopAsync);
        }

        public Task Run(Func<Task> action, int priority)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var ctx = new PriorityContext(this, priority);

            Enqueue(() =>
            {
                SynchronizationContext.SetSynchronizationContext(ctx);
                _ = RunRootAsync(action, tcs);
            }, priority);

            return tcs.Task;
        }

        private static async Task RunRootAsync(Func<Task> action, TaskCompletionSource tcs)
        {
            try { await action().ConfigureAwait(true); tcs.SetResult(); }
            catch (OperationCanceledException) { tcs.TrySetCanceled(); }
            catch (Exception ex) { tcs.SetException(ex); }
        }

        private void Enqueue(Action work, int priority)
        {
            lock (_lock) { _queue.Enqueue(work, priority); }
            _signal.Release();
        }

        private async Task WorkLoopAsync()
        {
            while (true)
            {
                try { await _signal.WaitAsync(_shutdown.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }

                Action work = null;
                lock (_lock)
                {
                    if (_queue.Count > 0) work = _queue.Dequeue();
                }

                if (work != null)
                {
                    try { work(); }
                    finally { SynchronizationContext.SetSynchronizationContext(null); }
                }
            }
        }

        public void Dispose() => _shutdown.Cancel();

        private sealed class PriorityContext : SynchronizationContext
        {
            private readonly PriorityDispatcherContext _scheduler;
            private readonly int _priority;

            public PriorityContext(PriorityDispatcherContext scheduler, int priority)
            {
                _scheduler = scheduler;
                _priority = priority;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                _scheduler.Enqueue(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(this);
                    d(state);
                }, _priority);
            }

            public override SynchronizationContext CreateCopy() => this;
        }
    }
}
