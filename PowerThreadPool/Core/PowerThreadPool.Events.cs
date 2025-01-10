﻿using System;
using System.Reflection.Metadata;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public partial class PowerPool
    {
        public event EventHandler<EventArgs> PoolStarted;
        public event EventHandler<PoolIdledEventArgs> PoolIdled;
        public event EventHandler<RunningWorkerCountChangedEventArgs> RunningWorkerCountChanged;
        public event EventHandler<WorkStartedEventArgs> WorkStarted;
        public event EventHandler<WorkEndedEventArgs> WorkEnded;
        public event EventHandler<EventArgs> PoolTimedOut;
        public event EventHandler<WorkTimedOutEventArgs> WorkTimedOut;
        public event EventHandler<WorkStoppedEventArgs> WorkStopped;
        public event EventHandler<WorkCanceledEventArgs> WorkCanceled;
        public event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;

        internal delegate void CallbackEndEventHandler(string id);
        internal event CallbackEndEventHandler CallbackEnd;

        /// <summary>
        /// Invoke work end event
        /// </summary>
        /// <param name="executeResult"></param>
        internal void InvokeWorkEndedEvent(ExecuteResultBase executeResult, object[] parameter)
        {
            executeResult.EndDateTime = DateTime.UtcNow;
            Interlocked.Increment(ref _endCount);
            Interlocked.Add(ref _executeTime, (long)(executeResult.EndDateTime - executeResult.StartDateTime).TotalMilliseconds);
            if (WorkEnded != null)
            {
                WorkEndedEventArgs e = new WorkEndedEventArgs()
                {
                    ID = executeResult.ID,
                    Parameter = parameter,
                    Exception = executeResult.Exception,
                    Result = executeResult.GetResult(),
                    Succeed = executeResult.Status == Status.Succeed,
                    QueueDateTime = executeResult.QueueDateTime,
                    StartDateTime = executeResult.StartDateTime,
                    EndDateTime = executeResult.EndDateTime,
                    RetryInfo = executeResult.RetryInfo,
                };

                if (executeResult.RetryInfo != null)
                {
                    executeResult.RetryInfo.StopRetry = e.RetryInfo.StopRetry;
                }

                SafeInvoke(WorkEnded, e, ErrorFrom.WorkEnded, executeResult, parameter);
            }
        }

        /// <summary>
        /// Invoke work stopped event
        /// </summary>
        /// <param name="executeResult"></param>
        internal void InvokeWorkStoppedEvent(ExecuteResultBase executeResult, object[] parameter)
        {
            executeResult.EndDateTime = DateTime.UtcNow;
            Interlocked.Increment(ref _endCount);
            Interlocked.Add(ref _executeTime, (long)(executeResult.EndDateTime - executeResult.StartDateTime).TotalMilliseconds);
            if (WorkStopped != null)
            {
                WorkStoppedEventArgs e = new WorkStoppedEventArgs()
                {
                    ID = executeResult.ID,
                    Parameter = parameter,
                    ForceStop = executeResult.Status == Status.ForceStopped,
                    QueueDateTime = executeResult.QueueDateTime,
                    StartDateTime = executeResult.StartDateTime,
                    EndDateTime = executeResult.EndDateTime,
                };
                SafeInvoke(WorkStopped, e, ErrorFrom.WorkStopped, executeResult, parameter);
            }
        }

        /// <summary>
        /// Invoke running worker count changed event
        /// </summary>
        /// <param name="isIncrement"></param>
        internal void InvokeRunningWorkerCountChangedEvent(bool isIncrement)
        {
            if (RunningWorkerCountChanged != null)
            {
                int runningWorkerCountTemp = _runningWorkerCount;
                int prevRunningWorkerCount;
                if (isIncrement)
                {
                    prevRunningWorkerCount = runningWorkerCountTemp - 1;
                }
                else
                {
                    prevRunningWorkerCount = runningWorkerCountTemp + 1;
                }
                RunningWorkerCountChangedEventArgs runningWorkerCountChangedEventArgs = new RunningWorkerCountChangedEventArgs
                {
                    NowCount = runningWorkerCountTemp,
                    PreviousCount = prevRunningWorkerCount,
                };
                SafeInvoke(RunningWorkerCountChanged, runningWorkerCountChangedEventArgs, ErrorFrom.RunningWorkerCountChanged, null, null);
            }
        }

        /// <summary>
        /// Invoke work canceled event
        /// </summary>
        /// <param name="executeResult"></param>
        internal void InvokeWorkCanceledEvent(ExecuteResultBase executeResult, object[] parameter)
        {
            executeResult.EndDateTime = DateTime.UtcNow;
            Interlocked.Increment(ref _endCount);
            Interlocked.Add(ref _executeTime, (long)(executeResult.EndDateTime - executeResult.StartDateTime).TotalMilliseconds);
            if (WorkCanceled != null)
            {
                WorkCanceledEventArgs e = new WorkCanceledEventArgs()
                {
                    ID = executeResult.ID,
                    Parameter = parameter,
                    QueueDateTime = executeResult.QueueDateTime,
                    StartDateTime = executeResult.StartDateTime,
                    EndDateTime = executeResult.EndDateTime,
                };
                SafeInvoke(WorkCanceled, e, ErrorFrom.WorkCanceled, executeResult, parameter);
            }
        }

        /// <summary>
        /// Work end
        /// </summary>
        /// <param name="work"></param>
        /// <param name="status"></param>
        internal void WorkCallbackEnd(WorkBase work, Status status)
        {
            if (status == Status.Failed)
            {
                _failedWorkSet.Add(work.ID);
            }
            else if (status == Status.Canceled)
            {
                _canceledWorkSet.Add(work.ID);
            }

            if (CallbackEnd != null)
            {
                CallbackEnd.Invoke(work.ID);
            }

            // If the result needs to be stored, there is a possibility of fetching the result through Group.
            // Therefore, Work should not be removed from _aliveWorkDic and _workGroupDic for the time being
            if (work.Group == null || !work.ShouldStoreResult)
            {
                _aliveWorkDic.TryRemove(work.ID, out _);
                work.Dispose();
            }
            if (work.Group != null && !work.ShouldStoreResult)
            {
                if (_workGroupDic.TryGetValue(work.Group, out ConcurrentSet<string> idSet))
                {
                    idSet.Remove(work.ID);
                }
            }
        }

        /// <summary>
        /// Invoke WorkTimedOut event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnWorkTimedOut(object sender, WorkTimedOutEventArgs e, object[] parameter)
        {
            if (WorkTimedOut != null)
            {
                SafeInvoke(WorkTimedOut, e, ErrorFrom.WorkTimedOut, null, parameter);
            }
        }

        /// <summary>
        /// Invoke WorkStarted event
        /// </summary>
        /// <param name="workID"></param>
        internal void OnWorkStarted(string workID, object[] parameter)
        {
            if (WorkStarted != null)
            {
                SafeInvoke(WorkStarted, new WorkStartedEventArgs()
                {
                    ID = workID,
                    Parameter = parameter
                }, ErrorFrom.WorkStarted, null, parameter);
            }
        }

        /// <summary>
        /// Safe invoke
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="eventHandler"></param>
        /// <param name="e"></param>
        /// <param name="errorFrom"></param>
        /// <param name="executeResult"></param>
        internal void SafeInvoke<TEventArgs>(EventHandler<TEventArgs> eventHandler, TEventArgs e, ErrorFrom errorFrom, ExecuteResultBase executeResult, object[] parameter)
            where TEventArgs : EventArgs
        {
            try
            {
                eventHandler.Invoke(this, e);
            }
            catch (ThreadInterruptedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ErrorOccurred != null)
                {
                    ErrorOccurredEventArgs ea = new ErrorOccurredEventArgs(ex, errorFrom, executeResult, parameter);

                    ErrorOccurred.Invoke(this, ea);
                }
            }
        }

        /// <summary>
        /// On work error occurred
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="errorFrom"></param>
        /// <param name="executeResult"></param>
        internal void OnWorkErrorOccurred(Exception exception, ErrorFrom errorFrom, ExecuteResultBase executeResult, object[] parameter)
        {
            if (ErrorOccurred != null)
            {
                ErrorOccurredEventArgs e = new ErrorOccurredEventArgs(exception, errorFrom, executeResult, parameter);

                ErrorOccurred.Invoke(this, e);
            }
        }

        /// <summary>
        /// Safe callback
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callback"></param>
        /// <param name="errorFrom"></param>
        /// <param name="executeResult"></param>
        internal void SafeCallback<TResult>(Action<ExecuteResult<TResult>> callback, ErrorFrom errorFrom, ExecuteResultBase executeResult, object[] parameter)
        {
            try
            {
                callback(executeResult.ToTypedResult<TResult>());
            }
            catch (ThreadInterruptedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ErrorOccurred != null)
                {
                    ErrorOccurredEventArgs e = new ErrorOccurredEventArgs(ex, errorFrom, executeResult, parameter);

                    ErrorOccurred.Invoke(this, e);
                }
            }
        }
    }
}
