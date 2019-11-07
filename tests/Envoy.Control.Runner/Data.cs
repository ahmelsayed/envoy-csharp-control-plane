using System;
using System.Linq;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.Core;
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
        public static Cluster CreateCluster(string name, string targetDomain)
        {
            var address = new Address
            {
                SocketAddress = new SocketAddress
                {
                    Address = targetDomain,
                    Protocol = Protocol.Tcp,
                    PortValue = 443,
                }
            };

            var cluster = new Cluster
            {
                Name = name,
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                Type = DiscoveryType.LogicalDns,
                DnsLookupFamily = DnsLookupFamily.V4Only,
                LbPolicy = LbPolicy.RoundRobin,
                TlsContext = new UpstreamTlsContext
                {
                    Sni = targetDomain
                }
            };
            cluster.Hosts.Add(address);
            return cluster;
        }

        public static Listener CreateListener(string name, string clusterName, string targetDomain)
        {
            var virtualHost = new VirtualHost
            {
                Name = $"local_{name}_virtualhost",
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
                    HostRewrite = targetDomain,
                    Cluster = clusterName,
                },
            });

            var manager = new HttpConnectionManager
            {
                CodecType = CodecType.Auto,
                StatPrefix = "ingress",
                RouteConfig = new RouteConfiguration
                {
                    Name = $"local_{name}_route",
                },
            };
            manager.RouteConfig.VirtualHosts.Add(virtualHost);
            manager.HttpFilters.Add(new HttpFilter
            {
                Name = "envoy.router"
            });

            var listener = new Listener
            {
                Name = $"listener_{name}",
                Address = new Address
                {
                    SocketAddress = new SocketAddress
                    {
                        Protocol = Protocol.Tcp,
                        Address = "127.0.0.1",
                        PortValue = 20000,
                    }
                }
            };
            var filterChain = new FilterChain();
            filterChain.Filters.Add(new Filter
            {
                Name = "envoy.http_connection_manager",
                TypedConfig = Any.Pack(manager),
            });
            listener.FilterChains.Add(filterChain);
            return listener;
        }

        public static Snapshot BBCSnapshot
        {
            get
            {
                var address = new Address
                {
                    SocketAddress = new SocketAddress
                    {
                        Address = "www.bbc.com",
                        Protocol = Protocol.Tcp,
                        PortValue = 443,
                    }
                };

                var cluster = new Cluster
                {
                    Name = "bbc0",
                    ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                    Type = DiscoveryType.LogicalDns,
                    DnsLookupFamily = DnsLookupFamily.V4Only,
                    LbPolicy = LbPolicy.RoundRobin,
                    TlsContext = new UpstreamTlsContext
                    {
                        Sni = "www.bbc.com"
                    }
                };
                cluster.Hosts.Add(address);

                var virtualHost = new VirtualHost
                {
                    Name = "local_bbc_service",
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
                        HostRewrite = "www.bbc.com",
                        Cluster = cluster.Name,
                    },
                });

                var manager = new HttpConnectionManager
                {
                    CodecType = CodecType.Auto,
                    StatPrefix = "ingress",
                    RouteConfig = new RouteConfiguration
                    {
                        Name = "local_bbc_route",
                    },
                };
                manager.RouteConfig.VirtualHosts.Add(virtualHost);
                manager.HttpFilters.Add(new HttpFilter
                {
                    Name = "envoy.router"
                });

                var listener = new Listener
                {
                    Name = "listener_bbc_0",
                    Address = new Address
                    {
                        SocketAddress = new SocketAddress
                        {
                            Protocol = Protocol.Tcp,
                            Address = "127.0.0.1",
                            PortValue = 20000,
                        }
                    }
                };
                var filterChain = new FilterChain();
                filterChain.Filters.Add(new Filter
                {
                    Name = "envoy.http_connection_manager",
                    TypedConfig = Any.Pack(manager),
                });
                listener.FilterChains.Add(filterChain);

                return new Snapshot(
                    new[] { cluster },
                    Enumerable.Empty<ClusterLoadAssignment>(),
                    new[] { listener },
                    Enumerable.Empty<RouteConfiguration>(),
                    Enumerable.Empty<Secret>(),
                    "1");
            }
        }

        public static Snapshot CNNSnapshot
        {
            get
            {
                var address = new Address
                {
                    SocketAddress = new SocketAddress
                    {
                        Address = "www.cnn.com",
                        Protocol = Protocol.Tcp,
                        PortValue = 443,
                    }
                };

                var cluster = new Cluster
                {
                    Name = "cnn0",
                    ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                    Type = DiscoveryType.LogicalDns,
                    DnsLookupFamily = DnsLookupFamily.V4Only,
                    LbPolicy = LbPolicy.RoundRobin,
                    TlsContext = new UpstreamTlsContext
                    {
                        Sni = "www.cnn.com"
                    }
                };
                cluster.Hosts.Add(address);

                var virtualHost = new VirtualHost
                {
                    Name = "local_cnn_service",
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
                        HostRewrite = "www.cnn.com",
                        Cluster = cluster.Name,
                    },
                });

                var manager = new HttpConnectionManager
                {
                    CodecType = CodecType.Auto,
                    StatPrefix = "ingress",
                    RouteConfig = new RouteConfiguration
                    {
                        Name = "local_google_route",
                    },
                };
                manager.RouteConfig.VirtualHosts.Add(virtualHost);
                manager.HttpFilters.Add(new HttpFilter
                {
                    Name = "envoy.router"
                });

                var listener = new Listener
                {
                    Name = "listener_cnn_0",
                    Address = new Address
                    {
                        SocketAddress = new SocketAddress
                        {
                            Protocol = Protocol.Tcp,
                            Address = "127.0.0.1",
                            PortValue = 20000,
                        }
                    }
                };
                var filterChain = new FilterChain();
                filterChain.Filters.Add(new Filter
                {
                    Name = "envoy.http_connection_manager",
                    TypedConfig = Any.Pack(manager),
                });
                listener.FilterChains.Add(filterChain);

                return new Snapshot(
                    new[] { cluster },
                    Enumerable.Empty<ClusterLoadAssignment>(),
                    new[] { listener },
                    Enumerable.Empty<RouteConfiguration>(),
                    Enumerable.Empty<Secret>(),
                    "2");
            }
        }
    }
}