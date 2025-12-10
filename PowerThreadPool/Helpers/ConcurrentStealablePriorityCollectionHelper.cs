using System.Collections.Generic;

namespace PowerThreadPool.Helpers
{
    internal class ConcurrentStealablePriorityCollectionHelper
    {
        internal static List<int> InsertPriorityDescending(List<int> oldList, int priority)
        {
            var newList = new List<int>(oldList.Count + 1);
            bool inserted = false;
            for (int i = 0; i < oldList.Count; ++i)
            {
                int p = oldList[i];
                if (!inserted && priority > p)
                {
                    newList.Add(priority);
                    inserted = true;
                }
                newList.Add(p);
            }
            if (!inserted)
            {
                newList.Add(priority);
            }
            return newList;
        }
    }
}
