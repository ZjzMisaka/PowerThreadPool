﻿using System;
using System.Threading;
using PowerThreadPool.Collections;

namespace PowerThreadPool.Option
{

    public class WorkOption<TResult>
    {
        public WorkOption()
        {
        }

        /// <summary>
        /// The custom work ID. If set to null, the thread pool will use a Guid as the work ID.
        /// </summary>
        public string CustomWorkID { get; set; } = null;

        /// <summary>
        /// The maximum amount of time the work is allowed to run before it is terminated.
        /// </summary>
        public TimeoutOption Timeout { get; set; } = null;

        /// <summary>
        /// The callback function that is called when the work finishes execution.
        /// </summary>
        public Action<ExecuteResult<TResult>> Callback { get; set; } = null;

        /// <summary>
        /// The priority level of the work. Higher priority works are executed before lower priority works.
        /// </summary>
        public int WorkPriority { get; set; } = 0;

        /// <summary>
        /// Specifies the scheduling priority of a System.Threading.Thread.
        /// </summary>
        public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

        /// <summary>
        /// A set of works that this work depends on. This work will not start until all dependent works have completed execution.
        /// </summary>
        public ConcurrentSet<string> Dependents { get; set; } = null;

        /// <summary>
        /// Is long running work.
        /// </summary>
        public bool LongRunning { get; set; } = false;
    }

    public class WorkOption : WorkOption<object>
    {
        public WorkOption()
        {
        }
    }
}
