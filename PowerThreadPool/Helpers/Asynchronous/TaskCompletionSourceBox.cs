using System;
using System.Threading.Tasks;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class TaskCompletionSourceBox<T> : ITaskCompletionSource
    {
        private readonly TaskCompletionSource<T> _tcs = new TaskCompletionSource<T>();

        public Task Task => _tcs.Task;
        public void SetResult(object result) => _tcs.SetResult((T)result);
        public void SetException(Exception ex) => _tcs.SetException(ex);
        public void SetCanceled() => _tcs.SetCanceled();

        public Task<T> TypedTask => _tcs.Task;
    }
}
