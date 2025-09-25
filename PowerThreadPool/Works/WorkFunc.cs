﻿using System;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkFunc<TResult> : Work<TResult>
    {
        private Func<TResult> _function;

        internal WorkFunc(PowerPool powerPool, WorkID id, Func<TResult> function, WorkOption<TResult> option) : base(powerPool, id, option)
        {
            _function = function;
        }

        internal override object Execute()
        {
            ++_executeCount;
            return _function();
        }
    }
}
