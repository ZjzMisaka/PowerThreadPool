using System;
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
        internal static bool s_enableTimeoutLog = true;
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
            if (stopwatch.Elapsed.Ticks >= 5000)
            {
                double milliseconds = (double)stopwatch.Elapsed.Ticks / Stopwatch.Frequency * 1000;
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
                if (s_enableTimeoutLog)
                {
                    Console.WriteLine(
                       $"The operation took too long to complete: {stopwatch.Elapsed.Ticks} ticks. ({milliseconds:f3}ms)" +
                       $"\n\tCaller: {callerName}" +
                       $"\n\tFile: {callerFilePath}" +
                       $"\n\tLine: {callerLineNumber}");
                }
#else
                Console.WriteLine($"The operation took too long to complete: {stopwatch.Elapsed.Ticks} ticks.");
#endif
            }
#endif
        }
    }
}
