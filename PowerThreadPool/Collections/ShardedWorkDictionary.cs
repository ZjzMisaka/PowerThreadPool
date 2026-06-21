using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Works;

internal sealed class ShardedWorkDictionary
{
    private readonly ConcurrentDictionary<WorkID, WorkBase>[] _shards;
    private readonly int _mask;
    private readonly int _shardCount;

    public ShardedWorkDictionary()
    {
        _shardCount = ComputeShardCount();
        _shards = new ConcurrentDictionary<WorkID, WorkBase>[_shardCount];
        _mask = _shardCount - 1;

        for (int i = 0; i < _shards.Length; ++i)
        {
            _shards[i] = new ConcurrentDictionary<WorkID, WorkBase>();
        }
    }

    private int ComputeShardCount()
    {
        int target = Environment.ProcessorCount * 4;

        int pow2 = RoundUpToPowerOf2(target);

        if (pow2 < 8) pow2 = 8;
        else if (pow2 > 256) pow2 = 256;

        return pow2;
    }

    private static int RoundUpToPowerOf2(int value)
    {
        if (value <= 1) return 1;

        uint v = (uint)(value - 1);
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        return (int)(v + 1);
    }

    private ConcurrentDictionary<WorkID, WorkBase> GetShard(WorkID id)
    {
        uint h = (uint)id.GetHashCode();
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return _shards[h & _mask];
    }

    public bool TryAddValue(WorkID id, WorkBase work)
    {
        return GetShard(id).TryAdd(id, work);
    }

    public bool TryGetValue(WorkID id, out WorkBase work)
    {
        return GetShard(id).TryGetValue(id, out work);
    }

    public bool TryRemove(WorkID id, out WorkBase work)
    {
        return GetShard(id).TryRemove(id, out work);
    }

    public bool ContainsKey(WorkID id)
    {
        return GetShard(id).ContainsKey(id);
    }

    public IEnumerable<WorkBase> Values
    {
        get
        {
            foreach (var shard in _shards)
            {
                foreach (var value in shard.Values)
                {
                    yield return value;
                }
            }
        }
    }
}
