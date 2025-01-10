using System;
using System.Collections.Concurrent;
using PowerThreadPool;

public class ConcurrentObservableCollection<T>
{
    private readonly IProducerConsumerCollection<T> _innerProducerConsumerCollection;
    private readonly BlockingCollection<T> _innerBlockingCollection;
    internal volatile bool _watching = false;
    internal string _group = null;
    internal PowerPool _powerPool = null;

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

    protected virtual void OnCollectionChanged()
    {
        if (CollectionChanged != null)
        {
            CollectionChanged.Invoke(this, EventArgs.Empty);
        }
    }

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

    internal void StartWatching(EventHandler handler)
    {
        _watching = true;
        CollectionChanged += handler;
    }

    public void StopWatching(bool keepRunning = false)
    {
        _watching = false;
        CollectionChanged = null;

        if (!keepRunning)
        {
            _powerPool.GetGroup(_group).Stop();
        }
    }
}
