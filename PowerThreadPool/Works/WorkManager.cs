using System;
using System.Collections.Concurrent;

namespace PowerThreadPool.Works
{
    internal class WorkManager
    {
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<WorkBase>> _actionWorkPoolDict = new ConcurrentDictionary<Type, ConcurrentQueue<WorkBase>>();
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<WorkBase>> _funcWorkPoolDict = new ConcurrentDictionary<Type, ConcurrentQueue<WorkBase>>();

        internal WorkBase Get<T>(bool isFunc)
        {
            ConcurrentQueue<WorkBase> pool = (isFunc ? _funcWorkPoolDict : _actionWorkPoolDict).GetOrAdd(
                typeof(T),
                _ => new ConcurrentQueue<WorkBase>());
            WorkBase work = null;

            while (pool.TryDequeue(out work))
            {
                if (!work.Refresh())
                {
                    continue;
                }

                long currentTickCount = Environment.TickCount;

                while (pool.TryPeek(out WorkBase workPeek) && currentTickCount - workPeek.DeadTickCount > 60000)
                {
                    if (pool.TryDequeue(out workPeek))
                    {
                        // 这里peek到的和dequeue到的work可能不是同一个实例. 
                        // 但这是允许的, 照样Dispose. 
                        workPeek.Dispose();
                    }
                }

                break;
            }

            if (work == null)
            {
                work = isFunc ? (WorkBase)new WorkFunc<T>() : new WorkAction<T>();
            }

            work._canSetIntoPool.InterlockedValue = Constants.CanSetIntoPool.Allowed;

            return work;
        }

        internal void Set(WorkBase workBase)
        {
            if (!workBase._canSetIntoPool.TrySet(Constants.CanSetIntoPool.NotAllowed, Constants.CanSetIntoPool.Allowed))
            {
                return;
            }

            ConcurrentQueue<WorkBase> pool = (workBase.IsFunc ? _funcWorkPoolDict : _actionWorkPoolDict).GetOrAdd(
                workBase.ResultType,
                _ => new ConcurrentQueue<WorkBase>());

            workBase.DeadTickCount = Environment.TickCount;

            pool.Enqueue(workBase);
        }
    }
}
