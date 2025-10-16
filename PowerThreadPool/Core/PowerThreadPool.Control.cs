using System;
#if NET5_0_OR_GREATER
#else
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public partial class PowerPool
    {
        /// <summary>
        /// Call this function inside the work logic where you want to pause when user call Pause(...)
        /// </summary>
        public void PauseIfRequested()
        {
            _pauseSignal.WaitOne();

            // Directly get current thread worker since it is guaranteed to exist
            // If not, just let Work execute failed
            if (!GetCurrentThreadWorker(out Worker worker))
            {
                throw new InvalidOperationException("PauseIfRequested must be called on a PowerPool worker thread.");
            }
            WorkBase pauseWork = null;
            if (worker.Work.IsPausing)
            {
                pauseWork = worker.Work;
            }
            else if (worker.Work.BaseAsyncWorkID != null && _aliveWorkDic.TryGetValue(worker.Work.BaseAsyncWorkID, out WorkBase work) && work.IsPausing)
            {
                pauseWork = work;
            }

            if (pauseWork != null)
            {
                worker.PauseTimer();
                pauseWork.PauseSignal.WaitOne();
                worker.ResumeTimer();
            }
        }

        /// <summary>
        /// Call this function inside the work logic where you want to stop when user call Stop(...)
        /// To exit the logic, the function will throw a PowerThreadPool.Exceptions.WorkStopException. Do not catch it. 
        /// If you do not want to exit the logic in this way (for example, if you have some unmanaged resources that need to be released before exiting), it is recommended to use CheckIfRequestedStop. 
        /// </summary>
        /// <param name="beforeStop">
        /// An optional function that is executed before the stop process.
        /// Return false to prevent stopping.
        /// </param>
        public void StopIfRequested(Func<bool> beforeStop = null)
        {
            WorkBase work = null;

            if (!CheckIfRequestedStopAndGetWork(ref work))
            {
                return;
            }

            if (work == null)
            {
                StopAllIfRequested(beforeStop);
            }
            else
            {
                StopByIDIfRequested(work, beforeStop);
            }
        }

        private void StopAllIfRequested(Func<bool> beforeStop = null)
        {
            if (GetCurrentThreadWorker(out Worker worker))
            {
                worker.Work.AllowEventsAndCallback = true;
            }
            if (beforeStop != null && !beforeStop())
            {
                return;
            }
            _workGroupDic.Clear();
            _asyncWorkIDDict.Clear();
            _asyncWorkCount = 0;
            throw new WorkStopException();
        }

        private void StopByIDIfRequested(WorkBase work, Func<bool> beforeStop = null)
        {
            if (beforeStop != null && !beforeStop())
            {
                return;
            }
            // If the result needs to be stored, there is a possibility of fetching the result through Group.
            // Therefore, Work should not be removed from _aliveWorkDic and _workGroupDic for the time being
            if (work.Group == null || !work.ShouldStoreResult)
            {
                if (work.BaseAsyncWorkID == null)
                {
                    _aliveWorkDic.TryRemove(work.ID, out _);
                    work.Dispose();
                }
            }
            if (work.Group != null && !work.ShouldStoreResult)
            {
                if (_workGroupDic.TryGetValue(work.Group, out ConcurrentSet<WorkID> idSet))
                {
                    idSet.Remove(work.ID);
                }
            }
            throw new WorkStopException();
        }

        /// <summary>
        /// Call this function inside the work logic where you want to stop when user call Stop(...)
        /// To exit the logic, the function will throw a PowerThreadPool.Exceptions.WorkStopException. Do not catch it. 
        /// If you do not want to exit the logic in this way (for example, if you have some unmanaged resources that need to be released before exiting), it is recommended to use CheckIfRequestedStop. 
        /// </summary>
        /// <param name="beforeStop">
        /// An optional function that is executed before the stop process.
        /// </param>
        public void StopIfRequested(Action beforeStop) =>
            StopIfRequested(() =>
            {
                beforeStop();
                return true;
            });

        /// <summary>
        /// Call this function inside the work logic where you want to check if requested stop (if user call Stop(...))
        /// When returning true, you can perform some pre operations (such as releasing unmanaged resources) and then safely exit the logic.
        /// </summary>
        /// <returns>Requested stop or not</returns>
        public bool CheckIfRequestedStop()
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                return true;
            }

            // Directly get current thread worker since it is guaranteed to exist
            // If not, just let Work execute failed
            if (!GetCurrentThreadWorker(out Worker worker))
            {
                throw new InvalidOperationException("CheckIfRequestedStop must be called on a PowerPool worker thread.");
            }
            if (worker.IsCancellationRequested())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Call this function inside the work logic where you want to check if requested stop (if user call Stop(...))
        /// </summary>
        /// <param name="work">The work executing now in current thread</param>
        /// <returns>
        /// Return true if stop.
        /// If work is null, it means stop all, otherwise it means stopping based on work id.
        /// </returns>
        private bool CheckIfRequestedStopAndGetWork(ref WorkBase work)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                return true;
            }

            if (GetCurrentThreadWorker(out Worker worker) && worker.WorkerState == WorkerStates.Running)
            {
                if (worker.IsCancellationRequested())
                {
                    work = worker.Work;
                    return true;
                }
                else if (worker.Work.BaseAsyncWorkID != null && _aliveWorkDic.TryGetValue(worker.Work.BaseAsyncWorkID, out WorkBase baseAsyncWork) && baseAsyncWork.ShouldStop)
                {
                    work = worker.Work;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        public void Wait(bool helpWhileWaiting = false)
        {
            Wait((CancellationToken)default, helpWhileWaiting);
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        public void Wait(CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            if (_poolState == PoolStates.NotRunning)
            {
                return;
            }

            if (helpWhileWaiting)
            {
                HelpWhileWaitingUntilPoolIdle(cancellationToken);
                return;
            }
            else
            {
                if (cancellationToken == default)
                {
                    _waitAllSignal.WaitOne();
                }
                else
                {
                    int idx = WaitHandle.WaitAny(new WaitHandle[] { _waitAllSignal, cancellationToken.WaitHandle });
                    if (idx == 1)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        private void HelpWhileWaitingUntilPoolIdle(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                if (!HelpWhileWaiting())
                {
                    if (RunningWorkerCount == 0 &&
                        WaitingWorkCount == 0 &&
                        AsyncWorkCount == 0)
                    {
                        return;
                    }
                    Thread.Yield();
                }
            }
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return false if the work isn't running</returns>
        public bool Wait(WorkID id, bool helpWhileWaiting = false)
        {
            return Wait(id, default, helpWhileWaiting);
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return false if the work isn't running</returns>
        public bool Wait(WorkID id, CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            if (id == null)
            {
                return false;
            }

            WorkBase work;
            if (TryGetSuspendOrAliveWork(id, out work))
            {
                return work.Wait(cancellationToken, helpWhileWaiting);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
        public List<WorkID> Wait(IEnumerable<WorkID> idList, bool helpWhileWaiting = false)
        {
            return Wait(idList, default, helpWhileWaiting);
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
        public List<WorkID> Wait(IEnumerable<WorkID> idList, CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            List<WorkID> failedIDList = new List<WorkID>();

            foreach (WorkID id in idList)
            {
                if (!Wait(id, cancellationToken, helpWhileWaiting))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        /// <returns>A Task</returns>
        public Task WaitAsync()
        {
            return WaitAsync((CancellationToken)default);
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <returns>A Task</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            Task task;
            if (CheckSignalAlreadySetWhenAsyncWait(null, out task))
            {
                return task;
            }

            TaskCompletionSource<object> tcs = NewTcs<object>();
            RegisteredWaitHandle rwh = null;
            WaitOrTimerCallback cb = (state, timedOut) =>
            {
                SetTcsResult(tcs);
            };
            rwh = ThreadPool.RegisterWaitForSingleObject(_waitAllSignal, cb, null, Timeout.Infinite, true);

            _waitRegDict[tcs.Task] = rwh;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
#if (NET46_OR_GREATER || NET5_0_OR_GREATER)
                    if (tcs.TrySetCanceled(cancellationToken))
                    {
                        SetTcsResult(tcs);
                    }
#else
                    if (tcs.TrySetCanceled())
                    {
                        SetTcsResult(tcs);
                    }
#endif
                });
            }

            if (CheckSignalAlreadySetWhenAsyncWait(tcs, out task))
            {
                return task;
            }

            return tcs.Task;
        }
#else
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                Wait(cancellationToken);
            });
        }
#endif

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        private bool CheckSignalAlreadySetWhenAsyncWait(TaskCompletionSource<object> tcs, out Task task)
        {
            bool res = false;
            task = default;

            if (_waitAllSignal.WaitOne(0))
            {
                res = true;

                SetTcsResult(tcs);

#if (NET46_OR_GREATER || NET5_0_OR_GREATER)
                task = Task.CompletedTask;
#else
                task = Task.FromResult(0);
#endif
            }

            return res;
        }

        private void SetTcsResult(TaskCompletionSource<object> tcs)
        {
            if (tcs != null)
            {
                tcs.TrySetResult(null);
                if (_waitRegDict.TryRemove(tcs.Task, out RegisteredWaitHandle h))
                {
                    h.Unregister(null);
                }
            }
        }
#endif

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Return false if the work isn't running</returns>
        public Task<bool> WaitAsync(WorkID id)
        {
            return WaitAsync(id, default);
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <returns>Return false if the work isn't running</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public Task<bool> WaitAsync(WorkID id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return Task.FromResult(false);
            }

            WorkBase work;
            if (TryGetSuspendOrAliveWork(id, out work))
            {
                return work.WaitAsync(cancellationToken);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
#else
        public Task<bool> WaitAsync(WorkID id, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                return Wait(id, cancellationToken, false);
            });
        }
#endif

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<List<WorkID>> WaitAsync(IEnumerable<WorkID> idList)
        {
            return await WaitAsync(idList, default);
        }
#else
        public Task<List<WorkID>> WaitAsync(IEnumerable<WorkID> idList)
        {
            return WaitAsync(idList, default);
        }
#endif

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<List<WorkID>> WaitAsync(IEnumerable<WorkID> idList, CancellationToken cancellationToken)
        {
            List<WorkID> failedIDList = new List<WorkID>();

            foreach (WorkID id in idList)
            {
                if (!await WaitAsync(id, cancellationToken))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }
#else
        public Task<List<WorkID>> WaitAsync(IEnumerable<WorkID> idList, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                List<WorkID> failedIDList = new List<WorkID>();

                foreach (WorkID id in idList)
                {
                    if (!Wait(id, cancellationToken, false))
                    {
                        failedIDList.Add(id);
                    }
                }

                return failedIDList;
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>.
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Work result</returns>
        public ExecuteResult<TResult> Fetch<TResult>(WorkID id, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Fetch<TResult>(id, default, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>.
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Work result</returns>
        public ExecuteResult<TResult> Fetch<TResult>(WorkID id, CancellationToken cancellationToken, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            if (id == null)
            {
                return null;
            }

            WorkBase work;
            ExecuteResultBase executeResultBase = null;
            if (_suspendedWork.TryGetValue(id, out work) || _aliveWorkDic.TryGetValue(id, out work) || _workDependencyController._workDict.TryGetValue(id, out work) || (removeAfterFetch ? _resultDic.TryRemove(id, out executeResultBase) : _resultDic.TryGetValue(id, out executeResultBase)))
            {
                if (executeResultBase != null)
                {
                    return executeResultBase.ToTypedResult<TResult>();
                }
                else
                {
                    ExecuteResult<TResult> res = work.Fetch<TResult>(cancellationToken, helpWhileWaiting);
                    if (removeAfterFetch)
                    {
                        RemoveAfterFetch(work);
                    }
                    return res;
                }
            }
            else
            {
                return new ExecuteResult<TResult>()
                {
                    ID = id,
                    IsFound = false,
                };
            }
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Work result</returns>
        public ExecuteResult<object> Fetch(WorkID id, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Fetch<object>(id, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Work result</returns>
        public ExecuteResult<object> Fetch(WorkID id, CancellationToken cancellationToken, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Fetch<object>(id, cancellationToken, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(IEnumerable<WorkID> idList, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Fetch<TResult>(idList, default, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(IEnumerable<WorkID> idList, CancellationToken cancellationToken, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            List<ExecuteResult<TResult>> resultList = new List<ExecuteResult<TResult>>();

            List<WorkBase> workList = new List<WorkBase>();

            foreach (WorkID id in idList)
            {
                GetFetchWorkByIDList(resultList, workList, id, removeAfterFetch);
            }

            foreach (WorkBase work in workList)
            {
                resultList.Add(work.Fetch<TResult>(cancellationToken, helpWhileWaiting));

                if (removeAfterFetch)
                {
                    RemoveAfterFetch(work);
                }
            }

            return resultList;
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<object>> Fetch(IEnumerable<WorkID> idList, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Fetch<object>(idList, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<object>> Fetch(IEnumerable<WorkID> idList, CancellationToken cancellationToken, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Fetch<object>(idList, cancellationToken, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="predicate">a function to test each source element for a condition</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(Func<ExecuteResult<TResult>, bool> predicate, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Fetch<TResult>(predicate, _ => true, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="predicate">a function to test each source element for a condition</param>
        /// <param name="predicateID">a function to test each source element for a condition</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        internal List<ExecuteResult<TResult>> Fetch<TResult>(Func<ExecuteResult<TResult>, bool> predicate, Func<ExecuteResult<TResult>, bool> predicateID, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            List<WorkID> idList = new List<WorkID>();

            foreach (KeyValuePair<WorkID, ExecuteResultBase> pair in _resultDic)
            {
                ExecuteResult<TResult> typedResult = pair.Value.ToTypedResult<TResult>();
                if (predicate(typedResult) && predicateID(typedResult))
                {
                    idList.Add(pair.Value.ID);
                }
            }

            return Fetch<TResult>(idList, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<ExecuteResult<TResult>> FetchAsync<TResult>(WorkID id, bool removeAfterFetch = false)
        {
            return await FetchAsync<TResult>(id, default, removeAfterFetch);
        }
#else
        public Task<ExecuteResult<TResult>> FetchAsync<TResult>(WorkID id, bool removeAfterFetch = false)
        {
            return FetchAsync<TResult>(id, default, removeAfterFetch);
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<ExecuteResult<TResult>> FetchAsync<TResult>(WorkID id, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            if (id == null)
            {
                return null;
            }

            WorkBase work;
            ExecuteResultBase executeResultBase = null;
            if (_suspendedWork.TryGetValue(id, out work) || _aliveWorkDic.TryGetValue(id, out work) || _workDependencyController._workDict.TryGetValue(id, out work) || (removeAfterFetch ? _resultDic.TryRemove(id, out executeResultBase) : _resultDic.TryGetValue(id, out executeResultBase)))
            {
                if (executeResultBase != null)
                {
                    return executeResultBase.ToTypedResult<TResult>();
                }
                else
                {
                    ExecuteResult<TResult> res = await work.FetchAsync<TResult>(cancellationToken);
                    if (removeAfterFetch)
                    {
                        RemoveAfterFetch(work);
                    }
                    return res;
                }
            }
            else
            {
                return new ExecuteResult<TResult>()
                {
                    ID = id,
                    IsFound = false,
                };
            }
        }
#else
        public Task<ExecuteResult<TResult>> FetchAsync<TResult>(WorkID id, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch<TResult>(id, cancellationToken, removeAfterFetch, false);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public Task<ExecuteResult<object>> FetchAsync(WorkID id, bool removeAfterFetch = false)
        {
            return FetchAsync<object>(id, removeAfterFetch);
        }
#else
        public Task<ExecuteResult<object>> FetchAsync(WorkID id, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(id, removeAfterFetch, false);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public Task<ExecuteResult<object>> FetchAsync(WorkID id, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            return FetchAsync<object>(id, cancellationToken, removeAfterFetch);
        }
#else
        public Task<ExecuteResult<object>> FetchAsync(WorkID id, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(id, cancellationToken, removeAfterFetch, false);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(IEnumerable<WorkID> idList, bool removeAfterFetch = false)
        {
            return await FetchAsync<TResult>(idList, default, removeAfterFetch);
        }
#else
        public Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(IEnumerable<WorkID> idList, bool removeAfterFetch = false)
        {
            return FetchAsync<TResult>(idList, default, removeAfterFetch);
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(IEnumerable<WorkID> idList, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            List<ExecuteResult<TResult>> resultList = new List<ExecuteResult<TResult>>();

            List<WorkBase> workList = new List<WorkBase>();

            foreach (WorkID id in idList)
            {
                GetFetchWorkByIDList(resultList, workList, id, removeAfterFetch);
            }

            foreach (WorkBase work in workList)
            {
                resultList.Add(await work.FetchAsync<TResult>(cancellationToken));

                if (removeAfterFetch)
                {
                    RemoveAfterFetch(work);
                }
            }

            return resultList;
        }
#else
        public Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(IEnumerable<WorkID> idList, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch<TResult>(idList, cancellationToken, removeAfterFetch, false);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public Task<List<ExecuteResult<object>>> FetchAsync(IEnumerable<WorkID> idList, bool removeAfterFetch = false)
        {
            return FetchAsync<object>(idList, removeAfterFetch);
        }
#else
        public Task<List<ExecuteResult<object>>> FetchAsync(IEnumerable<WorkID> idList, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(idList, removeAfterFetch, false);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel this operation.</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public Task<List<ExecuteResult<object>>> FetchAsync(IEnumerable<WorkID> idList, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            return FetchAsync<object>(idList, cancellationToken, removeAfterFetch);
        }
#else
        public Task<List<ExecuteResult<object>>> FetchAsync(IEnumerable<WorkID> idList, CancellationToken cancellationToken, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(idList, cancellationToken, removeAfterFetch, false);
            });
        }
#endif

        private void GetFetchWorkByIDList<TResult>(List<ExecuteResult<TResult>> resultList, List<WorkBase> workList, WorkID id, bool removeAfterFetch)
        {
            WorkBase workBase;
            ExecuteResultBase executeResultBase = null;
            if (_suspendedWork.TryGetValue(id, out workBase) || _aliveWorkDic.TryGetValue(id, out workBase) || (removeAfterFetch ? _resultDic.TryRemove(id, out executeResultBase) : _resultDic.TryGetValue(id, out executeResultBase)))
            {
                if (executeResultBase != null)
                {
                    resultList.Add(executeResultBase.ToTypedResult<TResult>());
                }
                else
                {
                    workList.Add(workBase);
                }
            }
            else
            {
                resultList.Add(new ExecuteResult<TResult>()
                {
                    ID = id,
                    IsFound = false,
                });
            }
        }

        /// <summary>
        /// Stop all works
        /// </summary>
        /// <returns>Return false if no thread running</returns>
        public bool Stop()
        {
            return Stop(false);
        }

        /// <summary>
        /// Call Thread.Interrupt() and force stop all works
        /// Although this approach is safer than Thread.Abort, from the perspective of the business logic,
        /// it can still potentially lead to unpredictable results and cannot guarantee the time consumption of exiting the thread,
        /// therefore you should avoid using force stop as much as possible.
        /// </summary>
        /// <returns>Return false if no thread running</returns>
        public bool ForceStop()
        {
            return Stop(true);
        }

        /// <summary>
        /// Stop all works
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        internal bool Stop(bool forceStop)
        {
            if (_poolState == PoolStates.NotRunning)
            {
                return false;
            }

            bool res = true;

            _poolStopping = true;

            if (forceStop)
            {
                _workGroupDic.Clear();
                Cancel();
                foreach (Worker worker in _aliveWorkerDic.Values)
                {
                    if (worker.CanForceStop.TrySet(CanForceStop.NotAllowed, CanForceStop.Allowed))
                    {
                        worker.ForceStop();
                    }
                    else
                    {
                        res = false;
                    }
                }
            }
            else
            {
                Cancel();
                _cancellationTokenSource.Cancel();
            }

            _workDependencyController.Cancel();

            return res;
        }

        /// <summary>
        /// Stop work by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if the work does not exist or has been done</returns>
        public bool Stop(WorkID id)
        {
            return Stop(id, false);
        }

        /// <summary>
        /// Call Thread.Interrupt() and force stop work by id
        /// Although this approach is safer than Thread.Abort, from the perspective of the business logic,
        /// it can still potentially lead to unpredictable results and cannot guarantee the time consumption of exiting the thread,
        /// therefore you should avoid using force stop as much as possible.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Return false if the work does not exist or has been done</returns>
        public bool ForceStop(WorkID id)
        {
            return Stop(id, true);
        }

        /// <summary>
        /// Stop work by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if the work does not exist or has been done</returns>
        internal bool Stop(WorkID id, bool forceStop)
        {
            if (id == null)
            {
                return false;
            }

            bool res = false;
            if (_aliveWorkDic.TryGetValue(id, out WorkBase work))
            {
                res = work.Stop(forceStop);
            }

            if (_asyncWorkIDDict.TryGetValue(id, out ConcurrentSet<WorkID> idSet))
            {
                foreach (WorkID subID in idSet)
                {
                    res = Stop(subID, forceStop);
                }
            }

            _workDependencyController.Cancel(id);

            return res;
        }

        /// <summary>
        /// Stop works by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        public List<WorkID> Stop(IEnumerable<WorkID> idList)
        {
            return Stop(idList, false);
        }

        /// <summary>
        /// Call Thread.Interrupt() and force stop works by id list
        /// Although this approach is safer than Thread.Abort, from the perspective of the business logic,
        /// it can still potentially lead to unpredictable results and cannot guarantee the time consumption of exiting the thread,
        /// therefore you should avoid using force stop as much as possible.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        public List<WorkID> ForceStop(IEnumerable<WorkID> idList)
        {
            return Stop(idList, true);
        }

        /// <summary>
        /// Stop works by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        internal List<WorkID> Stop(IEnumerable<WorkID> idList, bool forceStop)
        {
            List<WorkID> failedIDList = new List<WorkID>();

            idList = Cancel(idList);
            foreach (WorkID id in idList)
            {
                if (!Stop(id, forceStop))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        /// <summary>
        /// Pause all threads
        /// </summary>
        public void Pause()
        {
            _timeoutTimer.Pause();
            _pauseSignal.Reset();
        }

        /// <summary>
        /// Pause thread by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>If the work id exists</returns>
        public bool Pause(WorkID id)
        {
            if (id == null)
            {
                return false;
            }
            if (_aliveWorkDic.TryGetValue(id, out WorkBase work))
            {
                _pausingWorkSet.Add(work);
                return work.Pause();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Pause threads by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<WorkID> Pause(IEnumerable<WorkID> idList)
        {
            List<WorkID> failedIDList = new List<WorkID>();

            foreach (WorkID id in idList)
            {
                if (!Pause(id))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        /// <summary>
        /// Resume all threads
        /// </summary>
        /// <param name="resumeWorkPausedByID">if resume work paused by ID</param>
        public void Resume(bool resumeWorkPausedByID = false)
        {
            _timeoutTimer.Resume();
            _pauseSignal.Set();

            if (resumeWorkPausedByID)
            {
                foreach (WorkBase work in _pausingWorkSet)
                {
                    work.Resume();
                }
                _pausingWorkSet.Clear();
            }
        }

        /// <summary>
        /// Resume thread by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>If the work id exists</returns>
        public bool Resume(WorkID id)
        {
            bool res = false;
            if (id == null)
            {
                res = false;
            }
            else if (_aliveWorkDic.TryGetValue(id, out WorkBase work))
            {
                _pausingWorkSet.Remove(work);
                res = work.Resume();
            }
            return res;
        }

        /// <summary>
        /// Resume threads by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<WorkID> Resume(IEnumerable<WorkID> idList)
        {
            List<WorkID> failedIDList = new List<WorkID>();

            foreach (WorkID id in idList)
            {
                if (!Resume(id))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        /// <summary>
        /// Cancel all works that have not started running
        /// </summary>
        public void Cancel()
        {
            foreach (WorkBase work in _aliveWorkDic.Values)
            {
                work.Cancel(true);
            }

            _workDependencyController.Cancel();

            _stopSuspendedWork.Clear();
#if NET5_0_OR_GREATER
            _stopSuspendedWorkQueue.Clear();
#else
            _stopSuspendedWorkQueue = new ConcurrentQueue<WorkID>();
#endif
        }

        /// <summary>
        /// Cancel the work by id if the work has not started running
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>is succeed</returns>
        public bool Cancel(WorkID id)
        {
            if (id == null)
            {
                return false;
            }

            bool res = false;

            if (_workDependencyController.Cancel(id))
            {
                res = true;
            }
            else if (_suspendedWork.TryRemove(id, out _))
            {
                Interlocked.Decrement(ref _waitingWorkCount);
                res = true;
            }
            else if (_stopSuspendedWork.TryRemove(id, out _))
            {
                res = true;
            }
            else if (_aliveWorkDic.TryGetValue(id, out WorkBase work))
            {
                res = work.Cancel(true);
                if (res && _aliveWorkDic.TryRemove(id, out _))
                {
                    work.Dispose();
                }
            }
            else
            {
                res = false;
            }

            if (res)
            {
                TryRemoveAsyncWork(id, false);
            }

            return res;
        }

        /// <summary>
        /// Cancel the works by id if the work has not started running
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<WorkID> Cancel(IEnumerable<WorkID> idList)
        {
            List<WorkID> failedIDList = new List<WorkID>();

            foreach (WorkID id in idList)
            {
                if (!Cancel(id))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        private void RemoveAfterFetch(WorkBase work)
        {
            if (work.BaseAsyncWorkID != null)
            {
                TryRemoveAsyncWork(work.ID, true);
                _resultDic.TryRemove(work.AsyncWorkID, out _);
            }
            else
            {
                if (_aliveWorkDic.TryRemove(work.ID, out _))
                {
                    if (work.Group != null)
                    {
                        RemoveWorkFromGroup(work.Group, work);
                    }
                    work.Dispose();
                }

                _resultDic.TryRemove(work.ID, out _);
            }

            CheckPoolIdle();
        }

        internal bool HelpWhileWaiting()
        {
            List<WorkBase> works = null;
            if (GetCurrentThreadBaseWorker(out Worker workerCurrentThread))
            {
                if (workerCurrentThread.WaitingWorkCount >= 1
                    && workerCurrentThread.WorkStealability.TrySet(WorkStealability.NotAllowed, WorkStealability.Allowed))
                {
                    works = workerCurrentThread.Steal(1);
                    workerCurrentThread.WorkStealability.InterlockedValue = WorkStealability.Allowed;
                }
            }

            if (works == null || works.Count == 0)
            {
                foreach (Worker worker in _aliveWorkerDic.Values)
                {
                    if (worker.WaitingWorkCount >= 1
                        && worker.WorkStealability.TrySet(WorkStealability.NotAllowed, WorkStealability.Allowed))
                    {
                        works = worker.Steal(1);
                        worker.WorkStealability.InterlockedValue = WorkStealability.Allowed;
                        if (works != null && works.Count > 0)
                            break;
                    }
                }
            }

            if (works != null && works.Count > 0)
            {
                WorkBase work = works[0];

                Interlocked.Increment(ref _runningWorkerCount);
                InvokeRunningWorkerCountChangedEvent(true);
                Worker newWorker = null;
                if (!_helperWorkerQueue.TryDequeue(out newWorker))
                {
                    newWorker = new Worker();
                }
                newWorker.RunHelp(this, work);
                _helperWorkerQueue.Enqueue(newWorker);
                Interlocked.Decrement(ref _runningWorkerCount);
                InvokeRunningWorkerCountChangedEvent(false);

                CheckPoolIdle();

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryGetSuspendOrAliveWork(WorkID id, out WorkBase work)
        {
            return _suspendedWork.TryGetValue(id, out work) || _aliveWorkDic.TryGetValue(id, out work);
        }
    }
}
