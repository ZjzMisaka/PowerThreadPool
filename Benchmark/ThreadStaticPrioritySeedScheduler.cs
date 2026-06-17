namespace Benchmark
{
    public sealed class ThreadStaticPrioritySeedScheduler : TaskScheduler, IDisposable
    {
        private const int LowestPriority = int.MaxValue;

        private readonly PriorityQueue<Task, int> _queue = new();
        private readonly object _lock = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly Task[] _workers;
        private readonly CancellationTokenSource _shutdown = new();

        [ThreadStatic]
        private static int? s_pendingPriority;

        public ThreadStaticPrioritySeedScheduler(int workerCount)
        {
            _workers = new Task[workerCount];
            for (int i = 0; i < workerCount; i++)
                _workers[i] = Task.Run(WorkLoopAsync);
        }

        public Task Run(Func<Task> action, int priority)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<Task> wrapper = async () =>
            {
                try { await action().ConfigureAwait(true); tcs.SetResult(); }
                catch (OperationCanceledException) { tcs.TrySetCanceled(); }
                catch (Exception ex) { tcs.SetException(ex); }
            };

            s_pendingPriority = priority;
            try
            {
                Task.Factory.StartNew(
                    wrapper,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    this);
            }
            finally
            {
                s_pendingPriority = null;
            }
            return tcs.Task;
        }

        protected override void QueueTask(Task task)
        {
            int priority = s_pendingPriority ?? LowestPriority;
            lock (_lock) { _queue.Enqueue(task, priority); }
            _signal.Release();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            => false;

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            lock (_lock)
                return _queue.UnorderedItems.Select(x => x.Element).ToArray();
        }

        private async Task WorkLoopAsync()
        {
            while (true)
            {
                try { await _signal.WaitAsync(_shutdown.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }

                Task task = null;
                lock (_lock)
                {
                    if (_queue.Count > 0) task = _queue.Dequeue();
                }

                if (task != null)
                    TryExecuteTask(task);
            }
        }

        public void Dispose() => _shutdown.Cancel();
    }
}
