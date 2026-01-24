using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.Options
{
    internal static class WorkOptionExtensions
    {
        internal static void SetCallback<TResult>(
            this WorkOption option,
            Action<ExecuteResult<TResult>> callback)
        {
            option.Callback = baseResult =>
            {
                if (baseResult is ExecuteResult<TResult> typedResult)
                {
                    callback(typedResult);
                }
            };
        }
    }
}
