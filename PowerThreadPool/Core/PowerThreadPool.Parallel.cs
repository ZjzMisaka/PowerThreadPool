using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Constants;
using PowerThreadPool.Groups;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;

namespace PowerThreadPool
{
    public partial class PowerPool
    {
        /// <summary>
        /// Creates a parallel loop that executes iterations from start to end.
        /// </summary>
        /// <param name="start">The start index of the loop.</param>
        /// <param name="end">The end index of the loop.</param>
        /// <param name="body">The action to execute for each loop iteration.</param>
        /// <param name="step">The step value for each loop iteration. Default is 1.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a  group object.</returns>
        public Group For(int start, int end, Action<int> body, int step = 1, string groupName = null)
        {
            return For<object>(start, end, null, (_, index) => { body(index); }, step, groupName);
        }

        /// <summary>
        /// Creates a parallel loop that executes iterations from start to end.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
        /// <param name="start">The start index of the loop.</param>
        /// <param name="end">The end index of the loop.</param>
        /// <param name="source">The source collection of elements to be processed in the loop.</param>
        /// <param name="body">The action to execute for each loop iteration, receiving an element from the source collection.</param>
        /// <param name="step">The step value for each loop iteration. Default is 1.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a  group object.</returns>
        public Group For<TSource>(int start, int end, IList<TSource> source, Action<TSource> body, int step = 1, string groupName = null)
        {
            return For(start, end, source, (item, _) => { body(item); }, step, groupName);
        }

        /// <summary>
        /// Creates a parallel loop that executes iterations from start to end.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
        /// <param name="start">The start index of the loop.</param>
        /// <param name="end">The end index of the loop.</param>
        /// <param name="source">The source collection of elements to be processed in the loop.</param>
        /// <param name="body">The action to execute for each loop iteration, receiving an element from the source collection and the iteration index.</param>
        /// <param name="step">The step value for each loop iteration. Default is 1.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a  group object.</returns>
        /// <exception cref="ArgumentException">Thrown when the step is zero or the loop configuration is invalid.</exception>
        public Group For<TSource>(int start, int end, IList<TSource> source, Action<TSource, int> body, int step = 1, string groupName = null)
        {
            if (start > end && step == 1)
            {
                step = -1;
            }

            if (step == 0)
            {
                throw new ArgumentException("Step cannot be zero.", nameof(step));
            }
            if ((start > end && step > 0) || (start < end && step < 0))
            {
                throw new ArgumentException("Invalid start, end, and step combination. The loop will never terminate.", nameof(step));
            }

            string groupID = null;
            if (string.IsNullOrEmpty(groupName))
            {
                groupID = Guid.NewGuid().ToString();
            }
            else
            {
                groupID = groupName;
            }
            WorkOption workOption = new WorkOption()
            {
                Group = groupID,
            };
            for (int i = start; start <= end ? i < end : i > end; i += step)
            {
                int localI = i;
                QueueWorkItem(() => { body(source != null ? source[localI] : default, localI); }, workOption);
            }
            return GetGroup(groupID);
        }

        /// <summary>
        /// Creates a parallel loop that executes a specified action for each element in the source collection.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
        /// <param name="source">The source collection of elements to be processed.</param>
        /// <param name="body">The action to execute for each element in the source collection.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a  group object.</returns>
        public Group ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body, string groupName = null)
        {
            return ForEach(source, (item, _) => body(item), groupName);
        }

        /// <summary>
        /// Creates a parallel loop that executes a specified action for each element in the source collection.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
        /// <param name="source">The source collection of elements to be processed.</param>
        /// <param name="body">The action to execute for each element in the source collection and its index.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a  group object.</returns>
        public Group ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, int> body, string groupName = null)
        {
            string groupID = null;
            if (string.IsNullOrEmpty(groupName))
            {
                groupID = Guid.NewGuid().ToString();
            }
            else
            {
                groupID = groupName;
            }

            WorkOption workOption = new WorkOption()
            {
                Group = groupID,
            };
            int i = 0;
            foreach (TSource item in source)
            {
                int localI = i++;
                QueueWorkItem(() => { body(item, localI); }, workOption);
            }
            return GetGroup(groupID);
        }

        /// <summary>
        /// Watches an observable collection for changes and processes each element in the collection using the specified action. 
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
        /// <param name="source">The source collection of elements to be processed.</param>
        /// <param name="body">The action to execute for each element in the source collection and its index.</param>
        /// <param name="addBackWhenWorkCanceled">If the work is canceled, the elements will be added back to the collection.</param>
        /// <param name="addBackWorkStopped">If the work is stopped, the elements will be added back to the collection.</param>
        /// <param name="addBackWhenWorkFailed">If an exception occurs, the elements will be added back to the collection.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns></returns>
        public Group Watch<TSource>(
            ConcurrentObservableCollection<TSource> source,
            Action<TSource> body,
            bool addBackWhenWorkCanceled = true,
            bool addBackWorkStopped = true,
            bool addBackWhenWorkFailed = true,
            string groupName = null)
        {
            string groupID = null;
            if (string.IsNullOrEmpty(groupName))
            {
                groupID = Guid.NewGuid().ToString();
            }
            else
            {
                groupID = groupName;
            }
            WorkOption workOption = new WorkOption()
            {
                Group = groupID,
            };

            ConcurrentDictionary<string, TSource> idDict = new ConcurrentDictionary<string, TSource>();

            if (addBackWhenWorkCanceled)
            {
                WorkCanceled += (sWorkCanceled, eWorkCanceled) =>
                {
                    if (idDict.TryRemove(eWorkCanceled.ID, out TSource item))
                    {
                        source.TryAdd(item);
                    }
                };
            }
            if (addBackWorkStopped)
            {
                WorkStopped += (sWorkStopped, eWorkStopped) =>
                {
                    if (idDict.TryRemove(eWorkStopped.ID, out TSource item))
                    {
                        source.TryAdd(item);
                    }
                };
            }
            if (addBackWhenWorkFailed)
            {
                WorkEnded += (sWorkEnded, eWorkEnded) =>
                {
                    if (eWorkEnded.Succeed)
                    {
                        return;
                    }
                    if (idDict.TryRemove(eWorkEnded.ID, out TSource item))
                    {
                        source.TryAdd(item);
                    }
                };
            }

            void OnCollectionChanged(object sender, EventArgs e)
            {
                source.CollectionChanged -= OnCollectionChanged;
                if (source._canWatch.TrySet(CanWatch.NotAllowed, CanWatch.Allowed))
                {
                    while (source.TryTake(out TSource item))
                    {
                        string id = QueueWorkItem(() =>
                        {
                            body(item);
                        }, workOption);
                        idDict[id] = item;
                    }
                    source._canWatch.InterlockedValue = CanWatch.Allowed;
                    if (source._watchState == WatchStates.Watching)
                    {
                        source.CollectionChanged += OnCollectionChanged;
                    }
                }
            }

            if (!source.StartWatching(OnCollectionChanged))
            {
                return null;
            }

            Group group = GetGroup(groupID);
            source._group = group;

            OnCollectionChanged(null, null);

            return group;
        }

        /// <summary>
        /// Stops watching the observable collection for changes.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="keepRunning"></param>
        /// <param name="forceStop"></param>
        public void StopWatching<TSource>(ConcurrentObservableCollection<TSource> source, bool keepRunning = false, bool forceStop = false)
        {
            source.StopWatching(keepRunning, forceStop);
        }
    }
}
