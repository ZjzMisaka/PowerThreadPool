using System;

namespace PowerThreadPool
{
    internal static class WorkerContext
    {
        [ThreadStatic]
        internal static Worker s_current;
    }
}
