using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
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

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        /// <summary>
        /// Creates a parallel loop that executes iterations from start to end.
        /// </summary>
        /// <param name="start">The start index of the loop.</param>
        /// <param name="end">The end index of the loop.</param>
        /// <param name="body">The action to execute for each loop iteration.</param>
        /// <param name="step">The step value for each loop iteration. Default is 1.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a group object.</returns>
        public Group For(int start, int end, Func<int, Task> body, int step = 1, string groupName = null)
        {
            return For<object>(start, end, null, async (_, index) => { await body(index); }, step, groupName);
        }
#endif

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

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
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
        public Group For<TSource>(int start, int end, IList<TSource> source, Func<TSource, Task> body, int step = 1, string groupName = null)
        {
            return For(start, end, source, async (item, _) => { await body(item); }, step, groupName);
        }
#endif

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
            string groupID;
            WorkOption workOption;
            step = PrepareFor(start, end, step, groupName, out groupID, out workOption);
            for (int i = start; start <= end ? i < end : i > end; i += step)
            {
                int localI = i;
                QueueWorkItem(() => { body(source != null ? source[localI] : default, localI); }, workOption);
            }
            return GetGroup(groupID);
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
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
        public Group For<TSource>(int start, int end, IList<TSource> source, Func<TSource, int, Task> body, int step = 1, string groupName = null)
        {
            string groupID;
            WorkOption workOption;
            step = PrepareFor(start, end, step, groupName, out groupID, out workOption);
            for (int i = start; start <= end ? i < end : i > end; i += step)
            {
                int localI = i;
                QueueWorkItem(async () => { await body(source != null ? source[localI] : default, localI); }, out _, workOption);
            }
            return GetGroup(groupID);
        }
#endif

        private int PrepareFor(int start, int end, int step, string groupName, out string groupID, out WorkOption workOption)
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

            groupID = null;
            if (string.IsNullOrEmpty(groupName))
            {
                groupID = Guid.NewGuid().ToString();
            }
            else
            {
                groupID = groupName;
            }
            workOption = new WorkOption()
            {
                Group = groupID,
            };
            return step;
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

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        /// <summary>
        /// Creates a parallel loop that executes a specified action for each element in the source collection.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
        /// <param name="source">The source collection of elements to be processed.</param>
        /// <param name="body">The action to execute for each element in the source collection.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a group object.</returns>
        public Group ForEach<TSource>(IEnumerable<TSource> source, Func<TSource, Task> body, string groupName = null)
        {
            return ForEach(source, async (item, _) => await body(item), groupName);
        }
#endif

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
            string groupID;
            WorkOption workOption;
            PrepareForEach(groupName, out groupID, out workOption);
            int i = 0;
            foreach (TSource item in source)
            {
                int localI = i++;
                QueueWorkItem(() => { body(item, localI); }, workOption);
            }
            return GetGroup(groupID);
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        /// <summary>
        /// Creates a parallel loop that executes a specified action for each element in the source collection.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source collection.</typeparam>
        /// <param name="source">The source collection of elements to be processed.</param>
        /// <param name="body">The action to execute for each element in the source collection and its index.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a group object.</returns>
        public Group ForEach<TSource>(IEnumerable<TSource> source, Func<TSource, int, Task> body, string groupName = null)
        {
            string groupID;
            WorkOption workOption;
            PrepareForEach(groupName, out groupID, out workOption);
            int i = 0;
            foreach (TSource item in source)
            {
                int localI = i++;
                QueueWorkItem(async () => { await body(item, localI); }, out _, workOption);
            }
            return GetGroup(groupID);
        }
