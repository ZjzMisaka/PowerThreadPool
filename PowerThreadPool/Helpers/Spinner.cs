using System;
using System.Diagnostics;
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
using System.Runtime.CompilerServices;
#endif
using System.Threading;

namespace PowerThreadPool.Helpers
{
    internal class Spinner
    {
#if DEBUG
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        internal static bool s_enableTimeoutException = true;
        internal static void Start(
        Func<bool> func,
        [CallerMemberName] string callerName = null,
        [CallerFilePath] string callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
#else
        internal static void Start(Func<bool> func)
#endif
#else
        internal static void Start(Func<bool> func)
#endif
        {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            SpinWait.SpinUntil(func);
#if DEBUG
            stopwatch.Stop();
            if (stopwatch.Elapsed.Ticks >= 8000)
            {
                double milliseconds = (double)stopwatch.Elapsed.Ticks / Stopwatch.Frequency * 1000;
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
                if (s_enableTimeoutException)
                {
                    throw new TimeoutException(
                       $"The operation took too long to complete: {stopwatch.Elapsed.Ticks} ticks. ({milliseconds:f3}ms)" +
                       $"\nCaller: {callerName}" +
                       $"\nFile: {callerFilePath}" +
                       $"\nLine: {callerLineNumber}");
                }
#else
                throw new TimeoutException($"The operation took too long to complete: {stopwatch.Elapsed.Ticks} ticks.");
#endif
            }
#endif
        }
    }
}
