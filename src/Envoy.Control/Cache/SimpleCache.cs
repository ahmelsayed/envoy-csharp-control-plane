using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Envoy.Api.V2;
using Envoy.Api.V2.Core;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Envoy.Control.Cache
{
    public class SimpleCache<T> : ISnapshotCache<T> where T : notnull
    {
        private static readonly ILogger Logger = DiscoveryServerLoggerFactory.CreateLogger(nameof(SimpleCache<T>));
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly IDictionary<T, Snapshot> _snapshots = new Dictionary<T, Snapshot>();
        private readonly ConcurrentDictionary<T, CacheStatusInfo<T>> _statuses = new ConcurrentDictionary<T, CacheStatusInfo<T>>();
        private long _watchCount = 0;
        public IEnumerable<T> Groups => _statuses.Keys.ToImmutableHashSet();
        private readonly Func<Node, T> _hash;

        public SimpleCache(Func<Node, T> hash)
        {
            _hash = hash;
        }

        public bool ClearSnapshot(T group)
        {
            _rwLock.EnterWriteLock();
            try
            {
                _statuses.TryGetValue(group, out CacheStatusInfo<T>? status);
                if (status != null && status.NumWatches > 0)
                {
                    // Log warning
                    Logger.LogWarning("tried to clear snapshot for group with existing watches, group={0}", group);
                    return false;
                }
                _statuses.TryRemove(group, out CacheStatusInfo<T> _);
                _snapshots.Remove(group);
                return true;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public Watch CreateWatch(
            bool ads,
            DiscoveryRequest request,
            ISet<string>? knownResourceNames,
            Action<Response> responseCallback)
        {
            var group = _hash(request.Node);
            // even though we're modifying, we take a readLock to allow multiple watches to be created in parallel since it
            // doesn't conflict
            _rwLock.EnterReadLock();
            try
            {
                var status = _statuses.GetOrAdd(group, g => new CacheStatusInfo<T>(group));

                status.SetLastWatchRequestTime(DateTimeOffset.UtcNow.Ticks);

                _snapshots.TryGetValue(group, out Snapshot? snapshot);
                var version = snapshot == null
                    ? string.Empty
                    : snapshot.GetVersion(request.TypeUrl, request.ResourceNames);

                var watch = new Watch(ads, request, responseCallback);

                if (snapshot != null)
                {
                    var requestedResources = ImmutableHashSet.CreateRange(request.ResourceNames);
                    var newResourceHints = requestedResources.Except(knownResourceNames);

                    // If the request is asking for resources we haven't sent to the proxy yet, see if we have additional resources.
                    if (newResourceHints.Any())
                    {
                        // If any of the newly requested resources are in the snapshot respond immediately. If not we'll fall back to
                        // version comparisons.
                        if (snapshot.GetResources(request.TypeUrl).Keys.Any(newResourceHints.Contains))
                        {
                            this.Respond(watch, snapshot, group);
                            return watch;
                        }
                    }
                }

                // If the requested version is up-to-date or missing a response, leave an open watch.
                if (snapshot == null || request.VersionInfo.Equals(version))
                {
                    long watchId = Interlocked.Increment(ref _watchCount);

                    Logger.LogDebug("open watch {0} for {1}[{2}] from node {3} for version {4}",
                        watchId,
                        request.TypeUrl,
                        string.Join(", ", request.ResourceNames),
                        group,
                        request.VersionInfo);

                    status.SetWatch(watchId, watch);

                    watch.SetStop(() => status.RemoveWatch(watchId));

                    return watch;
                }

                // Otherwise, the watch may be responded immediately
                var responded = this.Respond(watch, snapshot, group);

                if (!responded)
                {
                    long watchId = Interlocked.Increment(ref _watchCount);


                    Logger.LogDebug("did not respond immediately, leaving open watch {0} for {1}[{2}] from node {3} for version {4}",
                        watchId,
                        request.TypeUrl,
                        string.Join(", ", request.ResourceNames),
                        group,
                        request.VersionInfo);

                    status.SetWatch(watchId, watch);
                    watch.SetStop(() => status.RemoveWatch(watchId));
                }

                return watch;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public Snapshot? GetSnapshot(T group)
        {
            _rwLock.EnterReadLock();
            try
            {
                if (_snapshots.TryGetValue(group, out Snapshot? snapshot))
                {
                    return snapshot;
                }
                return null;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public void SetSnapshot(T group, Snapshot snapshot)
        {
            CacheStatusInfo<T>? status;
            _rwLock.EnterWriteLock();
            try
            {
                // Update the existing snapshot entry.
                _snapshots[group] = snapshot;
                status = _statuses.GetValueOrDefault(group);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }

            if (status == null)
            {
                return;
            }

            status.WatchesRemoveIf((id, watch) =>
            {
                var version = snapshot.GetVersion(watch.Request.TypeUrl, watch.Request.ResourceNames);

                if (!watch.Request.VersionInfo.Equals(version))
                {
                    Logger.LogDebug("responding to open watch {0}[{1}] with new version {2}",
                        id,
                        string.Join(", ", watch.Request.ResourceNames),
                        version);

                    this.Respond(watch, snapshot, group);

                    // Discard the watch. A new watch will be created for future snapshots once envoy ACKs the response.
                    return true;
                }

                // Do not discard the watch. The request version is the same as the snapshot version, so we wait to respond.
                return false;
            });
        }

        public IStatusInfo<T>? GetStatusInfo(T group)
        {
            _rwLock.EnterReadLock();
            try
            {
                if (_statuses.TryGetValue(group, out CacheStatusInfo<T>? status))
                {
                    return status;
                }
                return null;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        private bool Respond(Watch watch, Snapshot snapshot, T group)
        {
            var snapshotResources = snapshot.GetResources(watch.Request.TypeUrl);

            if (watch.Request.ResourceNames.Any() && watch.Ads)
            {
                var missingNames = watch.Request.ResourceNames
                    .Where(name => !snapshotResources.ContainsKey(name))
                    .ToList();

                if (missingNames.Any())
                {
                    Logger.LogInformation(
                        "not responding in ADS mode for {0} from node {1} at version {2} for request [{3}] since [{4}] not in snapshot",
                        watch.Request.TypeUrl,
                        group,
                        snapshot.GetVersion(watch.Request.TypeUrl, watch.Request.ResourceNames),
                        string.Join(", ", watch.Request.ResourceNames),
                        string.Join(", ", missingNames));

                    return false;
                }
            }

            var version = snapshot.GetVersion(watch.Request.TypeUrl, watch.Request.ResourceNames);

            Logger.LogDebug("responding for {0} from node {1} at version {2} with version {3}",
                watch.Request.TypeUrl,
                group,
                watch.Request.VersionInfo,
                version);

            var response = CreateResponse(watch.Request, snapshotResources, version);

            try
            {
                watch.Respond(response);
                return true;
            }
            catch (WatchCancelledException)
            {
                Logger.LogError(
                    "failed to respond for {0} from node {1} at version {2} with version {3} because watch was already cancelled",
                    watch.Request.TypeUrl,
                    group,
                    watch.Request.VersionInfo,
                    version);
            }

            return false;
        }

        private Response CreateResponse(DiscoveryRequest request, IReadOnlyDictionary<string, IMessage> resources, string version)
        {
            var filtered = !request.ResourceNames.Any()
                ? resources.Values
                : request.ResourceNames
                    .Select(r => resources.GetValueOrDefault(r))
                    .Where(r => r != null)
                    .ToList()!;

            return new Response(request, filtered, version);
        }
    }
}