#endif

        private static void PrepareForEach(string groupName, out string groupID, out WorkOption workOption)
        {
            groupID = null;
            if (string.IsNullOrEmpty(groupName))
            {
                groupID = Guid.NewGuid().ToString();
            }
            else
            {
                groupID = groupName;
            }

            workOption = new WorkOption()
            {
                Group = groupID,
            };
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
                OnCollectionChangedHandler(source, body, null, workOption, idDict, onCollectionChanged);

            if (!StartWatching(source, onCollectionChanged))
            {
                return null;
            }

            Group group = GetGroup(groupID);
            source._group = group;

            onCollectionChanged(null, null);

            return group;
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
            Func<TSource, Task> body,
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
                OnCollectionChangedHandler(source, null, body, workOption, idDict, onCollectionChanged);

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
            if (string.IsNullOrEmpty(groupName))
            {
                return Guid.NewGuid().ToString();
            }
            else
            {
                return groupName;
            }
        }

        private WorkOption CreateWorkOption(string groupID)
        {
            return new WorkOption
            {
                Group = groupID,
                AutoCheckStopOnAsyncTask = false,
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
                Spinner.Start(() =>
                    TryAddBackFromDict(source, idDict, id)
                    || source._watchState == WatchStates.Idle, true);
            }

            if (addBackWhenWorkCanceled)
            {
                onCanceled = (_, e) => TryAddBack(e.ID);
                WorkCanceled += onCanceled;
                source._watchCanceledHandler = onCanceled;
            }
            if (addBackWhenWorkStopped)
            {
                onStopped = (_, e) => TryAddBack(e.ID);
                WorkStopped += onStopped;
                source._watchStoppedHandler = onStopped;
            }
            if (addBackWhenWorkFailed)
            {
                onEnded = (_, e) =>
                {
                    if (e.Succeed) return;
                    TryAddBack(e.ID);
                };
                WorkEnded += onEnded;
                source._watchEndedHandler = onEnded;
            }
        }

        private bool TryAddBackFromDict<TSource>(ConcurrentObservableCollection<TSource> source, ConcurrentDictionary<WorkID, TSource> idDict, WorkID id)
        {
            if (idDict.TryRemove(id, out TSource item))
            {
                source.TryAdd(item);
                return true;
            }
            return false;
        }

        private void OnCollectionChangedHandler<TSource>(
            ConcurrentObservableCollection<TSource> source,
            Action<TSource> bodyAction,
            Func<TSource, Task> bodyFunc,
            WorkOption workOption,
            ConcurrentDictionary<WorkID, TSource> idDict,
            EventHandler onCollectionChanged)
        {
            source.CollectionChanged -= onCollectionChanged;

            if (source._canWatch.TrySet(CanWatch.NotAllowed, CanWatch.Allowed))
            {
                while (source.TryTake(out TSource item))
                {
                    WorkID id = null;
                    if (bodyAction != null)
                    {
                        id = QueueWorkItem(() => bodyAction(item), workOption);
                    }
                    else
                    {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
                        id = QueueWorkItem(async () => await bodyFunc(item), out _, workOption);
#else
                        throw new InvalidOperationException("Asynchronous body function is not supported in this framework version.");
#endif
                    }
                    idDict[id] = item;
                }

                source._canWatch.InterlockedValue = CanWatch.Allowed;

                if (source._watchState.InterlockedValue == WatchStates.Watching)
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
        public void StopWatching<TSource>(ConcurrentObservableCollection<TSource> source, bool keepRunning = false)
        {
            StopWatchingCore(source, false, keepRunning);
        }

        /// <summary>
        /// Force tops watching the observable collection for changes.
        /// Although this approach is safer than Thread.Abort, from the perspective of the business logic,
        /// it can still potentially lead to unpredictable results and cannot guarantee the time consumption of exiting the thread,
        /// therefore you should avoid using force stop as much as possible.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="keepRunning"></param>
        public void ForceStopWatching<TSource>(ConcurrentObservableCollection<TSource> source, bool keepRunning = false)
        {
            StopWatchingCore(source, true, keepRunning);
        }

        /// <summary>
        /// Stops watching the observable collection for changes.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="forceStop"></param>
        /// <param name="keepRunning"></param>
        private void StopWatchingCore<TSource>(ConcurrentObservableCollection<TSource> source, bool forceStop, bool keepRunning = false)
        {
            if (forceStop)
            {
                source.ForceStopWatching(keepRunning);
            }
            else
            {
                source.StopWatching(keepRunning);
            }

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
