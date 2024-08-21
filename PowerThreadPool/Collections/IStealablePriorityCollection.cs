namespace PowerThreadPool.Collections
{
    public interface IStealablePriorityCollection<T>
    {
        void Set(T item, int priority);
        T Get();
        T Steal();
    }
}
