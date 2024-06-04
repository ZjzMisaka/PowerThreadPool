using System.Threading;

namespace PowerThreadPool.Helpers
{
    internal class IDGenerator
    {
        private static long s_counter = 0;
        private static readonly long s_maxCounterValue = long.MaxValue - 1000;

        public static string NextId()
        {
            long nextId = Interlocked.Increment(ref s_counter);

            if (nextId >= s_maxCounterValue)
            {
                Interlocked.CompareExchange(ref s_counter, 0, nextId);
                nextId = Interlocked.Increment(ref s_counter);
            }

            return nextId.ToString();
        }
    }
}
