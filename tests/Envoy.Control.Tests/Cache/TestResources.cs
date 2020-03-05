using System;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.Core;
using Envoy.Api.V2.Endpoint;
using Envoy.Api.V2.ListenerNS;
using Envoy.Api.V2.Route;
using Envoy.Config.Filter.Network.HttpConnectionManager.V2;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using static Envoy.Api.V2.Cluster.Types;
using static Envoy.Api.V2.Core.ApiConfigSource.Types;
using static Envoy.Api.V2.Core.GrpcService.Types;
using static Envoy.Api.V2.Core.SocketAddress.Types;
using static Envoy.Config.Filter.Network.HttpConnectionManager.V2.HttpConnectionManager.Types;

namespace Envoy.Control.Cache.Tests
{
    public class TestResources
    {
        private const string ANY_ADDRESS = "0.0.0.0";
        private const string LOCALHOST = "127.0.0.1";
        private const string XDS_CLUSTER = "xds_cluster";

        public static Cluster CreateCluster(string clusterName)
        {
            var edsSource = new ConfigSource
            {
                Ads = new AggregatedConfigSource()
            };


            return new Cluster
            {
                Name = clusterName,
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                EdsClusterConfig = new EdsClusterConfig
                {
                    EdsConfig = edsSource,
                    ServiceName = clusterName,
                },
                Type = DiscoveryType.Eds
            };
        }

        public static Cluster CreateCluster(String clusterName, String address, uint port)
        {
            var cluster = new Cluster
            {
                Name = clusterName,
                ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                Type = DiscoveryType.StrictDns,
            };
            cluster.Hosts.Add(new Address
            {
                SocketAddress = new SocketAddress
                {
                    Address = address,
                    PortValue = port,
                    Protocol = Protocol.Tcp,
                },
            });
            return cluster;
        }

        public static ClusterLoadAssignment CreateEndpoint(String clusterName, uint port)
        {

            var clusterLoadAssignment = new ClusterLoadAssignment { ClusterName = clusterName };

            var localityLbEndpoints = new LocalityLbEndpoints();
            localityLbEndpoints.LbEndpoints.Add(new LbEndpoint
            {
                Endpoint = new Endpoint
                {
                    Address = new Address
                    {
                        SocketAddress = new SocketAddress
                        {
                            Address = LOCALHOST,
                            PortValue = port,
                            Protocol = Protocol.Tcp,
                        }
                    }
                }
            });

            clusterLoadAssignment.Endpoints.Add(localityLbEndpoints);

            return clusterLoadAssignment;
        }

        public static Listener CreateListener(bool ads, string listenerName, uint port, string routeName)
        {
            ConfigSource rdsSource;
            if (ads)
            {
                rdsSource = new ConfigSource
                {
                    Ads = new AggregatedConfigSource()
                };
            }
            else
            {
                rdsSource = new ConfigSource
                {
                    ApiConfigSource = new ApiConfigSource
                    {

                        ApiType = ApiType.Grpc,
                    }
                };

                rdsSource.ApiConfigSource.GrpcServices.Add(new GrpcService
                {
                    EnvoyGrpc = new EnvoyGrpc
                    {
                        ClusterName = XDS_CLUSTER
                    }
                });
            }

            var manager = new HttpConnectionManager
            {
                CodecType = CodecType.Auto,
                StatPrefix = "http",
                Rds = new Rds
                {
                    ConfigSource = rdsSource,
                    RouteConfigName = routeName,
                }
            };

            manager.HttpFilters.Add(new HttpFilter
            {
                Name = Resources.FILTER_ENVOY_ROUTER,
            });

            var listener = new Listener
            {
                Name = listenerName,
                Address = new Address
                {
                    SocketAddress = new SocketAddress
                    {
                        Address = ANY_ADDRESS,
                        PortValue = port,
                        Protocol = Protocol.Tcp,
                    }
                }
            };

            var filterChain = new FilterChain();
            filterChain.Filters.Add(new Filter
            {
                Name = Resources.FILTER_HTTP_CONNECTION_MANAGER,
                TypedConfig = Any.Pack(manager),
            });

            listener.FilterChains.Add(filterChain);
            return listener;
        }

        public static RouteConfiguration CreateRoute(string routeName, string clusterName)
        {
            var virtualHost = new VirtualHost
            {
                Name = "all",
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
                    Cluster = clusterName
                },
            });

            var routeConfiguration = new RouteConfiguration { Name = routeName };
            routeConfiguration.VirtualHosts.Add(virtualHost);
            return routeConfiguration;
        }

        public static Secret CreateSecret(String secretName)
        {
            return new Secret
            {
                Name = secretName,
                TlsCertificate = new TlsCertificate
                {
                    PrivateKey = new DataSource
                    {
                        InlineString = "secret!"
                    }
                }
            };
        }
    }
}