using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool.Groups
{
    public class Group
    {
        internal string _groupName;
        internal PowerPool _powerPool;
        internal Group(PowerPool powerPool, string groupName)
        {
            _powerPool = powerPool;
            _groupName = groupName;
        }

        /// <summary>
        /// Group name
        /// </summary>
        public string Name => _groupName;

        /// <summary>
        /// Add work to group.
        /// </summary>
        /// <param name="workID"></param>
        /// <returns>
        /// Returns false if the work does not exist.
        /// Modifies WorkOption.Group.
        /// </returns>
        public bool Add(WorkID workID)
        {
            return _powerPool.AddWorkToGroup(Name, workID);
        }

        /// <summary>
        /// Remove work from group.
        /// </summary>
        /// <param name="workID"></param>
        /// <returns>Returns false if work does not exist, or if the work does not belong to the group.</returns>
        public bool Remove(WorkID workID)
        {
            return _powerPool.RemoveWorkFromGroup(Name, workID);
        }

        /// <summary>
        /// Wait until all the work belonging to the group is done.
        /// </summary>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        public void Wait(bool helpWhileWaiting = false)
        {
            _powerPool.Wait(_powerPool.GetGroupMemberList(Name), helpWhileWaiting);
        }

        /// <summary>
        /// Wait until all the work belonging to the group is done.
        /// </summary>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns></returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task WaitAsync(bool helpWhileWaiting = false)
        {
            await Task.Run(() =>
            {
                Wait(helpWhileWaiting);
            });
        }
#else
        public Task WaitAsync(bool helpWhileWaiting = false)
        {
            return Task.Factory.StartNew(() =>
            {
                Wait(helpWhileWaiting);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return _powerPool.Fetch<TResult>(_powerPool.GetGroupMemberList(Name), removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<object>> Fetch(bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return _powerPool.Fetch<object>(_powerPool.GetGroupMemberList(Name), removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="predicate">a function to test each source element for a condition; the second parameter of the function represents the index of the source element</param>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(Func<ExecuteResult<TResult>, bool> predicate, bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            ConcurrentSet<WorkID> idList = (ConcurrentSet<WorkID>)_powerPool.GetGroupMemberList(Name);
            Func<ExecuteResult<TResult>, bool> predicateID = e => idList.Contains(e.ID);
            return _powerPool.Fetch(predicate, predicateID, removeAfterFetch, helpWhileWaiting);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return await Task.Run(() =>
            {
                return Fetch<TResult>(removeAfterFetch, helpWhileWaiting);
            });
        }
#else
        public Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch<TResult>(removeAfterFetch, helpWhileWaiting);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <param name="helpWhileWaiting">When a caller is blocked waiting, they can "help" the pool progress by executing available work.</param>
        /// <returns>Return a list of work result</returns>
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        public async Task<List<ExecuteResult<object>>> FetchAsync(bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return await Task.Run(() =>
            {
                return Fetch(removeAfterFetch, helpWhileWaiting);
            });
        }
#else
        public Task<List<ExecuteResult<object>>> FetchAsync(bool removeAfterFetch = false, bool helpWhileWaiting = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(removeAfterFetch, helpWhileWaiting);
            });
        }
#endif

        /// <summary>
        /// Stop all the work belonging to the group.
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public List<WorkID> Stop(bool forceStop = false)
        {
            return _powerPool.Stop(_powerPool.GetGroupMemberList(Name), forceStop);
        }

        /// <summary>
        /// Pause all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<WorkID> Pause()
        {
            return _powerPool.Pause(_powerPool.GetGroupMemberList(Name));
        }

        /// <summary>
        /// Resume all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<WorkID> Resume()
        {
            return _powerPool.Resume(_powerPool.GetGroupMemberList(Name));
        }

        /// <summary>
        /// Cancel all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<WorkID> Cancel()
        {
            return _powerPool.Cancel(_powerPool.GetGroupMemberList(Name));
        }
    }
}
