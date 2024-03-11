using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Constants
{
    internal class WorkerGettedFlags
    {
        internal const int Unlocked = 0;
        internal const int Locked = 1;
        internal const int ToBeDisabled = 2;
        internal const int Disabled = -1;
    }
}
