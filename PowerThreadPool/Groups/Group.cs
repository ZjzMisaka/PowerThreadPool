using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Groups
{
    public class Group
    {
        internal string groupName;
        internal PowerPool powerPool;
        internal Group(PowerPool powerPool, string groupName)
        {
            this.powerPool = powerPool;
            this.groupName = groupName;
        }

        /// <summary>
        /// Wait until all the work belonging to the group is done.
        /// </summary>
        public void Wait()
        {
            powerPool.Wait(powerPool.GetGroupMemberList(groupName));
        }

        /// <summary>
        /// Wait until all the work belonging to the group is done.
        /// </summary>
        /// <returns></returns>
        public async Task WaitAsync()
        {
            await Task.Run(() =>
            {
                Wait();
            });
        }

        /// <summary>
        /// Stop all the work belonging to the group.
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public List<string> Stop(bool forceStop = false)
        {
            return powerPool.Stop(powerPool.GetGroupMemberList(groupName), forceStop);
        }

        /// <summary>
        /// Pause all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Pause()
        {
            return powerPool.Pause(powerPool.GetGroupMemberList(groupName));
        }

        /// <summary>
        /// Resume all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Resume()
        {
            return powerPool.Resume(powerPool.GetGroupMemberList(groupName));
        }

        /// <summary>
        /// Cancel all the work belonging to the group.
        /// </summary>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Cancel()
        {
            return powerPool.Cancel(powerPool.GetGroupMemberList(groupName));
        }
    }
}
