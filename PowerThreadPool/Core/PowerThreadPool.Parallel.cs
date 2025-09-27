using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Groups;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Options;
using PowerThreadPool.Works;

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
        /// <returns>Returns a group object.</returns>
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
        /// <returns>Returns a group object.</returns>
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
        /// <returns>Returns a group object.</returns>
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
        /// <returns>Returns a group object.</returns>
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
        /// <returns>Returns a group object.</returns>
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
        /// <param name="addBackWhenWorkStopped">If the work is stopped, the elements will be added back to the collection.</param>
        /// <param name="addBackWhenWorkFailed">If an exception occurs, the elements will be added back to the collection.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a group object.</returns>
        public Group Watch<TSource>(
            ConcurrentObservableCollection<TSource> source,
            Action<TSource> body,
            bool addBackWhenWorkCanceled = true,
            bool addBackWhenWorkStopped = true,
            bool addBackWhenWorkFailed = true,
            string groupName = null)
        {
            string groupID = CreateGroupId(groupName);
            WorkOption workOption = CreateWorkOption(groupID);
            ConcurrentDictionary<WorkID, TSource> idDict = new ConcurrentDictionary<WorkID, TSource>();

            RegisterAddBackEvents(
                source,
                idDict,
                addBackWhenWorkCanceled,
                addBackWhenWorkStopped,
                addBackWhenWorkFailed);

            EventHandler onCollectionChanged = null;
            onCollectionChanged = (s, e) =>
                OnCollectionChangedHandler(source, body, workOption, idDict, onCollectionChanged);

            if (!StartWatching(source, onCollectionChanged))
            {
                return null;
            }

            Group group = GetGroup(groupID);
            source._group = group;

            onCollectionChanged(null, null);

            return group;
        }

        private string CreateGroupId(string groupName)
        {
            return string.IsNullOrEmpty(groupName) ? Guid.NewGuid().ToString() : groupName;
        }

        private WorkOption CreateWorkOption(string groupID)
        {
            return new WorkOption
            {
                Group = groupID
            };
        }

        private void RegisterAddBackEvents<TSource>(
            ConcurrentObservableCollection<TSource> source,
            ConcurrentDictionary<WorkID, TSource> idDict,
            bool addBackWhenWorkCanceled,
            bool addBackWhenWorkStopped,
            bool addBackWhenWorkFailed)
        {
            EventHandler<WorkCanceledEventArgs> onCanceled = null;
            EventHandler<WorkStoppedEventArgs> onStopped = null;
            EventHandler<WorkEndedEventArgs> onEnded = null;

            void TryAddBack(WorkID id)
            {
                TryAddBackCore(source, idDict, id);
            }

            if (addBackWhenWorkCanceled)
            {
                onCanceled = (s, e) => TryAddBack(e.ID);
                WorkCanceled += onCanceled;
                source._watchCanceledHandler = onCanceled;
            }

            if (addBackWhenWorkStopped)
            {
                onStopped = (s, e) => TryAddBack(e.ID);
                WorkStopped += onStopped;
                source._watchStoppedHandler = onStopped;
            }

            if (addBackWhenWorkFailed)
            {
                onEnded = (s, e) =>
                {
                    if (e.Succeed) return;
                    TryAddBack(e.ID);
                };
                WorkEnded += onEnded;
                source._watchEndedHandler = onEnded;
            }
        }

        private void TryAddBackCore<TSource>(
            ConcurrentObservableCollection<TSource> source,
            ConcurrentDictionary<WorkID, TSource> idDict,
            WorkID id)
        {
            TSource item = default;

            if (idDict.TryRemove(id, out item))
            {
                source.TryAdd(item);
                return;
            }

            Spinner.Start(() =>
                (idDict.TryRemove(id, out item) && source.TryAdd(item)) ||
                source._watchState == WatchStates.Idle);
        }

        private void OnCollectionChangedHandler<TSource>(
            ConcurrentObservableCollection<TSource> source,
            Action<TSource> body,
            WorkOption workOption,
            ConcurrentDictionary<WorkID, TSource> idDict,
            EventHandler onCollectionChanged)
        {
            source.CollectionChanged -= onCollectionChanged;

            if (source._canWatch.TrySet(CanWatch.NotAllowed, CanWatch.Allowed))
            {
                while (source._watchState.InterlockedValue == WatchStates.Watching
                           && source.TryTake(out TSource item))
                {
                    WorkID id = QueueWorkItem(() => body(item), workOption);
                    idDict[id] = item;
                }

                source._canWatch.InterlockedValue = CanWatch.Allowed;

                if (source._watchState == WatchStates.Watching)
                {
                    source.CollectionChanged += onCollectionChanged;
                }
            }
        }

        private bool StartWatching<TSource>(
            ConcurrentObservableCollection<TSource> source,
            EventHandler onCollectionChanged)
        {
            return source.StartWatching(onCollectionChanged);
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

            if (!keepRunning && source._group != null)
            {
                source._group.Wait(true);
            }

            if (source._watchCanceledHandler != null)
            {
                WorkCanceled -= source._watchCanceledHandler;
                source._watchCanceledHandler = null;
            }
            if (source._watchStoppedHandler != null)
            {
                WorkStopped -= source._watchStoppedHandler;
                source._watchStoppedHandler = null;
            }
            if (source._watchEndedHandler != null)
            {
                WorkEnded -= source._watchEndedHandler;
                source._watchEndedHandler = null;
            }
        }
    }
}
