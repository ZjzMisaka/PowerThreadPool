using System;
using System.ComponentModel;

namespace PowerThreadPool.Options
{
    public enum RejectType
    {
        AbortPolicy,
        CallerRunsPolicy,
        DiscardPolicy,
        DiscardQueuedPolicy,
        [ObsoleteAttribute("Use DiscardQueuedPolicy instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        DiscardOldestPolicy,
    }

    public class RejectOption
    {
        /// <summary>
        /// Thread queue limit.
        /// If the queue is full, the reject policy will be used.
        /// </summary>
        public int ThreadQueueLimit { get; set; }

        /// <summary>
        /// Reject type.
        /// </summary>
        public RejectType RejectType { get; set; }
    }
}
