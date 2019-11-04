using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.Core;
using Envoy.Api.V2.ListenerNS;
using Envoy.Api.V2.Route;
using Envoy.Config.Filter.Network.HttpConnectionManager.V2;
using Envoy.Control.Cache;
using Envoy.Control.Server;
using Google.Protobuf.WellKnownTypes;
using static Envoy.Api.V2.Cluster.Types;

namespace Test
{
    public class TestMain
    {
        private const string GROUP = "test-id";

        public static async Task Main(string[] args)
        {
            var cache = new SimpleCache<string>(_ => GROUP);
            var simplecluster = new Cluster
            {
                Name = "cluster0",
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                Type = DiscoveryType.Static,
                DnsLookupFamily = DnsLookupFamily.V4Only,
                LbPolicy = LbPolicy.RoundRobin,
                TlsContext = new UpstreamTlsContext
                {
                    Sni = "www.google.com"
                }
            };

            var cluster = new Cluster
            {
                Name = "service_google",
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                Type = DiscoveryType.Static,
                DnsLookupFamily = DnsLookupFamily.V4Only,
                LbPolicy = LbPolicy.RoundRobin,
                TlsContext = new UpstreamTlsContext
                {
                    Sni = "www.google.com"
                }
            };

            cluster.Hosts.Add(new Address
            {
                SocketAddress = new SocketAddress
                {
                    Address = "google.com",
                    PortValue = 443
                }
            });
            var virtualHost = new VirtualHost
            {
                Name = "local_service",
            };
            virtualHost.Domains.Add("*");
            virtualHost.Routes.Add(new Route
            {
                Match = new RouteMatch
                {
                    Prefix = "/"
                },
                Route_ = new RouteAction
                {
                    HostRewrite = "www.google.com",
                    Cluster = "service_google"
                },
            });

            var manager = new HttpConnectionManager
            {
                CodecType = HttpConnectionManager.Types.CodecType.Auto,
                StatPrefix = "ingress_http",
                RouteConfig = new RouteConfiguration
                {
                    Name = "local_route",
                },
            };

            manager.RouteConfig.VirtualHosts.Add(virtualHost);
            manager.HttpFilters.Add(new HttpFilter { Name = "envoy.router" });

            var listener = new Listener
            {
                Name = "listener_0",
                Address = new Address
                {
                    SocketAddress = new SocketAddress
                    {
                        Protocol = SocketAddress.Types.Protocol.Tcp,
                        Address = "0.0.0.0",
                        PortValue = 10000
                    }
                }
            };


            var filterChain = new FilterChain();
            filterChain.Filters.Add(new Envoy.Api.V2.ListenerNS.Filter
            {
                Name = "envoy.http_connection_manager",
                TypedConfig = Any.Pack(manager),
            });

            listener.FilterChains.Add(filterChain);

            cache.SetSnapshot(
                GROUP,
                new Snapshot(
                    new[] { simplecluster },
                    Enumerable.Empty<ClusterLoadAssignment>(),
                    // new[] { listener },
                    Enumerable.Empty<Listener>(),
                    Enumerable.Empty<RouteConfiguration>(),
                    Enumerable.Empty<Secret>(),
                    "1"
                )
            );

            var discoveryServer = new DiscoveryServer(cache);
            discoveryServer
                .UseAggregatedDiscoveryService()
                .UseClusterDiscoveryService()
                .UseEndpointDiscoveryService()
                .UseListenerDiscoveryService()
                .UseRouteDiscoveryService()
                .UseSecretDiscoveryService();

            var task = discoveryServer.RunAsync();

            Console.WriteLine("First configuration on 1234");
            Console.WriteLine("Press enter to continue");
            Thread.Sleep(TimeSpan.FromSeconds(50));
            // Console.ReadKey();

            var updatedCluster = new Cluster
            {
                Name = "cluster1",
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                Type = DiscoveryType.Static,
            };

            updatedCluster.Hosts.Add(new Address
            {
                SocketAddress = new SocketAddress
                {
                    Address = "127.0.0.1",
                    PortValue = 1235
                }
            });
            cache.SetSnapshot(
                GROUP,
                new Snapshot(
                    new[] { updatedCluster },
                    Enumerable.Empty<ClusterLoadAssignment>(),
                    Enumerable.Empty<Listener>(),
                    Enumerable.Empty<RouteConfiguration>(),
                    Enumerable.Empty<Secret>(),
                    "2"
                )
            );
            // cache.SetSnapshot()
            Console.WriteLine("Now on 1235 for ever");
            await task;
        }
    }
}