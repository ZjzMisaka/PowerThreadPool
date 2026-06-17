using System.Collections.Concurrent;

namespace Benchmark
{
    public sealed class PerPriorityLaneScheduler : IDisposable
    {
        private readonly PriorityQueue<(Task task, Lane lane), Key> _queue = new();
        private readonly object _lock = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly Task[] _workers;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly ConcurrentDictionary<int, Lane> _lanes = new();
        private long _seq;

        public PerPriorityLaneScheduler(int workerCount)
        {
            _workers = new Task[workerCount];
            for (int i = 0; i < workerCount; i++)
                _workers[i] = Task.Run(WorkLoopAsync);
        }

        public Task Run(Func<Task> action, int priority)
        {
            var lane = _lanes.GetOrAdd(priority, p => new Lane(this, p));
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<Task> wrapper = async () =>
            {
                try { await action().ConfigureAwait(true); tcs.SetResult(); }
                catch (OperationCanceledException) { tcs.TrySetCanceled(); }
                catch (Exception ex) { tcs.SetException(ex); }
            };

            Task.Factory.StartNew(
                wrapper,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                lane);

            return tcs.Task;
        }

        private void Enqueue(Task task, Lane lane, int priority)
        {
            lock (_lock) { _queue.Enqueue((task, lane), new Key(priority, _seq++)); }
            _signal.Release();
        }

        private IEnumerable<Task> Snapshot()
        {
            lock (_lock)
                return _queue.UnorderedItems.Select(x => x.Element.task).ToArray();
        }

        private async Task WorkLoopAsync()
        {
            while (true)
            {
                try { await _signal.WaitAsync(_shutdown.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }

                (Task task, Lane lane) item = default;
                bool has = false;
                lock (_lock)
                {
                    if (_queue.Count > 0) { item = _queue.Dequeue(); has = true; }
                }

                if (has) item.lane.Execute(item.task);
            }
        }

        public void Dispose() => _shutdown.Cancel();

        private readonly record struct Key(int Priority, long Seq) : IComparable<Key>
        {
            public int CompareTo(Key other)
            {
                int c = Priority.CompareTo(other.Priority);
                return c != 0 ? c : Seq.CompareTo(other.Seq);
            }
        }

        private sealed class Lane : TaskScheduler
        {
            private readonly PerPriorityLaneScheduler _hub;
            private readonly int _priority;

            public Lane(PerPriorityLaneScheduler hub, int priority)
            {
                _hub = hub;
                _priority = priority;
            }

            protected override void QueueTask(Task task) => _hub.Enqueue(task, this, _priority);
            protected override bool TryExecuteTaskInline(Task task, bool wasQueued) => false;
            protected override IEnumerable<Task> GetScheduledTasks() => _hub.Snapshot();

            internal bool Execute(Task task) => TryExecuteTask(task);
        }
    }
}
