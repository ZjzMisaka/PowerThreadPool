using System;
using System.Collections.Concurrent;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Groups;
using PowerThreadPool.Helpers.LockFree;

public class ConcurrentObservableCollection<T>
{
    internal InterlockedFlag<WatchStates> _watchState = WatchStates.Idle;
    internal InterlockedFlag<CanWatch> _canWatch = CanWatch.Allowed;

    private readonly IProducerConsumerCollection<T> _innerProducerConsumerCollection;
    private readonly BlockingCollection<T> _innerBlockingCollection;
    internal Group _group = null;

    internal EventHandler<WorkCanceledEventArgs> _watchCanceledHandler;
    internal EventHandler<WorkStoppedEventArgs> _watchStoppedHandler;
    internal EventHandler<WorkEndedEventArgs> _watchEndedHandler;

    internal EventHandler _collectionChangedHandler;
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
            _collectionChangedHandler = handler;
            CollectionChanged += handler;
            res = true;
        }
        return res;
    }

    /// <summary>
    /// Stops watching the observable collection for changes.
    /// </summary>
    /// <param name="keepRunning"></param>
    public void StopWatching(bool keepRunning = false)
    {
        StopWatchingCore(false, keepRunning);
    }

    /// <summary>
    /// Force stops watching the observable collection for changes.
    /// Although this approach is safer than Thread.Abort, from the perspective of the business logic,
    /// it can still potentially lead to unpredictable results and cannot guarantee the time consumption of exiting the thread,
    /// therefore you should avoid using force stop as much as possible.
    /// </summary>
    /// <param name="keepRunning"></param>
    public void ForceStopWatching(bool keepRunning = false)
    {
        StopWatchingCore(true, keepRunning);
    }

    /// <summary>
    /// Stops watching the observable collection for changes.
    /// </summary>
    /// <param name="forceStop"></param>
    /// <param name="keepRunning"></param>
    private void StopWatchingCore(bool forceStop, bool keepRunning = false)
    {
        if (_watchState == WatchStates.Idle)
        {
            return;
        }

        CollectionChanged -= _collectionChangedHandler;
        _collectionChangedHandler = null;
        _watchState.InterlockedValue = WatchStates.Idle;

        Spinner.Start(() => _canWatch.InterlockedValue == CanWatch.Allowed);

        if (!keepRunning)
        {
            _group.Stop(forceStop);
        }
    }
}
