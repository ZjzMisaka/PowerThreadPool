// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using PowerThreadPool.Constants;
using System.Threading;

namespace PowerThreadPool.Works
{
    internal class WorkerLocker : IDisposable
    {
        private readonly WorkBase _work;
        private Worker _worker = null;
        private readonly bool _isHoldWork;
        private bool _disposed;

        public WorkerLocker(WorkBase work,
                            bool isHoldWork = true,
                            bool isLock = true)
        {

            _work = work;
            _isHoldWork = isHoldWork;

            if (isLock)
                Lock();

        }

        private void Lock()
        {

            do
            {
                Unlock();

                SpinWait.SpinUntil(() => (_worker = _work.Worker) != null);

                SpinWait.SpinUntil(() => _worker.StealingLock.TrySet(WorkerStealingFlags.Locked, WorkerStealingFlags.Unlocked));

                if (_isHoldWork)
                {
                    SpinWait.SpinUntil(() => _worker.WorkHeld.TrySet(WorkHeldFlags.Held, WorkHeldFlags.NotHeld));
                }
            }
            while (_work.Worker == null || (_work.Worker != null && _work.Worker.ID != _worker.ID));

        }

        private void Unlock()
        {
            if (_worker != null)
            {
                _worker.StealingLock.InterlockedValue = WorkerStealingFlags.Unlocked;

                if (_isHoldWork)
                {
                    _worker.WorkHeld.InterlockedValue = WorkHeldFlags.NotHeld;
                }
            }
        }



        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    Unlock();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~WorkLocker()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(true);
            GC.SuppressFinalize(this);
        }


    }
}
