using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Constants
{
    internal class WorkerStates
    {
        internal const int Idle = 0;
        internal const int Running = 1;
        internal const int ToBeDisposed = 2;
    }
}
