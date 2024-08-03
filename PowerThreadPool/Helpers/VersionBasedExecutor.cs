using System;
using System.Threading;

namespace PowerThreadPool.Helpers
{
    internal class VersionBasedExecutor
    {
        private readonly Action _actionVersionChanged;

        private long _updatedVersion = long.MinValue;
        private long _executeVersion = long.MinValue;

        internal VersionBasedExecutor(Action funcVersionChanged)
        {
            _actionVersionChanged = funcVersionChanged;
        }

        internal void UpdateVersion()
        {
            if (_updatedVersion == long.MaxValue)
            {
                Interlocked.CompareExchange(ref _updatedVersion, long.MinValue, long.MaxValue);
            }
            Interlocked.Increment(ref _updatedVersion);
        }

        internal void Run()
        {
            if (_executeVersion != _updatedVersion && Interlocked.Exchange(ref _executeVersion, _updatedVersion) != _updatedVersion)
            {
                _actionVersionChanged();
            }
        }
    }
}
