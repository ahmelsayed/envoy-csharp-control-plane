using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.Core;
using Envoy.Api.V2.Endpoint;
using Envoy.Api.V2.ListenerNS;
using Envoy.Api.V2.Route;
using Envoy.Config.Filter.Network.HttpConnectionManager.V2;
using Envoy.Control.Cache;
using Envoy.Type.Matcher;
using Google.Protobuf.WellKnownTypes;
using static Envoy.Api.V2.Cluster.Types;
using static Envoy.Api.V2.Core.ApiConfigSource.Types;
using static Envoy.Api.V2.Core.SocketAddress.Types;
using static Envoy.Config.Filter.Network.HttpConnectionManager.V2.HttpConnectionManager.Types;
using static Envoy.Type.Matcher.RegexMatcher.Types;

namespace Envoy.Control.Runner
{
    public static class Data
    {
        const string xdsControllerName = "envoy_controller_cluster";

        public static Snapshot CreateBaseSnapshot()
        {
            return new Snapshot(
                Enumerable.Empty<Cluster>(),
                Enumerable.Empty<ClusterLoadAssignment>(),
                new[] { CreateHTTPListener() },
                new[] { CreateHTTPRoute() },
                Enumerable.Empty<Secret>(),
                Guid.NewGuid().ToString()
            );
        }

        public static Cluster CreateCluster(string name)
        {
            var cluster =  new Cluster
            {
                Name = name,
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                Type = DiscoveryType.Eds,
                LbPolicy = LbPolicy.RoundRobin,
                EdsClusterConfig = new EdsClusterConfig
                {
                    EdsConfig = GetXdsConfigSource()
                }
            };

            return cluster;
        }

        public static ClusterLoadAssignment CreateClusterLoadAssignment(string clusterName, IEnumerable<(string address, int port)> endpoints)
        {
            var localityLbEndpoints = new LocalityLbEndpoints();
            localityLbEndpoints.LbEndpoints.AddRange(endpoints.Select(e => new LbEndpoint
            {
                Endpoint = new Endpoint
                {
                    Address = new Address
                    {
                        SocketAddress = new SocketAddress
                        {
                            Address = e.address,
                            Protocol = Protocol.Tcp,
                            PortValue = (uint)e.port,
                        }
                    }
                }
            }));
            var cla = new ClusterLoadAssignment
            {
                ClusterName = clusterName,
            };
            cla.Endpoints.Add(localityLbEndpoints);
            return cla;
        }

        public static VirtualHost CreateVirtualHost(string name, string domain, string clusterName)
        {
            var virtualHost = new VirtualHost
            {
                Name = name,
            };
            virtualHost.Domains.Add(domain);
            virtualHost.Routes.Add(new Route
            {
                Match = new RouteMatch
                {
                    SafeRegex = new RegexMatcher
                    {
                        Regex = "/*",
                        GoogleRe2 = new GoogleRE2()
                    },
                },
                Route_ = new RouteAction
                {
                    Cluster = clusterName,
                },
            });
            return virtualHost;
        }


        public static ConfigSource GetXdsConfigSource()
        {
            var configSource = new ConfigSource
            {
                ApiConfigSource = new ApiConfigSource
                {
                    ApiType = ApiType.Grpc,
                    SetNodeOnFirstMessageOnly = true,
                }
            };
            configSource
                .ApiConfigSource
                .GrpcServices
                .Add(new GrpcService
                {
                    EnvoyGrpc = new GrpcService.Types.EnvoyGrpc
                    {
                        ClusterName = xdsControllerName
                    }
                });
            return configSource;
        }

        internal static RouteConfiguration UpdateRoutes(VirtualHost virtualHost, RouteConfiguration existingRoute)
        {
            var newRoute = CreateHTTPRoute();
            var virtualHosts = existingRoute.VirtualHosts.AddOrUpdateInPlace(virtualHost, (a, b) => a.Name == b.Name);
            newRoute.VirtualHosts.AddRange(virtualHosts);
            return newRoute;
        }

        public static RouteConfiguration CreateHTTPRoute()
        {
            return new RouteConfiguration
            {
                Name = "http_route"
            };
        }

        static int versions = 0;
        public static Listener CreateHTTPListener()
        {
            var listener = new Listener
            {
                Name = $"http_listener",
                Address = new Address
                {
                    SocketAddress = new SocketAddress
                    {
                        Protocol = Protocol.Tcp,
                        Address = "0.0.0.0",
                        PortValue = 8080,
                    }
                }
            };
            listener.FilterChains.Add(CreateHTTPFilterChain("http_route"));
            return listener;
        }

        public static FilterChain CreateHTTPFilterChain(string routeName)
        {
            var filterChain = new FilterChain();
            filterChain.Filters.Add(new Filter
            {
                Name = "envoy.http_connection_manager",
                TypedConfig = Any.Pack(CreateHttpConnectionManager(routeName)),
            });
            return filterChain;
        }

        public static HttpConnectionManager CreateHttpConnectionManager(string routeName)
        {
            var manager = new HttpConnectionManager
            {
                CodecType = CodecType.Auto,
                StatPrefix = "ingress",
                UseRemoteAddress = true,
                Rds = new Rds
                {
                    RouteConfigName = routeName,
                    ConfigSource =  GetXdsConfigSource()
                }
            };
            manager.HttpFilters.Add(new HttpFilter
            {
                Name = "envoy.router"
            });
            return manager;
        }
    }
}