using System.Collections.Generic;

namespace PowerThreadPool.Collections.Comparer
{
    internal class DescendingIntComparer : IComparer<int>
    {
        internal static DescendingIntComparer Instance { get; } = new DescendingIntComparer();
        private DescendingIntComparer() { }

        public int Compare(int x, int y)
        {
            return y.CompareTo(x);
        }
    }
}
