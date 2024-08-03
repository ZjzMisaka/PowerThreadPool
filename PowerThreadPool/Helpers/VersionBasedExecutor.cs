using System;
using System.Threading;

namespace PowerThreadPool.Helpers
{
    internal class VersionBasedExecutor
    {
        private Action _actionVersionChanged;

        private long _updatedVersion = long.MinValue;
        private long _executeVersion = long.MinValue;

        internal VersionBasedExecutor(Action funcVersionChanged)
        {
            _actionVersionChanged = funcVersionChanged;
        }

        internal void UpdateVersion()
        {
            Interlocked.CompareExchange(ref _updatedVersion, long.MinValue, long.MaxValue);
            Interlocked.Increment(ref _updatedVersion);
        }

        internal void Run()
        {
            if (Interlocked.Exchange(ref _executeVersion, _updatedVersion) != _updatedVersion)
            {
                _actionVersionChanged();
            }
        }
    }
}
