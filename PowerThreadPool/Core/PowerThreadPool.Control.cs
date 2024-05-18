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

            foreach (Worker worker in _aliveWorkerList)
            {
                if (worker.WorkerState == WorkerStates.Running && worker._thread == Thread.CurrentThread && worker.IsPausing())
                {
                    worker.PauseTimer();
                    worker.WaitForResume();
                    worker.ResumeTimer();
                }
            }
        }

        /// <summary>
        /// Call this function inside the work logic where you want to stop when user call Stop(...)
        /// To exit the logic, the function will throw a PowerThreadPool.Exceptions.WorkStopException. Do not catch it. 
        /// If you do not want to exit the logic in this way (for example, if you have some unmanaged resources that need to be released before exiting), it is recommended to use CheckIfRequestedStop. 
        /// </summary>
        public void StopIfRequested()
        {
            WorkBase work = null;
            bool res = CheckIfRequestedStopAndGetWork(ref work);

            if (!res)
            {
                _settedWorkDic.Clear();
                _workGroupDic.Clear();
                throw new WorkStopException();
            }
            else if (work != null)
            {
                _settedWorkDic.TryRemove(work.ID, out _);
                if (work.Group != null)
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

            foreach (Worker worker in _aliveWorkerList)
            {
                if (worker.WorkerState == WorkerStates.Running && worker._thread == Thread.CurrentThread && worker.IsCancellationRequested())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Call this function inside the work logic where you want to check if requested stop (if user call Stop(...))
        /// </summary>
        /// <param name="work">The work executing now in current thread</param>
        /// <returns>Return false if stop all</returns>
        private bool CheckIfRequestedStopAndGetWork(ref WorkBase work)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                return false;
            }

            foreach (Worker worker in _aliveWorkerList)
            {
                if (worker.WorkerState == WorkerStates.Running && worker._thread == Thread.CurrentThread && worker.IsCancellationRequested())
                {
                    work = worker.Work;
                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        public void Wait()
        {
            if (_poolRunning == PoolRunningFlags.NotRunning)
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
            if (_suspendedWork.TryGetValue(id, out work) || _settedWorkDic.TryGetValue(id, out work))
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
        public async Task WaitAsync()
        {
            await Task.Run(() =>
            {
                Wait();
            });
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Return false if the work isn't running</returns>
        public async Task<bool> WaitAsync(string id)
        {
            return await Task.Run(() =>
            {
                return Wait(id);
            });
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
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

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Work result</returns>
        public ExecuteResult<TResult> Fetch<TResult>(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            WorkBase work;
            if (_suspendedWork.TryGetValue(id, out work) || _settedWorkDic.TryGetValue(id, out work))
            {
                return work.Fetch().ToTypedResult<TResult>();
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
        /// <returns>Work result</returns>
        public ExecuteResult<object> Fetch(string id)
        {
            return Fetch<object>(id);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(IEnumerable<string> idList)
        {
            List<ExecuteResult<TResult>> resultList = new List<ExecuteResult<TResult>>();

            List<WorkBase> workList = new List<WorkBase>();

            foreach (string id in idList)
            {
                WorkBase workBase;
                if (_suspendedWork.TryGetValue(id, out workBase) || _settedWorkDic.TryGetValue(id, out workBase))
                {
                    workList.Add(workBase);
                }
                else
                {
                    resultList.Add(new ExecuteResult<TResult>() { ID = id });
                }
            }

            foreach (WorkBase work in workList)
            {
                resultList.Add(work.Fetch().ToTypedResult<TResult>());
            }

            return resultList;
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<object>> Fetch(IEnumerable<string> idList)
        {
            return Fetch<object>(idList);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Work result</returns>
        public async Task<ExecuteResult<TResult>> FetchAsync<TResult>(string id)
        {
            return await Task.Run(() =>
            {
                return Fetch<TResult>(id);
            });
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Work result</returns>
        public async Task<ExecuteResult<object>> FetchAsync(string id)
        {
            return await Task.Run(() =>
            {
                return Fetch(id);
            });
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of work result</returns>
        public async Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(IEnumerable<string> idList)
        {
            return await Task.Run(() =>
            {
                return Fetch<TResult>(idList);
            });
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of work result</returns>
        public async Task<List<ExecuteResult<object>>> FetchAsync(IEnumerable<string> idList)
        {
            return await Task.Run(() =>
            {
                return Fetch(idList);
            });
        }

        /// <summary>
        /// Stop all works
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public bool Stop(bool forceStop = false)
        {
            if (_poolRunning == PoolRunningFlags.NotRunning)
            {
                return false;
            }

            _poolStopping = true;

            if (forceStop)
            {
                _settedWorkDic.Clear();
                _workGroupDic.Clear();
                IEnumerable<Worker> workersToStop = _aliveWorkerList;
                foreach (Worker worker in workersToStop)
                {
                    worker.ForceStop(true);
                }
            }
            else
            {
                _cancellationTokenSource.Cancel();
                IEnumerable<Worker> workersToStop = _aliveWorkerList;
                foreach (Worker worker in workersToStop)
                {
                    worker.Cancel();
                }
            }

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
            if (_settedWorkDic.TryGetValue(id, out WorkBase work))
            {
                res = work.Stop(forceStop);
            }

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
            if (_poolTimer != null)
            {
                _poolTimer.Stop();
            }
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
            if (_settedWorkDic.TryGetValue(id, out WorkBase work))
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
            if (_poolTimer != null)
            {
                _poolTimer.Start();
            }
            _pauseSignal.Set();
            if (resumeWorkPausedByID)
            {
                foreach (Worker worker in _aliveWorkerList)
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
            else if (_settedWorkDic.TryGetValue(id, out WorkBase work))
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
        /// Cancel all tasks that have not started running
        /// </summary>
        public void Cancel()
        {
            foreach (Worker worker in _aliveWorkerList)
            {
                worker.Cancel();
            }
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

            if (_suspendedWork.TryRemove(id, out _))
            {
                return true;
            }
            else if (_settedWorkDic.TryGetValue(id, out WorkBase work))
            {
                return work.Cancel(true);
            }
            else
            {
                return false;
            }
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
    }
}
