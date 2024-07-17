using System.Collections.Generic;
using System.Threading.Tasks;
using PowerThreadPool.Results;

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

        public string Name => _groupName;

        /// <summary>
        /// Wait until all the work belonging to the group is done.
        /// </summary>
        public void Wait()
        {
            _powerPool.Wait(_powerPool.GetGroupMemberList(Name));
        }

        /// <summary>
        /// Wait until all the work belonging to the group is done.
        /// </summary>
        /// <returns></returns>
#if NET45_OR_GREATER
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
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<TResult>> Fetch<TResult>(bool removeAfterFetch = false)
        {
            return _powerPool.Fetch<TResult>(_powerPool.GetGroupMemberList(Name), removeAfterFetch);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
        public List<ExecuteResult<object>> Fetch(bool removeAfterFetch = false)
        {
            return _powerPool.Fetch<object>(_powerPool.GetGroupMemberList(Name), removeAfterFetch);
        }

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
#if NET45_OR_GREATER
        public async Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(bool removeAfterFetch = false)
        {
            return await Task.Run(() =>
            {
                return Fetch<TResult>(removeAfterFetch);
            });
        }
#else
        public Task<List<ExecuteResult<TResult>>> FetchAsync<TResult>(bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch<TResult>(removeAfterFetch);
            });
        }
#endif

        /// <summary>
        /// Fetch the work result.
        /// </summary>
        /// <param name="removeAfterFetch">remove the result from storage</param>
        /// <returns>Return a list of work result</returns>
#if NET45_OR_GREATER
        public async Task<List<ExecuteResult<object>>> FetchAsync(bool removeAfterFetch = false)
        {
            return await Task.Run(() =>
            {
                return Fetch(removeAfterFetch);
            });
        }
#else
        public Task<List<ExecuteResult<object>>> FetchAsync(bool removeAfterFetch = false)
        {
            return Task.Factory.StartNew(() =>
            {
                return Fetch(removeAfterFetch);
            });
        }
#endif

        /// <summary>
        /// Stop all the work belonging to the group.
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public List<string> Stop(bool forceStop = false)
        {
            return _powerPool.Stop(_powerPool.GetGroupMemberList(Name), forceStop);
        }

        /// <summary>
        /// Pause all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Pause()
        {
            return _powerPool.Pause(_powerPool.GetGroupMemberList(Name));
        }

        /// <summary>
        /// Resume all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Resume()
        {
            return _powerPool.Resume(_powerPool.GetGroupMemberList(Name));
        }

        /// <summary>
        /// Cancel all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Cancel()
        {
            return _powerPool.Cancel(_powerPool.GetGroupMemberList(Name));
        }
    }
}
