using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Envoy.Control.Cache
{
    public class CacheStatusInfo<T> : IStatusInfo<T>
    {
        private readonly ConcurrentDictionary<long, Watch> _watches =
            new ConcurrentDictionary<long, Watch>();

        // TODO: revisit
        public long LastWatchRequestTime => this._lastWatchRequestTime;
        private long _lastWatchRequestTime;

        public int NumWatches => this._watches.Count;
        public T NodeGroup { get; }
        public IImmutableSet<long> WatchIds => this._watches.Keys.ToImmutableHashSet();

        public CacheStatusInfo(T nodeGroup)
        {
            this.NodeGroup = nodeGroup;
        }

        public void RemoveWatch(long watchId)
            => this._watches.TryRemove(watchId, out Watch _);

        public void SetWatch(long watchId, Watch watch)
            => this._watches.TryAdd(watchId, watch);

        public void SetLastWatchRequestTime(long ticks)
            => Interlocked.Exchange(ref this._lastWatchRequestTime, ticks);

        public void WatchesRemoveIf(Func<long, Watch, bool> filter)
        {
            foreach (var watch in this._watches.ToArray())
            {
                if (filter(watch.Key, watch.Value))
                {
                    this._watches.TryRemove(watch.Key, out Watch _);
                }
            }
        }
    }
}