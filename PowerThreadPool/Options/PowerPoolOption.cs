using System;
using System.Collections.Generic;
using PowerThreadPool.Collections;
using PowerThreadPool.Results;

namespace PowerThreadPool.Options
{
    public enum QueueType
    {
        FIFO,
        LIFO,
    }

    public enum WorkIDType
    {
        LongIncrement,
        Guid,
    }

    public class PowerPoolOption
    {
        internal List<PowerPool> PowerPoolList { get; set; } = new List<PowerPool>();

        private int _maxThreads = Environment.ProcessorCount * 2;
        /// <summary>
        /// The maximum number of threads that the thread pool can support.
        /// </summary>
        public int MaxThreads
        {
            get => _maxThreads;
            set
            {
                DestroyThreadOption destroyThreadOption = DestroyThreadOption;

                if (destroyThreadOption != null)
                {
                    destroyThreadOption.CheckThreadCount(destroyThreadOption.MinThreads, value);
                }
                _maxThreads = value;

                WorkLoopMaxStep = GetWorkStealingLoopMaxStep(_maxThreads);

                OnThreadCountSettingChanged();
            }
        }

        private int _workLoopMaxStep = GetWorkStealingLoopMaxStep(Environment.ProcessorCount * 2);
        internal int WorkLoopMaxStep
        {
            get => _workLoopMaxStep;
            set => _workLoopMaxStep = value;
        }

        private DestroyThreadOption _destroyThreadOption;
        /// <summary>
        /// The option for destroying threads in the thread pool.
        /// </summary>
        public DestroyThreadOption DestroyThreadOption
        {
            get => _destroyThreadOption;
            set
            {
                if (value != null)
                {
                    value.PowerPoolOption = this;
                }
                _destroyThreadOption = value;
            }
        }

        /// <summary>
        /// The total maximum amount of time that all works in the thread pool are permitted to run collectively before they are terminated.
        /// </summary>
        public TimeoutOption TimeoutOption { get; set; } = null;

        /// <summary>
        /// The default maximum amount of time a work in the pool is allowed to run before it is terminated.
        /// </summary>
        public TimeoutOption DefaultWorkTimeoutOption { get; set; } = null;

        /// <summary>
        /// After setting, it will be triggered regularly when the pool is in the running state. 
        /// </summary>
        public RunningTimerOption RunningTimerOption { get; set; } = null;

        /// <summary>
        /// The default callback function that is called when a work finishes execution.
        /// </summary>
        public Action<ExecuteResult<object>> DefaultCallback { get; set; } = null;

        /// <summary>
        /// Indicates whether the pool should begin in a suspended state.
        /// </summary>
        public bool StartSuspended { get; set; } = false;

        /// <summary>
        /// FIFO or LIFO.
        /// </summary>
        public QueueType QueueType { get; set; } = QueueType.FIFO;

        /// <summary>
        /// Determines whether to clear the result storage when the pool starts.
        /// </summary>
        public bool ClearResultStorageWhenPoolStart { get; set; } = true;

        /// <summary>
        /// Determines whether to clear the records of failed work when the pool starts.
        /// </summary>
        public bool ClearFailedWorkRecordWhenPoolStart { get; set; } = true;

        /// <summary>
        /// A factory function that creates instances of 
        /// <see cref="IStealablePriorityCollection{T}"/> of type <see cref="string"/>.
        /// </summary>
        public Func<IStealablePriorityCollection<string>> CustomQueueFactory { get; set; }

        /// <summary>
        /// The type of work ID to be used.
        /// </summary>
        public WorkIDType WorkIDType { get; set; } = WorkIDType.LongIncrement;

        /// <summary>
        /// Reject policy.
        /// </summary>
        public RejectOption RejectOption { get; set; } = null;

        internal void OnThreadCountSettingChanged()
        {
            foreach (PowerPool powerPool in PowerPoolList)
            {
                if (!powerPool._disposed && !powerPool._disposing)
                {
                    powerPool.FillWorkerQueue();
                }
            }
        }

        private static int GetWorkStealingLoopMaxStep(int maxThreads)
        {
            return (int)Math.Min(maxThreads, Math.Log(maxThreads + 1) * 3);
        }
    }
}
