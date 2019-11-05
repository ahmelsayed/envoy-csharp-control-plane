using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.ListenerNS;
using Envoy.Config.Filter.Network.HttpConnectionManager.V2;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Envoy.Control.Cache
{
    public class Resources
    {
        // private static readonly Logger LOGGER = LoggerFactory.getLogger(Resources.class);

        private const string TYPE_URL_PREFIX = "type.googleapis.com/envoy.api.v2.";

        public const string FILTER_ENVOY_ROUTER = "envoy.router";
        public const string FILTER_HTTP_CONNECTION_MANAGER = "envoy.http_connection_manager";
        public const string CLUSTER_TYPE_URL = TYPE_URL_PREFIX + "Cluster";
        public const string ENDPOINT_TYPE_URL = TYPE_URL_PREFIX + "ClusterLoadAssignment";
        public const string LISTENER_TYPE_URL = TYPE_URL_PREFIX + "Listener";
        public const string ROUTE_TYPE_URL = TYPE_URL_PREFIX + "RouteConfiguration";
        public const string SECRET_TYPE_URL = TYPE_URL_PREFIX + "auth.Secret";

        public static readonly IEnumerable<string> TYPE_URLS = new string[]
        {
             CLUSTER_TYPE_URL,
             ENDPOINT_TYPE_URL,
             LISTENER_TYPE_URL,
             ROUTE_TYPE_URL,
             SECRET_TYPE_URL
        }.ToImmutableArray();

        public static readonly ImmutableDictionary<string, System.Type> RESOURCE_TYPE_BY_URL =
            new Dictionary<string, System.Type>
            {
                { CLUSTER_TYPE_URL, typeof(Cluster) },
                { ENDPOINT_TYPE_URL, typeof(ClusterLoadAssignment) },
                { LISTENER_TYPE_URL, typeof(Listener) },
                { ROUTE_TYPE_URL, typeof(RouteConfiguration) },
                { SECRET_TYPE_URL, typeof(Secret) }
            }.ToImmutableDictionary();

        public static string GetResourceName(IMessage resource)
        {
            if (resource is Cluster cluster)
            {
                return cluster.Name;
            }

            if (resource is ClusterLoadAssignment clusterLoadAssignment)
            {
                return clusterLoadAssignment.ClusterName;
            }

            if (resource is Listener listener)
            {
                return listener.Name;
            }

            if (resource is RouteConfiguration routeConfiguration)
            {
                return routeConfiguration.Name;
            }

            if (resource is Secret secret)
            {
                return secret.Name;
            }

            return string.Empty;
        }

        public static string GetResourceName(Any anyResource)
        {
            if (RESOURCE_TYPE_BY_URL.TryGetValue(anyResource.TypeUrl, out System.Type type))
            {
                var method = typeof(Any).GetMethod("Unpack");
                if (method == null)
                {
                    throw new InvalidOperationException($"Any type is missing Unpack method. {typeof(Any).AssemblyQualifiedName}");
                }

                var genericMethod = method.MakeGenericMethod(type);
                var result = genericMethod.Invoke(anyResource, null);
                if (result != null && result is IMessage message)
                {
                    return GetResourceName(message);
                }
                else if (result == null)
                {
                    throw new InvalidOperationException($"Cannot unpack type");
                }
                else
                {
                    throw new InvalidCastException($"Expected {nameof(IMessage)}, but got {result.GetType().FullName}");
                }
            }
            else
            {
                throw new InvalidOperationException($"cannot unpack non-xDS message type: {anyResource.TypeUrl}");
            }
        }

        public static ISet<string> GetResourceReferences<T>(IEnumerable<T> resources) where T : IMessage
        {
            var refs = ImmutableHashSet.CreateBuilder<string>();

            foreach (IMessage resource in resources)
            {
                if (resource is ClusterLoadAssignment || resource is RouteConfiguration)
                {
                    // Endpoints have no dependencies.

                    // References to clusters in routes (and listeners) are not included in the result, because the clusters are
                    // currently retrieved in bulk, and not by name.

                    continue;
                }

                if (resource is Cluster cluster)
                {
                    // For EDS clusters, use the cluster name or the service name override.
                    if (cluster.Type == Cluster.Types.DiscoveryType.Eds)
                    {
                        if (!string.IsNullOrEmpty(cluster.EdsClusterConfig.ServiceName))
                        {
                            refs.Add(cluster.EdsClusterConfig.ServiceName);
                        }
                        else
                        {
                            refs.Add(cluster.Name);
                        }
                    }
                }
                else if (resource is Listener listener)
                {
                    // Extract the route configuration names from the HTTP connection manager.
                    foreach (FilterChain chain in listener.FilterChains)
                    {
                        foreach (Filter filter in chain.Filters)
                        {
                            if (!string.Equals(filter.Name, FILTER_HTTP_CONNECTION_MANAGER))
                            {
                                continue;
                            }

                            try
                            {

                                // TODO: Filter#getConfig() is deprecated, migrate to use Filter#getTypedConfig().
                                // TODO: revisit
                                var config = StructToHttpConnectionManager(filter.Config);

                                if (config.RouteSpecifierCase == HttpConnectionManager.RouteSpecifierOneofCase.Rds &&
                                    !string.IsNullOrEmpty(config.Rds.RouteConfigName))
                                {
                                    refs.Add(config.Rds.RouteConfigName);
                                }
                            }
                            catch (InvalidProtocolBufferException)
                            {
                                // LOGGER.error(
                                //     "Failed to convert HTTP connection manager config struct into protobuf message for listener {}",
                                //     getResourceName(l),
                                //     e);
                            }
                        }
                    }
                }
            }

            return refs.ToImmutable();
        }

        // TODO: revisit
        private static HttpConnectionManager StructToHttpConnectionManager(Struct _struct)
        {
            var json = new JsonFormatter(JsonFormatter.Settings.Default.WithPreserveProtoFieldNames(true)).Format(_struct);
            return JsonParser.Default.Parse<HttpConnectionManager>(json);
        }
    }
}