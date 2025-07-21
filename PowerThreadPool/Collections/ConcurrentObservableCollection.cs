using System;
using System.Collections.Concurrent;
using PowerThreadPool.Constants;
using PowerThreadPool.Groups;
using PowerThreadPool.Helpers.LockFree;

public class ConcurrentObservableCollection<T>
{
    internal InterlockedFlag<WatchStates> _watchState = WatchStates.Idle;
    internal InterlockedFlag<CanWatch> _canWatch = CanWatch.Allowed;

    private readonly IProducerConsumerCollection<T> _innerProducerConsumerCollection;
    private readonly BlockingCollection<T> _innerBlockingCollection;
    internal Group _group = null;

    public event EventHandler CollectionChanged;

    public ConcurrentObservableCollection(IProducerConsumerCollection<T> collection)
    {
        _innerProducerConsumerCollection = collection;
        _innerBlockingCollection = null;
    }

    public ConcurrentObservableCollection(BlockingCollection<T> collection)
    {
        _innerBlockingCollection = collection;
        _innerProducerConsumerCollection = null;
    }

    public ConcurrentObservableCollection()
    {
        _innerBlockingCollection = new BlockingCollection<T>();
        _innerProducerConsumerCollection = null;
    }

    /// <summary>
    /// Gets the number of elements contained in the collection
    /// </summary>
    public int Count
    {
        get
        {
            int count = -1;
            if (_innerProducerConsumerCollection != null)
            {
                count = _innerProducerConsumerCollection.Count;
            }
            else if (_innerBlockingCollection != null)
            {
                count = _innerBlockingCollection.Count;
            }
            return count;
        }
    }

    internal virtual void OnCollectionChanged()
    {
        if (CollectionChanged != null)
        {
            CollectionChanged.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Attempts to add an object to the collection
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool TryAdd(T item)
    {
        bool res = false;
        if (_innerProducerConsumerCollection != null)
        {
            res = _innerProducerConsumerCollection.TryAdd(item);
        }
        else if (_innerBlockingCollection != null)
        {
            res = _innerBlockingCollection.TryAdd(item);
        }
        if (res)
        {
            OnCollectionChanged();
        }
        return res;
    }

    /// <summary>
    /// Attempts to remove and return an object from the collection
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool TryTake(out T item)
    {
        bool res = false;
        item = default;
        if (_innerProducerConsumerCollection != null)
        {
            res = _innerProducerConsumerCollection.TryTake(out item);
        }
        else if (_innerBlockingCollection != null)
        {
            res = _innerBlockingCollection.TryTake(out item);
        }
        if (res)
        {
            OnCollectionChanged();
        }
        return res;
    }

    internal bool StartWatching(EventHandler handler)
    {
        bool res = false;
        if (_watchState.TrySet(WatchStates.Watching, WatchStates.Idle))
        {
            CollectionChanged += handler;
            res = true;
        }
        return res;
    }

    /// <summary>
    /// Stops watching the observable collection for changes.
    /// </summary>
    /// <param name="keepRunning"></param>
    /// <param name="forceStop"></param>
    public void StopWatching(bool keepRunning = false, bool forceStop = false)
    {
        if (_watchState == WatchStates.Idle)
        {
            return;
        }

        CollectionChanged = null;
        _watchState.InterlockedValue = WatchStates.Idle;

        if (!keepRunning)
        {
            _group.Stop(forceStop);
        }
    }
}
