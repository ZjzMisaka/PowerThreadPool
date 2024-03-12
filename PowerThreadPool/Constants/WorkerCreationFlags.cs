using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Constants
{
    internal class WorkerStealingFlags
    {
        internal const int Unlocked = 0;
        internal const int Locked = 1;
    }
}
