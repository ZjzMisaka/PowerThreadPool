using System;
#if DEBUG
using System.Diagnostics;
#endif
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
using System.Runtime.CompilerServices;
#endif
using System.Threading;

namespace PowerThreadPool.Helpers.LockFree
{
    // Spinner usage guidelines:
    // Before using spinning, you must ensure in tests that the spin duration in Ticks is less than 5000.
    // Otherwise, spinning should not be used and other optimization methods should be considered.
    // Unit tests that use mocks to force the Spinner to spin for a long time are exceptions.
    // If using the Spinner causes significant overhead, use the Spinner only in DEBUG mode for verification,
    // and manually spin in Release mode.
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
            if (func())
            {
                return;
            }
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
