using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Google.Protobuf;
using static Envoy.Control.Cache.Resources;

namespace Envoy.Control.Cache
{
    public class Snapshot
    {
        public SnapshotResources<Cluster> Clusters { get; }
        public SnapshotResources<ClusterLoadAssignment> Endpoints { get; }
        public SnapshotResources<Listener> Listeners { get; }
        public SnapshotResources<RouteConfiguration> Routes { get; }
        public SnapshotResources<Secret> Secrets { get; }

        public Snapshot(
            IEnumerable<Cluster> clusters,
            IEnumerable<ClusterLoadAssignment> endpoints,
            IEnumerable<Listener> listeners,
            IEnumerable<RouteConfiguration> routes,
            IEnumerable<Secret> secrets,
            string version)
        {
            this.Clusters = new SnapshotResources<Cluster>(clusters, version);
            this.Endpoints = new SnapshotResources<ClusterLoadAssignment>(endpoints, version);
            this.Listeners = new SnapshotResources<Listener>(listeners, version);
            this.Routes = new SnapshotResources<RouteConfiguration>(routes, version);
            this.Secrets = new SnapshotResources<Secret>(secrets, version);
        }

        public Snapshot(
            IEnumerable<Cluster> clusters,
            string clustersVersion,
            IEnumerable<ClusterLoadAssignment> endpoints,
            string endpointsVersion,
            IEnumerable<Listener> listeners,
            string listenersVersion,
            IEnumerable<RouteConfiguration> routes,
            string routesVersion,
            IEnumerable<Secret> secrets,
            string secretsVersion)
        {
            this.Clusters = new SnapshotResources<Cluster>(clusters, clustersVersion);
            this.Endpoints = new SnapshotResources<ClusterLoadAssignment>(endpoints, endpointsVersion);
            this.Listeners = new SnapshotResources<Listener>(listeners, listenersVersion);
            this.Routes = new SnapshotResources<RouteConfiguration>(routes, routesVersion);
            this.Secrets = new SnapshotResources<Secret>(secrets, secretsVersion);
        }

        public Snapshot(
            IEnumerable<Cluster> clusters,
            Func<IEnumerable<string>, string> clustersVersionResolver,
            IEnumerable<ClusterLoadAssignment> endpoints,
            Func<IEnumerable<string>, string> endpointsVersionResolver,
            IEnumerable<Listener> listeners,
            Func<IEnumerable<string>, string> listenersVersionResolver,
            IEnumerable<RouteConfiguration> routes,
            Func<IEnumerable<string>, string> routesVersionResolver,
            IEnumerable<Secret> secrets,
            Func<IEnumerable<string>, string> secretsVersionResolver)
        {
            this.Clusters = new SnapshotResources<Cluster>(clusters, clustersVersionResolver);
            this.Endpoints = new SnapshotResources<ClusterLoadAssignment>(endpoints, endpointsVersionResolver);
            this.Listeners = new SnapshotResources<Listener>(listeners, listenersVersionResolver);
            this.Routes = new SnapshotResources<RouteConfiguration>(routes, routesVersionResolver);
            this.Secrets = new SnapshotResources<Secret>(secrets, secretsVersionResolver);
        }

        public void EnsureConsistent()
        {
            var clusterEndpointsRefs = GetResourceReferences(this.Clusters.Resources.Values);
            EnsureAllResourceNamesExist(
                CLUSTER_TYPE_URL,
                ENDPOINT_TYPE_URL,
                clusterEndpointsRefs,
                this.Endpoints.Resources);

            var listenerRouteRefs = GetResourceReferences(this.Listeners.Resources.Values);
            EnsureAllResourceNamesExist(
                LISTENER_TYPE_URL,
                ROUTE_TYPE_URL,
                listenerRouteRefs,
                this.Routes.Resources);
        }

        public IReadOnlyDictionary<string, IMessage> GetResources(string typeUrl)
        {
            if (string.IsNullOrEmpty(typeUrl))
            {
                return ImmutableDictionary.Create<string, IMessage>();
            }

            switch (typeUrl)
            {
                case CLUSTER_TYPE_URL:
                    return this.Clusters.Resources
                    .Select(kv => new { key = kv.Key, value = (IMessage)kv.Value })
                    .ToDictionary(a => a.key, b => b.value);
                case ENDPOINT_TYPE_URL:
                    return this.Endpoints.Resources
                    .Select(kv => new { key = kv.Key, value = (IMessage)kv.Value })
                    .ToDictionary(a => a.key, b => b.value);
                case LISTENER_TYPE_URL:
                    return this.Listeners.Resources
                    .Select(kv => new { key = kv.Key, value = (IMessage)kv.Value })
                    .ToDictionary(a => a.key, b => b.value);
                case ROUTE_TYPE_URL:
                    return this.Routes.Resources
                    .Select(kv => new { key = kv.Key, value = (IMessage)kv.Value })
                    .ToDictionary(a => a.key, b => b.value);
                case SECRET_TYPE_URL:
                    return this.Secrets.Resources
                    .Select(kv => new { key = kv.Key, value = (IMessage)kv.Value })
                    .ToDictionary(a => a.key, b => b.value);
                default:
                    return ImmutableDictionary.Create<string, IMessage>();
            }
        }

        public string GetVersion(string typeUrl)
         => this.GetVersion(typeUrl, Enumerable.Empty<string>());

        public string GetVersion(string typeUrl, IEnumerable<string> resourceNames)
        {
            if (string.IsNullOrEmpty(typeUrl))
            {
                return string.Empty;
            }

            switch (typeUrl)
            {
                case CLUSTER_TYPE_URL:
                    return this.Clusters.GetVersion(resourceNames);
                case ENDPOINT_TYPE_URL:
                    return this.Endpoints.GetVersion(resourceNames);
                case LISTENER_TYPE_URL:
                    return this.Listeners.GetVersion(resourceNames);
                case ROUTE_TYPE_URL:
                    return this.Routes.GetVersion(resourceNames);
                case SECRET_TYPE_URL:
                    return this.Secrets.GetVersion(resourceNames);
                default:
                    return string.Empty;
            }
        }

        private static void EnsureAllResourceNamesExist<T>(
            string parentTypeUrl,
            string dependencyTypeUrl,
            ISet<string> resourceNames,
            IReadOnlyDictionary<string, T> resources) where T : IMessage
        {

            if (resourceNames.Count != resources.Count)
            {
                throw new SnapshotConsistencyException(
                    string.Format(
                        "Mismatched {0} -> {1} reference and resource lengths, [{3}] != {4}",
                        parentTypeUrl,
                        dependencyTypeUrl,
                        string.Join(", ", resourceNames),
                        resources.Count));
            }

            foreach (string name in resourceNames)
            {
                if (!resources.ContainsKey(name))
                {
                    throw new SnapshotConsistencyException(
                        string.Format(
                            "{0} named '{1}', referenced by a {2}, not listed in [{3}]",
                            dependencyTypeUrl,
                            name,
                            parentTypeUrl,
                            string.Join(", ", resources.Keys)));
                }
            }
        }
    }
}