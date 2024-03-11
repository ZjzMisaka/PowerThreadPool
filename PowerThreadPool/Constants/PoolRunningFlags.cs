using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Constants
{
    internal class PoolRunningFlags
    {
        internal const int NotRunning = 0;
        internal const int IdleChecked = 1;
        internal const int Running = 2;
    }
}
