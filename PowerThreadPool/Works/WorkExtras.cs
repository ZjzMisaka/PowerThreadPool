using System;
using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Helpers.Timers;

namespace PowerThreadPool.Works
{
    /// <summary>
    /// Cold-state holder for WorkBase: members that only exist when the corresponding
    /// optional feature is used (Wait/Fetch signals, Pause, timeout timer, cancellation
    /// token source, statistics timestamps and the async-only completion flag).
    /// A Work that uses none of those features never allocates this object.
    ///
    /// Must be a class with public fields (not properties) so call sites can publish
    /// members atomically via Interlocked.CompareExchange(ref ...).
    /// The instance itself is published through WorkBase.Extras with a CAS full barrier,
    /// so no further synchronization is needed to read its fields after publication.
    /// </summary>
    internal sealed class WorkExtras
    {
        // Internal fields follow the repo's _camelCase naming rule. They must remain
        // fields (not properties) so call sites can publish them atomically via
        // Interlocked.CompareExchange(ref ...).
        internal ManualResetEventSlim _waitSignal;
        internal ManualResetEvent _pauseSignal;
        internal AsyncManualResetEvent _pauseAsyncSignal;
        internal DeferredActionTimer _timeoutTimer;
        internal CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Queue datetime (UTC). Only written when statistics collection is enabled.
        /// </summary>
        internal DateTime _queueDateTime;
        /// <summary>
        /// Start datetime (UTC). Only written when statistics collection is enabled.
        /// </summary>
        internal DateTime _startDateTime;
        internal long _duration;

        internal InterlockedFlag<CanSetTaskCompletionSource> _canSetTaskCompletionSource = CanSetTaskCompletionSource.Allowed;
    }
}
