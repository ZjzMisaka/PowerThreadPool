using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Works
{
    internal sealed class WorkHandle
    {
        internal WorkID ID { get; }
        internal ManualResetEvent WaitSignal { get; set; }
        internal volatile bool _isDone;
        internal bool IsDone
        {
            get => _isDone;
            set => _isDone = value;
        }
        internal InterlockedFlag<CanCancel> _canCancel = CanCancel.Allowed;
        internal WorkBase Shell { get; set; }
    }
}
