﻿namespace PowerThreadPool.Collections
{
    public interface IStealablePriorityCollection<T>
    {
        /// <summary>
        /// Sets an item with a specified priority in the collection.
        /// </summary>
        /// <param name="item">The item to be added to the collection.</param>
        /// <param name="priority">The priority associated with the item.</param>
        void Set(T item, int priority);

        /// <summary>
        /// Retrieves and removes the highest priority item from the collection.
        /// The method is typically called by the owner thread to fetch the next task.
        /// </summary>
        /// <returns>The highest priority item in the collection.</returns>
        T Get();

        /// <summary>
        /// Steals and removes the lowest priority item from the collection.
        /// This method is typically called by other threads to steal work from the owner thread.
        /// </summary>
        /// <returns>The lowest priority item in the collection.</returns>
        T Steal();
    }
}
