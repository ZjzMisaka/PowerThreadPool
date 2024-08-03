using System;

namespace PowerThreadPool.Helpers
{
    internal class ConditionalExecutor
    {
        private readonly Action _actionVersionChanged;
        private volatile bool _updated;

        internal ConditionalExecutor(Action funcVersionChanged)
        {
            _actionVersionChanged = funcVersionChanged;
        }

        internal void Update()
        {
            _updated = true;
        }

        internal void Run()
        {
            if (_updated)
            {
                _updated = false;
                _actionVersionChanged();
            }
        }
    }
}
