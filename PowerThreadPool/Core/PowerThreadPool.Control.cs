﻿using System;
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

            // Directly retrieve the worker from the dictionary since the key is guaranteed to exist
            // If not, just let Work execute failed
            Worker worker = _aliveWorkerDic[Thread.CurrentThread.ManagedThreadId];
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
                if (_aliveWorkerDic.TryGetValue(Thread.CurrentThread.ManagedThreadId, out Worker worker))
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
            else
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
                    if (_workGroupDic.TryGetValue(work.Group, out ConcurrentSet<string> idSet))
                    {
                        idSet.Remove(work.ID);
                    }
                }
                throw new WorkStopException();
            }
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

            // Directly retrieve the worker from the dictionary since the key is guaranteed to exist
            // If not, just let Work execute failed
            Worker worker = _aliveWorkerDic[Thread.CurrentThread.ManagedThreadId];
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

            if (_aliveWorkerDic.TryGetValue(Thread.CurrentThread.ManagedThreadId, out Worker worker) && worker.WorkerState == WorkerStates.Running)
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
        public void Wait()
        {
            if (_poolState == PoolStates.NotRunning)
            {
                return;
            }
            _waitAllSignal.WaitOne();
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Return false if the work isn't running</returns>
        public bool Wait(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            WorkBase work;
            if (_suspendedWork.TryGetValue(id, out work) || _aliveWorkDic.TryGetValue(id, out work))
            {
                return work.Wait();
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
        /// <returns>Return a list of ID for work that doesn't running</returns>
        public List<string> Wait(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
            {
                if (!Wait(id))
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
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task WaitAsync()
        {
            await Task.Run(() =>
            {
                Wait();
            });
        }
#else
        public Task WaitAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                Wait();
            });
        }
#endif

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Return false if the work isn't running</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<bool> WaitAsync(string id)
        {
            return await Task.Run(() =>
            {
                return Wait(id);
            });
        }
#else
        public Task<bool> WaitAsync(string id)
        {
            return Task.Factory.StartNew(() =>
            {
                return Wait(id);
            });
        }
#endif

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<List<string>> WaitAsync(IEnumerable<string> idList)
        {
            return await Task.Run(() =>
            {
                List<string> failedIDList = new List<string>();

                foreach (string id in idList)
                {
                    if (!Wait(id))
                    {
                        failedIDList.Add(id);
                    }
                }

                return failedIDList;
            });
        }
#else
        public Task<List<string>> WaitAsync(IEnumerable<string> idList)
        {
            return Task.Factory.StartNew(() =>
            {
                List<string> failedIDList = new List<string>();

                foreach (string id in idList)
                {
                    if (!Wait(id))
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
        /// <returns>Work result</returns>
        public ExecuteResult<TResult> Fetch<TResult>(string id, bool removeAfterFetch = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            WorkBase work;
            ExecuteResultBase executeResultBase = null;
            if (_suspendedWork.TryGetValue(id, out work) || _aliveWorkDic.TryGetValue(id, out work) || (removeAfterFetch ? _resultDic.TryRemove(id, out executeResultBase) : _resultDic.TryGetValue(id, out executeResultBase)))
            {
                if (executeResultBase != null)
                {
                    return executeResultBase.ToTypedResult<TResult>();
                }
                else
                {
                    ExecuteResult<TResult> res = work.Fetch<TResult>();
                    if (removeAfterFetch)
                    {
                        RemoveAfterFetch(work);
                    }
                    return res;
                }
            }
            else
            {
                return new ExecuteResult<TResult>() { ID = id };
            }
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Work result</returns>
        public ExecuteResult<object> Fetch(string id, bool removeAfterFetch = false)
        {
            return Fetch<object>(id, removeAfterFetch);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(IEnumerable<string> idList, bool removeAfterFetch = false)
        {
            List<ExecuteResult<TResult>> resultList = new List<ExecuteResult<TResult>>();

            List<WorkBase> workList = new List<WorkBase>();

            foreach (string id in idList)
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
                    resultList.Add(new ExecuteResult<TResult>() { ID = id });
                }
            }

            foreach (WorkBase work in workList)
            {
                resultList.Add(work.Fetch<TResult>());

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
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<object>> Fetch(IEnumerable<string> idList, bool removeAfterFetch = false)
        {
            return Fetch<object>(idList, removeAfterFetch);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="predicate">a function to test each source element for a condition</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(Func<ExecuteResult<TResult>, bool> predicate, bool removeAfterFetch = false)
        {
            return Fetch<TResult>(predicate, _ => true, removeAfterFetch);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="predicate">a function to test each source element for a condition</param>
        /// <param name="predicateID">a function to test each source element for a condition</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
        internal List<ExecuteResult<TResult>> Fetch<TResult>(Func<ExecuteResult<TResult>, bool> predicate, Func<ExecuteResult<TResult>, bool> predicateID, bool removeAfterFetch = false)
        {
            List<string> idList = new List<string>();

            foreach (KeyValuePair<string, ExecuteResultBase> pair in _resultDic)
            {
                ExecuteResult<TResult> typedResult = pair.Value.ToTypedResult<TResult>();
                if (predicate(typedResult) && predicateID(typedResult))
                {
                    idList.Add(pair.Value.ID);
                }
            }

            return Fetch<TResult>(idList, removeAfterFetch);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<ExecuteResult<TResult>> FetchAsync<TResult>(string id, bool removeAfterFetch = false)
        {
            return await Task.Run(() =>
            {
                return Fetch<TResult>(id, removeAfterFetch);
            });
        }
#else
        public Task<ExecuteResult<TResult>> FetchAsync<TResult>(string id, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch<TResult>(id, removeAfterFetch);
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
        public async Task<ExecuteResult<object>> FetchAsync(string id, bool removeAfterFetch = false)
        {
            return await Task.Run(() =>
            {
                return Fetch(id, removeAfterFetch);
            });
        }
#else
        public Task<ExecuteResult<object>> FetchAsync(string id, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(id, removeAfterFetch);
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
        public async Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(IEnumerable<string> idList, bool removeAfterFetch = false)
        {
            return await Task.Run(() =>
            {
                return Fetch<TResult>(idList, removeAfterFetch);
            });
        }
#else
        public Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(IEnumerable<string> idList, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch<TResult>(idList, removeAfterFetch);
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
        public async Task<List<ExecuteResult<object>>> FetchAsync(IEnumerable<string> idList, bool removeAfterFetch = false)
        {
            return await Task.Run(() =>
            {
                return Fetch(idList, removeAfterFetch);
            });
        }
#else
        public Task<List<ExecuteResult<object>>> FetchAsync(IEnumerable<string> idList, bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(idList, removeAfterFetch);
            });
        }
#endif

        /// <summary>
        /// Stop all works
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public bool Stop(bool forceStop = false)
        {
            if (_poolState == PoolStates.NotRunning)
            {
                return false;
            }

            _poolStopping = true;

            if (forceStop)
            {
                _workGroupDic.Clear();
                foreach (Worker worker in _aliveWorkerDic.Values)
                {
                    if (worker.CanForceStop.TrySet(CanForceStop.NotAllowed, CanForceStop.Allowed))
                    {
                        worker.ForceStop(true);
                    }
                }
            }
            else
            {
                Cancel();
                _cancellationTokenSource.Cancel();
            }

            _workDependencyController.Cancel();

            return true;
        }

        /// <summary>
        /// Stop work by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if the work does not exist or has been done</returns>
        public bool Stop(string id, bool forceStop = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            bool res = false;
            if (_aliveWorkDic.TryGetValue(id, out WorkBase work))
            {
                res = work.Stop(forceStop);
            }

            if (_asyncWorkIDDict.TryGetValue(id, out ConcurrentSet<string> idSet))
            {
                foreach (string subID in idSet)
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
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        public List<string> Stop(IEnumerable<string> idList, bool forceStop = false)
        {
            List<string> failedIDList = new List<string>();

            idList = Cancel(idList);
            foreach (string id in idList)
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
        public bool Pause(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            if (_aliveWorkDic.TryGetValue(id, out WorkBase work))
            {
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
        public List<string> Pause(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
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
                foreach (Worker worker in _aliveWorkerDic.Values)
                {
                    if (worker.WorkerState == WorkerStates.Running)
                    {
                        worker.Resume();
                    }
                }
            }
        }

        /// <summary>
        /// Resume thread by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>If the work id exists</returns>
        public bool Resume(string id)
        {
            bool res = false;
            if (string.IsNullOrEmpty(id))
            {
                res = false;
            }
            else if (_aliveWorkDic.TryGetValue(id, out WorkBase work))
            {
                res = work.Resume();
            }
            return res;
        }

        /// <summary>
        /// Resume threads by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Resume(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
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
            foreach (Worker worker in _aliveWorkerDic.Values)
            {
                worker.Cancel();
            }

            _workDependencyController.Cancel();

            _stopSuspendedWork.Clear();
#if NET5_0_OR_GREATER
            _stopSuspendedWorkQueue.Clear();
#else
            _stopSuspendedWorkQueue = new ConcurrentQueue<string>();
#endif
        }

        /// <summary>
        /// Cancel the work by id if the work has not started running
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>is succeed</returns>
        public bool Cancel(string id)
        {
            if (string.IsNullOrEmpty(id))
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
                    Interlocked.Decrement(ref _aliveWorkerCount);
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
        public List<string> Cancel(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
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
            }
            else if (_aliveWorkDic.TryRemove(work.ID, out _))
            {
                if (work.Group != null)
                {
                    RemoveWorkFromGroup(work.Group, work);
                }
                work.Dispose();
            }

            _resultDic.TryRemove(work.ID, out _);

            CheckPoolIdle();
        }
    }
}
