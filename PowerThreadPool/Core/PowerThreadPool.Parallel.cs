using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    }
}
