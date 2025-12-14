using System.Reflection;
using PowerThreadPool.Helpers.Asynchronous;
using Xunit.Abstractions;

namespace UnitTest
{
    public class AsyncManualResetEventTest
    {
        private readonly ITestOutputHelper _output;

        public AsyncManualResetEventTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestCtorInitialStateFalseIsNotSet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(false);

            Assert.False(evt.IsSet);
            Assert.False(evt.WaitAsync().IsCompleted);
        }

        [Fact]
        public void TestCtorInitialStateTrueIsSetAndTaskCompleted()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(true);

            Assert.True(evt.IsSet);
            Assert.True(evt.WaitAsync().IsCompleted);
        }

        [Fact]
        public async Task TestWaitAsyncBlocksUntilSet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(false);

            Task waitTask = evt.WaitAsync();

            Assert.False(waitTask.IsCompleted);

            evt.Set();

            await waitTask.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.True(waitTask.IsCompletedSuccessfully);
            Assert.True(evt.IsSet);
        }

        [Fact]
        public async Task TestSetMultipleTimesDoesNotThrowAndRemainsSet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(false);

            evt.Set();
            evt.Set();
            evt.Set();

            Assert.True(evt.IsSet);
            await evt.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task TestResetWhenNotSetDoesNothingAndWaitStillBlocks()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(false);

            Task waitTask = evt.WaitAsync();

            evt.Reset();

            Assert.False(evt.IsSet);
            Assert.False(waitTask.IsCompleted);

            evt.Set();
            await waitTask.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.True(waitTask.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task TestResetWhenSetCausesSubsequentWaitToBlock()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(true);

            Assert.True(evt.IsSet);
            await evt.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1));

            evt.Reset();
            Assert.False(evt.IsSet);

            Task newWaitTask = evt.WaitAsync();
            Assert.False(newWaitTask.IsCompleted);

            evt.Set();
            await newWaitTask.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.True(newWaitTask.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task TestMultipleWaitersAreAllReleasedWhenSet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(false);

            const int waiterCount = 10;
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < waiterCount; ++i)
            {
                tasks.Add(evt.WaitAsync());
            }

            Assert.All(tasks, t => Assert.False(t.IsCompleted));

            evt.Set();

            await Task.WhenAll(tasks.Select(t => t.WaitAsync(TimeSpan.FromSeconds(1))));

            Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
            Assert.True(evt.IsSet);
        }

        [Fact]
        public async Task TestSetBeforeWaitAsyncWaitAsyncReturnsCompletedTask()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(false);

            evt.Set();

            Task waitTask1 = evt.WaitAsync();
            Task waitTask2 = evt.WaitAsync();

            Assert.True(waitTask1.IsCompleted);
            Assert.True(waitTask2.IsCompleted);

            await waitTask1.WaitAsync(TimeSpan.FromSeconds(1));
            await waitTask2.WaitAsync(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task TestResetThenSetReleasesNewWaitersOnly()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(true);

            Task firstWait = evt.WaitAsync();
            Assert.True(firstWait.IsCompleted);

            evt.Reset();
            Assert.False(evt.IsSet);

            Task secondWait = evt.WaitAsync();
            Assert.False(secondWait.IsCompleted);

            evt.Set();
            await secondWait.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.True(secondWait.IsCompletedSuccessfully);
            Assert.True(evt.IsSet);
        }

        [Fact]
        public async Task TestResetLoopBranchCanHandleConcurrentResetAndSet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(true);

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            int resetCount = 0;
            int setCount = 0;

            Task resetTask = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    evt.Reset();
                    Interlocked.Increment(ref resetCount);
                }
            }, cts.Token);

            Task setTask = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    evt.Set();
                    Interlocked.Increment(ref setCount);
                }
            }, cts.Token);

            await Task.Delay(300, cts.Token);
            cts.Cancel();

            try { await Task.WhenAll(resetTask, setTask); } catch (OperationCanceledException) { }

            Assert.True(resetCount > 0);
            Assert.True(setCount > 0);

            evt.Set();
            Task waitTask = evt.WaitAsync();
            await waitTask.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.True(waitTask.IsCompletedSuccessfully);
        }

        [Fact]
        public async void TestResetWhenCompareExchangeFailsEventRemainsConsistent()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            AsyncManualResetEvent evt = new AsyncManualResetEvent(true);

            Assert.True(evt._tcs.Task.IsCompleted);
            Assert.True(evt.IsSet);

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            CancellationToken token = cts.Token;

            Exception resetLoopException = null;
            Exception tcsMutationException = null;

            Thread resetThread = new Thread(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        evt.Reset();
                    }
                }
                catch (Exception ex)
                {
                    resetLoopException = ex;
                }
            })
            {
                IsBackground = true
            };

            Thread mutateThread = new Thread(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var old = evt._tcs;
                        var newTcs = AsyncManualResetEvent.NewTcs();
                        newTcs.TrySetResult(true);

                        evt._tcs = newTcs;

                        Thread.SpinWait(50);
                    }
                }
                catch (Exception ex)
                {
                    tcsMutationException = ex;
                }
            })
            {
                IsBackground = true
            };

            resetThread.Start();
            mutateThread.Start();

            Thread.Sleep(500);
            cts.Cancel();

            resetThread.Join(TimeSpan.FromSeconds(1));
            mutateThread.Join(TimeSpan.FromSeconds(1));

            Assert.Null(resetLoopException);
            Assert.Null(tcsMutationException);

            evt.Reset();
            Assert.False(evt.IsSet);
            Assert.False(evt._tcs.Task.IsCompleted);

            Task waitTask = evt.WaitAsync();
            Assert.False(waitTask.IsCompleted);

            evt.Set();
            Assert.True(evt.IsSet);

            await waitTask.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.True(waitTask.IsCompletedSuccessfully);
        }
    }
}
