using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Envoy.Api.V2;
using Google.Protobuf;

namespace Envoy.Control.Cache
{
    public class SimpleCache<T> : ISnapshotCache<T>
    {
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly IDictionary<T, Snapshot> snapshots = new Dictionary<T, Snapshot>();
        private readonly ConcurrentDictionary<T, CacheStatusInfo<T>> statuses = new ConcurrentDictionary<T, CacheStatusInfo<T>>();
        private long _watchCount = 0;
        public IEnumerable<T> Groups => this.statuses.Keys.ToImmutableHashSet();
        private readonly Func<Api.V2.Core.Node, T> hash;

        public SimpleCache(Func<Api.V2.Core.Node, T> hash)
        {
            this.hash = hash;
        }

        public bool ClearSnapshot(T group)
        {
            this._rwLock.EnterWriteLock();
            try
            {
                this.statuses.TryGetValue(group, out CacheStatusInfo<T> status);
                if (status != null && status.NumWatches > 0)
                {
                    // Log warning
                    // LOGGER.warn("tried to clear snapshot for group with existing watches, group={}", group);
                    return false;
                }
                this.statuses.TryRemove(group, out CacheStatusInfo<T> _);
                this.snapshots.Remove(group);
                return true;
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        public Watch CreateWatch(
            bool ads,
            DiscoveryRequest request,
            ISet<string> knownResourceNames,
            Action<Response> responseCallback)
        {
            var group = this.hash(request.Node);
            // even though we're modifying, we take a readLock to allow multiple watches to be created in parallel since it
            // doesn't conflict
            this._rwLock.EnterReadLock();
            try
            {
                var status = this.statuses.GetOrAdd(group, g => new CacheStatusInfo<T>(group));

                status.SetLastWatchRequestTime(DateTimeOffset.UtcNow.Ticks);

                this.snapshots.TryGetValue(group, out Snapshot snapshot);
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
                    long watchId = Interlocked.Increment(ref this._watchCount);

                    // if (LOGGER.isDebugEnabled())
                    // {
                    //     LOGGER.debug("open watch {} for {}[{}] from node {} for version {}",
                    //         watchId,
                    //         request.getTypeUrl(),
                    //         String.join(", ", request.getResourceNamesList()),
                    //         group,
                    //         request.getVersionInfo());
                    // }

                    status.SetWatch(watchId, watch);

                    watch.SetStop(() => status.RemoveWatch(watchId));

                    return watch;
                }

                // Otherwise, the watch may be responded immediately
                var responded = this.Respond(watch, snapshot, group);

                if (!responded)
                {
                    long watchId = Interlocked.Increment(ref this._watchCount);

                    // if (LOGGER.isDebugEnabled())
                    // {
                    //     LOGGER.debug("did not respond immediately, leaving open watch {} for {}[{}] from node {} for version {}",
                    //         watchId,
                    //         request.getTypeUrl(),
                    //         String.join(", ", request.getResourceNamesList()),
                    //         group,
                    //         request.getVersionInfo());
                    // }

                    status.SetWatch(watchId, watch);
                    watch.SetStop(() => status.RemoveWatch(watchId));
                }

                return watch;
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }
        }

        public Snapshot GetSnapshot(T group)
        {
            this._rwLock.EnterReadLock();
            try
            {
                if (this.snapshots.TryGetValue(group, out Snapshot snapshot))
                {
                    return snapshot;
                }
                return null;
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }
        }

        public void SetSnapshot(T group, Snapshot snapshot)
        {
            CacheStatusInfo<T> status;
            this._rwLock.EnterWriteLock();
            try
            {
                // Update the existing snapshot entry.
                snapshots[group] = snapshot;
                status = statuses.GetValueOrDefault(group);
            }
            finally
            {
                this._rwLock.ExitWriteLock();
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
                    // if (LOGGER.isDebugEnabled())
                    // {
                    //     LOGGER.debug("responding to open watch {}[{}] with new version {}",
                    //         id,
                    //         String.join(", ", watch.request().getResourceNamesList()),
                    //         version);
                    // }

                    this.Respond(watch, snapshot, group);

                    // Discard the watch. A new watch will be created for future snapshots once envoy ACKs the response.
                    return true;
                }

                // Do not discard the watch. The request version is the same as the snapshot version, so we wait to respond.
                return false;
            });
        }

        public IStatusInfo<T> GetStatusInfo(T group)
        {
            this._rwLock.EnterReadLock();
            try
            {
                if (this.statuses.TryGetValue(group, out CacheStatusInfo<T> status))
                {
                    return status;
                }
                return null;
            }
            finally
            {
                this._rwLock.ExitReadLock();
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
                    // LOGGER.info(
                    //     "not responding in ADS mode for {} from node {} at version {} for request [{}] since [{}] not in snapshot",
                    //     watch.request().getTypeUrl(),
                    //     group,
                    //     snapshot.version(watch.request().getTypeUrl(), watch.request().getResourceNamesList()),
                    //     String.join(", ", watch.request().getResourceNamesList()),
                    //     String.join(", ", missingNames));

                    return false;
                }
            }

            var version = snapshot.GetVersion(watch.Request.TypeUrl, watch.Request.ResourceNames);

            // LOGGER.debug("responding for {} from node {} at version {} with version {}",
            //     watch.request().getTypeUrl(),
            //     group,
            //     watch.request().getVersionInfo(),
            //     version);

            var response = CreateResponse(watch.Request, snapshotResources, version);

            try
            {
                watch.Respond(response);
                return true;
            }
            catch (WatchCancelledException e)
            {
                // LOGGER.error(
                //     "failed to respond for {} from node {} at version {} with version {} because watch was already cancelled",
                //     watch.request().getTypeUrl(),
                //     group,
                //     watch.request().getVersionInfo(),
                //     version);
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
                    .ToList();

            return new Response(request, filtered, version);
        }
    }
}