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

        public void Wait()
        {
            powerPool.Wait(powerPool.GetGroupMemberList(groupName));
        }

        public async Task WaitAsync()
        {
            await Task.Run(() =>
            {
                Wait();
            });
        }

        public List<string> Stop(bool forceStop = false)
        {
            return powerPool.Stop(powerPool.GetGroupMemberList(groupName), forceStop);
        }

        public List<string> Pause()
        {
            return powerPool.Pause(powerPool.GetGroupMemberList(groupName));
        }

        public List<string> Resume()
        {
            return powerPool.Resume(powerPool.GetGroupMemberList(groupName));
        }

        public List<string> Cancel()
        {
            return powerPool.Cancel(powerPool.GetGroupMemberList(groupName));
        }
    }
}
