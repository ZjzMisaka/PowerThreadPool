using System;
using System.Collections.Generic;
using PowerThreadPool.Collections;
using PowerThreadPool.Groups;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public partial class PowerPool
    {
        /// <summary>
        /// Get group object
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>Group object</returns>
        public Group GetGroup(string groupName)
        {
            return new Group(this, groupName);
        }

        /// <summary>
        /// Get all members of a group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>Work id collection</returns>
        public IEnumerable<string> GetGroupMemberList(string groupName)
        {
            List<string> groupList = new List<string>() { groupName };
            GetChildGroupList(groupName, groupList);

            ConcurrentSet<string> memberSet = new ConcurrentSet<string>();
            foreach (string group in groupList)
            {
                if (_workGroupDic.TryGetValue(group, out ConcurrentSet<string> groupMemberList))
                {
                    foreach (string member in groupMemberList)
                    {
                        memberSet.Add(member);
                    }
                }
            }

            return memberSet;
        }

        /// <summary>
        /// Add work to group.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="workID"></param>
        /// <returns>
        /// Returns false if the work does not exist.
        /// Modifies WorkOption.Group.
        /// </returns>
        public bool AddWorkToGroup(string groupName, string workID)
        {
            if (_aliveWorkDic.TryGetValue(workID, out WorkBase work))
            {
                work.RenewWorkOptionIfDefault();
                work.Group = groupName;
                _workGroupDic.AddOrUpdate(groupName, new ConcurrentSet<string> { workID }, (key, oldValue) => { oldValue.Add(workID); return oldValue; });
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove work from group.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="workID"></param>
        /// <returns>Returns false if either the work or the group does not exist, or if the work does not belong to the group.</returns>
        public bool RemoveWorkFromGroup(string groupName, string workID)
        {
            if (_aliveWorkDic.TryGetValue(workID, out WorkBase work))
            {
                return RemoveWorkFromGroup(groupName, work);
            }

            return false;
        }

        /// <summary>
        /// Remove work from group.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="work"></param>
        /// <returns>Returns false if either the work or the group does not exist, or if the work does not belong to the group.</returns>
        private bool RemoveWorkFromGroup(string groupName, WorkBase work)
        {
            if (_workGroupDic.TryGetValue(groupName, out ConcurrentSet<string> workIDSet))
            {
                work.RenewWorkOptionIfDefault();
                work.Group = null;
                return workIDSet.Remove(work.ID);
            }

            return false;
        }

        /// <summary>
        /// Set group relation
        /// </summary>
        /// <param name="parentGroup">parent group</param>
        /// <param name="childGroup">child group</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetGroupRelation(string parentGroup, string childGroup)
        {
            List<string> groupList = new List<string>();
            GetChildGroupList(childGroup, groupList);
            if (groupList.Contains(parentGroup))
            {
                throw new InvalidOperationException($"Cannot create a cyclic group relation: '{parentGroup}' is already a subgroup of '{childGroup}'.");
            }
            _groupRelationDic.AddOrUpdate(parentGroup, new ConcurrentSet<string>() { childGroup }, (key, oldValue) => { oldValue.Add(childGroup); return oldValue; });
        }

        /// <summary>
        /// Remove group relation
        /// </summary>
        /// <param name="parentGroup">parent group</param>
        /// <param name="childGroup">child group</param>
        /// <returns>is succeed</returns>
        public bool RemoveGroupRelation(string parentGroup, string childGroup = null)
        {
            bool res = false;
            if (_groupRelationDic.TryGetValue(parentGroup, out ConcurrentSet<string> childGroupSet))
            {
                if (childGroup != null)
                {
                    res = childGroupSet.Remove(childGroup);
                }
                else
                {
                    childGroupSet.Clear();
                    res = true;
                }
            }
            return res;
        }

        /// <summary>
        /// Reset group relation
        /// </summary>
        public void ResetGroupRelation()
        {
            _groupRelationDic.Clear();
        }

        /// <summary>
        /// Get child group list
        /// </summary>
        /// <param name="groupName"></param>
        private void GetChildGroupList(string groupName, List<string> groupList)
        {
            if (_groupRelationDic.TryGetValue(groupName, out ConcurrentSet<string> childGroupSet))
            {
                foreach (string childGroupName in childGroupSet)
                {
                    groupList.Add(childGroupName);
                    GetChildGroupList(childGroupName, groupList);
                }
            }
        }
    }
}
