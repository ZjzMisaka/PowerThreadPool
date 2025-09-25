﻿using System;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkAction<TUseless> : Work<TUseless>
    {
        private Action _action;

        internal WorkAction(PowerPool powerPool, WorkID id, Action action, WorkOption<TUseless> option) : base(powerPool, id, option)
        {
            _action = action;
        }

        internal override object Execute()
        {
            ++_executeCount;
            _action();
            return null;
        }
    }
}
