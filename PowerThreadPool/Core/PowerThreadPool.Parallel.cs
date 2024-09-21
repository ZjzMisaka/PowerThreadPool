using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Groups;
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
        /// Executes the provided action repeatedly while the specified condition evaluates to a non-default value of the specified type.
        /// </summary>
        /// <typeparam name="TSource">The type of the value returned by the condition function.</typeparam>
        /// <param name="condition">A function that returns a value of type TSource. The action continues to execute as long as this returns a non-default value.</param>
        /// <param name="body">The action to execute repeatedly while the condition returns a non-default value. The action receives the current value returned by the condition function.</param>
        /// <param name="groupName">The optional name for the group. Default is null.</param>
        /// <returns>Returns a group object.</returns>
        public Group Where<TSource>(Func<TSource> condition, Action<TSource> body, string groupName = null) where TSource : class
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
            ConcurrentBag<string> bag = new ConcurrentBag<string>();
            Group group = GetGroup(groupID);

            QueueWorkItem(() =>
            {
                bool continueLoop = false;
                do
                {
                    while (true)
                    {
                        TSource itemLoop = condition();
                        if (itemLoop == null)
                        {
                            break;
                        }
                        string idLoop = QueueWorkItem(() => { body(itemLoop); }, workOption);
                        bag.Add(idLoop);
                    }
                    Wait(bag);

                    TSource item = condition();
                    if (item != null)
                    {
                        string id = QueueWorkItem(() => { body(item); }, workOption);
                        bag.Add(id);
                        continueLoop = true;
                    }
                    else
                    {
                        continueLoop = false;
                    }
                } while (continueLoop);

            }, workOption);

            return group;
        }
    }
}
