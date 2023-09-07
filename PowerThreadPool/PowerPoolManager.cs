using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    public static class PowerPoolManager
    {
        private static ConcurrentDictionary<string, PowerPool> instanceDic;

        public static int ManagedCount 
        {
            get { return instanceDic.Count; }
        }

        public static IEnumerable<string> ManagedList
        {
            get { return instanceDic.Keys.ToList(); }
        }

        /// <summary>
        /// Register pool
        /// </summary>
        /// <param name="powerPool"></param>
        /// <returns>Power pool id</returns>
        public static string RegisterPool(PowerPool powerPool)
        {
            string powerPoolId = Guid.NewGuid().ToString();
            if (instanceDic.TryAdd(powerPoolId, powerPool))
            {
                return powerPoolId;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Unregist pool
        /// </summary>
        /// <param name="id">Power pool id</param>
        /// <returns>Succeed or not</returns>
        public static bool UnregistPool(string id)
        {
            if (instanceDic.TryRemove(id, out _))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to pause when user call Pause(...)
        /// </summary>
        /// <param name="id">Power pool id</param>
        public static void PauseIfRequested(string id)
        {
            if (instanceDic.TryGetValue(id, out PowerPool powerPool))
            {
                powerPool.manualResetEvent.WaitOne();
                foreach (string workID in powerPool.manualResetEventDic.Keys)
                {
                    if (Thread.CurrentThread.Name == workID)
                    {
                        powerPool.manualResetEventDic[workID].WaitOne();
                    }
                }
            }
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to stop when user call ForceStop(...)
        /// </summary>
        /// <param name="id">Power pool id</param>
        public static void StopIfRequested(string id)
        {
            if (CheckIfRequestedStop(id))
            {
                throw new OperationCanceledException();
            }
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to pause when user call Pause(...)
        /// </summary>
        public static void PauseIfRequested()
        {
            foreach (string id in ManagedList)
            {
                if (instanceDic.TryGetValue(id, out PowerPool powerPool))
                {
                    powerPool.manualResetEvent.WaitOne();
                    foreach (string workID in powerPool.manualResetEventDic.Keys)
                    {
                        if (Thread.CurrentThread.Name == workID)
                        {
                            powerPool.manualResetEventDic[workID].WaitOne();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to stop when user call ForceStop(...)
        /// </summary>
        public static void StopIfRequested()
        {
            foreach (string id in ManagedList)
            {
                if (CheckIfRequestedStop(id))
                {
                    throw new OperationCanceledException();
                }
            }
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to check if requested stop (if user call ForceStop(...))
        /// </summary>
        /// <param name="id">Power pool id</param>
        /// <returns></returns>
        public static bool CheckIfRequestedStop(string id)
        {
            if (instanceDic.TryGetValue(id, out PowerPool powerPool))
            {
                if (powerPool.cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return true;
                }
                foreach (string workID in powerPool.cancellationTokenSourceDic.Keys)
                {
                    if (Thread.CurrentThread.Name == workID)
                    {
                        if (powerPool.cancellationTokenSourceDic[workID].Token.IsCancellationRequested)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }
    }
}
