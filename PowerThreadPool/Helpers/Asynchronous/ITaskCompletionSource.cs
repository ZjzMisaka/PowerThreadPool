using System;
using System.Threading.Tasks;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal interface ITaskCompletionSource
    {
        Task Task { get; }
        void SetResult(object result);
        void SetException(Exception ex);
        void SetCanceled();
    }
}
