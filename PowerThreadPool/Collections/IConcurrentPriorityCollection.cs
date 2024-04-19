using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Collections
{
    internal interface IConcurrentPriorityCollection<T>
    {
        void Set(T item, int priority);
        T Get();
    }
}
