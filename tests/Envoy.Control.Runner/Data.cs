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
using static Envoy.Api.V2.Core.SocketAddress.Types;
using static Envoy.Config.Filter.Network.HttpConnectionManager.V2.HttpConnectionManager.Types;
using static Envoy.Type.Matcher.RegexMatcher.Types;

namespace Envoy.Control.Runner
{
    public static class Data
    {
        public static Cluster CreateCluster(string name, IEnumerable<(string address, int port)> endpoints)
        {
            return new Cluster
            {
                Name = name,
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                Type = DiscoveryType.LogicalDns,
                DnsLookupFamily = DnsLookupFamily.V4Only,
                LbPolicy = LbPolicy.RoundRobin,
                LoadAssignment = CreateLoadAssignment(name, endpoints)
            };
        }

        private static ClusterLoadAssignment CreateLoadAssignment(string name, IEnumerable<(string address, int port)> endpoints)
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
                ClusterName = name,
            };
            cla.Endpoints.Add(localityLbEndpoints);
            return cla;
        }

        static int versions = 0;
        internal static Snapshot CreateSnapshot(Cluster cluster, Listener listener)
        {
            return new Snapshot(
                new[] { cluster },
                Enumerable.Empty<ClusterLoadAssignment>(),
                new[] { listener },
                Enumerable.Empty<RouteConfiguration>(),
                Enumerable.Empty<Secret>(),
                Interlocked.Increment(ref versions).ToString()
            );
        }

        public static Listener CreateListener(string name, string address, int port, string clusterName)
        {

            var listener = new Listener
            {
                Name = $"listener_{name}",
                Address = new Address
                {
                    SocketAddress = new SocketAddress
                    {
                        Protocol = Protocol.Tcp,
                        Address = address,
                        PortValue = (uint)port,
                    }
                }
            };
            listener.FilterChains.Add(CreateFilterChain(name, clusterName));
            return listener;
        }

        private static FilterChain CreateFilterChain(string name, string clusterName)
        {
            var filterChain = new FilterChain();
            filterChain.Filters.Add(new Filter
            {
                Name = "envoy.http_connection_manager",
                TypedConfig = Any.Pack(CreateHttpConnectionManager(name, clusterName)),
            });
            return filterChain;
        }

        private static HttpConnectionManager CreateHttpConnectionManager(string name, string clusterName)
        {
            var manager = new HttpConnectionManager
            {
                CodecType = CodecType.Auto,
                StatPrefix = "ingress",
                RouteConfig = new RouteConfiguration
                {
                    Name = $"{name}_route",
                },
            };
            manager.RouteConfig.VirtualHosts.Add(CreateVirtualHost(name, "*", clusterName));
            manager.HttpFilters.Add(new HttpFilter
            {
                Name = "envoy.router"
            });
            return manager;
        }

        private static VirtualHost CreateVirtualHost(string name, string domain, string clusterName)
        {
            var virtualHost = new VirtualHost
            {
                Name = $"{name}_virtualhost",
            };
            virtualHost.Domains.Add("*");
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
    }
}