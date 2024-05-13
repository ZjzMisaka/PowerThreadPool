namespace PowerThreadPool.Collections
{
    internal interface IConcurrentPriorityCollection<T>
    {
        void Set(T item, int priority);
        T Get();
    }
}
