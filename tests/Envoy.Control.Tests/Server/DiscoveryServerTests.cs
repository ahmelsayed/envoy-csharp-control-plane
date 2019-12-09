using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.Core;
using Envoy.Control.Cache;
using Envoy.Control.Cache.Tests;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using NSubstitute;
using Xunit;
using static Envoy.Api.V2.ClusterDiscoveryService;
using static Envoy.Api.V2.EndpointDiscoveryService;
using static Envoy.Api.V2.ListenerDiscoveryService;
using static Envoy.Api.V2.RouteDiscoveryService;
using static Envoy.Service.Discovery.V2.AggregatedDiscoveryService;
using static Envoy.Service.Discovery.V2.SecretDiscoveryService;

namespace Envoy.Control.Server.Tests
{
    public class DiscoveryServerTests
    {
        private static readonly Random random = new Random();
        private static readonly bool Ads = random.Next() % 2 == 0;
        private static readonly string CLUSTER_NAME = "cluster0";
        private static readonly string LISTENER_NAME = "listener0";
        private static readonly string ROUTE_NAME = "route0";
        private static readonly string SECRET_NAME = "secret0";
        private static readonly uint ENDPOINT_PORT = Ports.GetAvailablePort();
        private static readonly uint LISTENER_PORT = Ports.GetAvailablePort();
        private static readonly Node NODE = new Node
        {
            Id = "test-id",
            Cluster = "test-cluster"
        };
        private static readonly string VERSION = random.Next().ToString();
        private static readonly Cluster CLUSTER = TestResources.CreateCluster(CLUSTER_NAME);
        private static readonly ClusterLoadAssignment ENDPOINT = TestResources.CreateEndpoint(CLUSTER_NAME, ENDPOINT_PORT);
        private static readonly Listener LISTENER = TestResources.CreateListener(Ads, LISTENER_NAME, LISTENER_PORT, ROUTE_NAME);
        private static readonly RouteConfiguration ROUTE = TestResources.CreateRoute(ROUTE_NAME, CLUSTER_NAME);
        private static readonly Secret SECRET = TestResources.CreateSecret(SECRET_NAME);

        [Fact]
        public async Task TestAggregatedHandler()
        {
            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher)
                .ConfigureDiscoveryService<AggregatedDiscoveryService>()
                .Build();

            await server.StartAsync();

            var client = new AggregatedDiscoveryServiceClient(CreateGrpcChannel());

            var duplex = client.StreamAggregatedResources();
            var clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));

            await duplex.RequestStream.WriteAsync(new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.LISTENER_TYPE_URL
            });

            await duplex.RequestStream.WriteAsync(new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.CLUSTER_TYPE_URL,
            });

            var clusterRequest = new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.ENDPOINT_TYPE_URL,
            };
            clusterRequest.ResourceNames.Add(CLUSTER_NAME);
            await duplex.RequestStream.WriteAsync(clusterRequest);

            var routeRequest = new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.ROUTE_TYPE_URL,
            };
            routeRequest.ResourceNames.Add(ROUTE_NAME);
            await duplex.RequestStream.WriteAsync(routeRequest);

            var secretRequest = new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.SECRET_TYPE_URL,
            };
            secretRequest.ResourceNames.Add(SECRET_NAME);
            await duplex.RequestStream.WriteAsync(secretRequest);

            await duplex.RequestStream.CompleteAsync();

            var (responseErrors, responses, completed, error) = await clientTask;
            completed.Should().BeTrue();
            error.Should().BeFalse();
            responseErrors.Should().BeEmpty();

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                configWatcher.counts.Should().Contain(typeUrl, 1);
            }

            configWatcher.counts.Should().HaveCount(Resources.TYPE_URLS.Count());

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                responses.Should().Contain(r => r.TypeUrl == typeUrl && r.VersionInfo == VERSION,
                    "missing expected response of type %s", typeUrl);
            }
        }

        [Fact]
        public async Task TestSeparateHandlers()
        {
            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher)
                .ConfigureDiscoveryService<ClusterDiscoveryService>()
                .ConfigureDiscoveryService<EndpointDiscoveryService>()
                .ConfigureDiscoveryService<ListenerDiscoveryService>()
                .ConfigureDiscoveryService<RouteDiscoveryService>()
                .ConfigureDiscoveryService<SecretDiscoveryService>()
                .Build();
            await server.StartAsync();

            var channel = CreateGrpcChannel();
            var clusterClient = new ClusterDiscoveryServiceClient(channel);
            var endpointClient = new EndpointDiscoveryServiceClient(channel);
            var listenerClient = new ListenerDiscoveryServiceClient(channel);
            var routeClient = new RouteDiscoveryServiceClient(channel);
            var secretClient = new SecretDiscoveryServiceClient(channel);

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                var discoveryRequest = new DiscoveryRequest
                {
                    Node = NODE,
                    TypeUrl = typeUrl
                };
                Task<(List<string>, List<DiscoveryResponse>, bool, bool)> clientTask = null;
                AsyncDuplexStreamingCall<DiscoveryRequest, DiscoveryResponse> duplex = null;
                switch (typeUrl)
                {
                    case Resources.CLUSTER_TYPE_URL:
                        duplex = clusterClient.StreamClusters();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.ENDPOINT_TYPE_URL:
                        duplex = endpointClient.StreamEndpoints();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        discoveryRequest.ResourceNames.Add(CLUSTER_NAME);
                        break;
                    case Resources.LISTENER_TYPE_URL:
                        duplex = listenerClient.StreamListeners();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.ROUTE_TYPE_URL:
                        duplex = routeClient.StreamRoutes();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        discoveryRequest.ResourceNames.Add(ROUTE_NAME);
                        break;
                    case Resources.SECRET_TYPE_URL:
                        duplex = secretClient.StreamSecrets();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        discoveryRequest.ResourceNames.Add(SECRET_NAME);
                        break;
                    default:
                        Assert.True(false, "Unsupported resource type: " + typeUrl);
                        break;
                }

                await duplex.RequestStream.WriteAsync(discoveryRequest);
                await duplex.RequestStream.CompleteAsync();

                var (responseErrors, responses, completed, error) = await clientTask;

                completed.Should().BeTrue();
                error.Should().BeFalse();
                responseErrors.Should().BeEmpty();

                configWatcher.counts.Should().Contain(typeUrl, 1);
                responses.Should().Contain(
                    r => r.TypeUrl == typeUrl && r.VersionInfo == VERSION,
                          "missing expected response of type %s", typeUrl);
            }

            configWatcher.counts.Should().HaveCount(Resources.TYPE_URLS.Count());
        }

        [Fact]
        public async Task TestWatchClosed()
        {
            var configWatcher = new MockConfigWatcher(true, new Dictionary<string, (string, IEnumerable<IMessage>)>());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher)
                .ConfigureDiscoveryService<AggregatedDiscoveryService>()
                .Build();
            await server.StartAsync();

            var client = new AggregatedDiscoveryServiceClient(CreateGrpcChannel());

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                // MockDiscoveryResponseObserver responseObserver = new MockDiscoveryResponseObserver();
                var ctx = new CancellationTokenSource();
                var duplex = client.StreamAggregatedResources(cancellationToken: ctx.Token);
                var clientTask = HandleResponses(duplex.ResponseStream);

                await duplex.RequestStream.WriteAsync(new DiscoveryRequest
                {
                    Node = NODE,
                    TypeUrl = typeUrl
                });

                ctx.Cancel();
                var (responseErrors, responses, completed, error) = await clientTask;
                error.Should().BeTrue();
                completed.Should().BeFalse();
                responseErrors.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestSendError()
        {
            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher)
                .ConfigureDiscoveryService<AggregatedDiscoveryService>()
                .Build();
            await server.StartAsync();

            var client = new AggregatedDiscoveryServiceClient(CreateGrpcChannel());

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                var duplex = client.StreamAggregatedResources();
                var clientTask = HandleResponses(duplex.ResponseStream, sendError: true);


                await duplex.RequestStream.WriteAsync(new DiscoveryRequest
                {
                    Node = NODE,
                    TypeUrl = typeUrl
                });

                var (responseErrors, responses, completed, error) = await clientTask;
                completed.Should().BeFalse();
                error.Should().BeTrue();
                responseErrors.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestStaleNonce()
        {
            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher)
                .ConfigureDiscoveryService<AggregatedDiscoveryService>()
                .Build();
            await server.StartAsync();

            var client = new AggregatedDiscoveryServiceClient(CreateGrpcChannel());

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                var duplex = client.StreamAggregatedResources();
                var clientTask = HandleResponses(duplex.ResponseStream);

                await duplex.RequestStream.WriteAsync(new DiscoveryRequest
                {
                    Node = NODE,
                    TypeUrl = typeUrl,
                });

                // Stale request, should not create a new watch.
                await duplex.RequestStream.WriteAsync(new DiscoveryRequest
                {
                    Node = NODE,
                    TypeUrl = typeUrl,
                    ResponseNonce = "xyz"
                });

                // Fresh request, should create a new watch.
                await duplex.RequestStream.WriteAsync(new DiscoveryRequest
                {
                    Node = NODE,
                    TypeUrl = typeUrl,
                    ResponseNonce = "1",
                    VersionInfo = "0"
                });

                await duplex.RequestStream.CompleteAsync();

                var (responseErrors, responses, completed, error) = await clientTask;

                // Assert that 2 watches have been created for this resource type.
                configWatcher.counts[typeUrl].Should().Be(2);
            }
        }

        // TODO: test doesn't assert anything
        [Fact]
        public async Task TestAggregateHandlerDefaultRequestType()
        {
            var configWatcher = new MockConfigWatcher(true, new Dictionary<string, (string, IEnumerable<IMessage>)>());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher)
                .ConfigureDiscoveryService<AggregatedDiscoveryService>()
                .Build();
            await server.StartAsync();

            var client = new AggregatedDiscoveryServiceClient(CreateGrpcChannel());

            var duplex = client.StreamAggregatedResources();
            var clientTask = HandleResponses(duplex.ResponseStream);

            // Leave off the type URL. For ADS requests it should fail because the type URL is required.
            await duplex.RequestStream.WriteAsync(new DiscoveryRequest
            {
                Node = NODE,
            });

            await duplex.RequestStream.CompleteAsync();
            var (responseErrors, responses, completed, error) = await clientTask;
        }

        public async Task TestSeparateHandlersDefaultRequestType()
        {
            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher)
                .ConfigureDiscoveryService<ClusterDiscoveryService>()
                .ConfigureDiscoveryService<EndpointDiscoveryService>()
                .ConfigureDiscoveryService<ListenerDiscoveryService>()
                .ConfigureDiscoveryService<RouteDiscoveryService>()
                .ConfigureDiscoveryService<SecretDiscoveryService>()
                .Build();
            await server.StartAsync();

            var channel = CreateGrpcChannel();
            var clusterClient = new ClusterDiscoveryServiceClient(channel);
            var endpointClient = new EndpointDiscoveryServiceClient(channel);
            var listenerClient = new ListenerDiscoveryServiceClient(channel);
            var routeClient = new RouteDiscoveryServiceClient(channel);
            var secretClient = new SecretDiscoveryServiceClient(channel);

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                Task<(List<string>, List<DiscoveryResponse>, bool, bool)> clientTask = null;
                AsyncDuplexStreamingCall<DiscoveryRequest, DiscoveryResponse> duplex = null;
                switch (typeUrl)
                {
                    case Resources.CLUSTER_TYPE_URL:
                        duplex = clusterClient.StreamClusters();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.ENDPOINT_TYPE_URL:
                        duplex = endpointClient.StreamEndpoints();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.LISTENER_TYPE_URL:
                        duplex = listenerClient.StreamListeners();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.ROUTE_TYPE_URL:
                        duplex = routeClient.StreamRoutes();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.SECRET_TYPE_URL:
                        duplex = secretClient.StreamSecrets();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    default:
                        Assert.True(false, "Unsupported resource type: " + typeUrl);
                        break;
                }

                // Leave off the type URL. For xDS requests it should default to the value for that handler's type.
                var discoveryRequest = new DiscoveryRequest
                {
                    Node = NODE,
                };

                await duplex.RequestStream.WriteAsync(discoveryRequest);
                await duplex.RequestStream.CompleteAsync();

                var (responseErrors, _, _, _) = await clientTask;

                responseErrors.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestCallbacksAggregateHandler()
        {
            var assertionErrors = new List<string>();
            int streamCloses = 0, streamOpens = 0, streamRequests = 0, streamResponses = 0;
            var callbacks = Substitute.For<IDiscoveryServerCallbacks>();

            void ValidateTypeUrl(string typeUrl, string caller)
            {
                if (typeUrl != Resources.ANY_TYPE_URL)
                {
                    assertionErrors.Add($"{caller}#typeUrl => expected {Resources.ANY_TYPE_URL}, got {typeUrl}");
                }
            }

            callbacks
                .When(x => x.OnStreamClose(Arg.Any<long>(), Arg.Any<string>()))
                .Do(args =>
                {
                    ValidateTypeUrl(args.ArgAt<string>(1), "OnStreamClose");
                    Interlocked.Increment(ref streamCloses);
                });

            callbacks
                .When(x => x.OnStreamOpen(Arg.Any<long>(), Arg.Any<string>()))
                .Do(args =>
                {
                    ValidateTypeUrl(args.ArgAt<string>(1), "OnStreamOpen");
                    Interlocked.Increment(ref streamOpens);
                });

            callbacks
                .When(x => x.OnStreamRequest(Arg.Any<long>(), Arg.Any<DiscoveryRequest>()))
                .Do(_ => Interlocked.Increment(ref streamRequests));

            callbacks
                .When(x => x.OnStreamResponse(Arg.Any<long>(), Arg.Any<DiscoveryRequest>(), Arg.Any<DiscoveryResponse>()))
                .Do(_ => Interlocked.Increment(ref streamResponses));


            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher, callbacks)
                .ConfigureDiscoveryService<AggregatedDiscoveryService>()
                .Build();
            await server.StartAsync();

            var client = new AggregatedDiscoveryServiceClient(CreateGrpcChannel());
            var duplex = client.StreamAggregatedResources();
            var clientTask = HandleResponses(duplex.ResponseStream);

            await duplex.RequestStream.WriteAsync(new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.LISTENER_TYPE_URL
            });

            WaitUntil(ref streamOpens, 1);
            streamOpens.Should().Be(1);

            await duplex.RequestStream.WriteAsync(new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.CLUSTER_TYPE_URL
            });

            var cluster = new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.ENDPOINT_TYPE_URL,
            };
            cluster.ResourceNames.Add(CLUSTER_NAME);

            await duplex.RequestStream.WriteAsync(cluster);

            var route = new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.ROUTE_TYPE_URL
            };
            route.ResourceNames.Add(ROUTE_NAME);
            await duplex.RequestStream.WriteAsync(route);

            var secret = new DiscoveryRequest
            {
                Node = NODE,
                TypeUrl = Resources.SECRET_TYPE_URL,
            };
            secret.ResourceNames.Add(SECRET_NAME);
            await duplex.RequestStream.WriteAsync(secret);


            WaitUntil(ref streamRequests, Resources.TYPE_URLS.Count());
            streamRequests.Should().Be(Resources.TYPE_URLS.Count());

            WaitUntil(ref streamRequests, Resources.TYPE_URLS.Count());
            streamResponses.Should().Be(Resources.TYPE_URLS.Count());

            // Send another round of requests. These should not trigger any responses.
            streamResponses = 0;
            streamRequests = 0;

            await duplex.RequestStream.WriteAsync(new DiscoveryRequest
            {
                Node = NODE,
                ResponseNonce = "0",
                VersionInfo = VERSION,
                TypeUrl = Resources.LISTENER_TYPE_URL
            });

            await duplex.RequestStream.WriteAsync(new DiscoveryRequest
            {
                Node = NODE,
                ResponseNonce = "1",
                TypeUrl = Resources.CLUSTER_TYPE_URL,
                VersionInfo = VERSION,
            });

            var cluster2 = new DiscoveryRequest
            {
                Node = NODE,
                ResponseNonce = "2",
                TypeUrl = Resources.ENDPOINT_TYPE_URL,
                VersionInfo = VERSION
            };
            cluster2.ResourceNames.Add(CLUSTER_NAME);
            await duplex.RequestStream.WriteAsync(cluster2);

            var route2 = new DiscoveryRequest
            {
                Node = NODE,
                ResponseNonce = "3",
                TypeUrl = Resources.ROUTE_TYPE_URL,
                VersionInfo = VERSION
            };
            route2.ResourceNames.Add(ROUTE_NAME);
            await duplex.RequestStream.WriteAsync(route2);

            var secert2 = new DiscoveryRequest
            {
                Node = NODE,
                ResponseNonce = "4",
                TypeUrl = Resources.SECRET_TYPE_URL,
                VersionInfo = VERSION,
            };
            secert2.ResourceNames.Add(SECRET_NAME);
            await duplex.RequestStream.WriteAsync(secert2);

            WaitUntil(ref streamRequests, Resources.TYPE_URLS.Count());
            streamRequests.Should().Be(Resources.TYPE_URLS.Count());
            streamResponses.Should().Be(0);

            await duplex.RequestStream.CompleteAsync();

            WaitUntil(ref streamCloses, 1);
            streamCloses.Should().Be(1);

            assertionErrors.Should().BeEmpty();
        }

        [Fact]
        public async Task TestCallbacksSeparateHandlers()
        {
            var streamCloses = new ConcurrentDictionary<string, StrongBox<int>>();
            var streamOpens = new ConcurrentDictionary<string, StrongBox<int>>();
            var streamRequests = new ConcurrentDictionary<string, StrongBox<int>>();
            var streamResponses = new ConcurrentDictionary<string, StrongBox<int>>();

            Resources.TYPE_URLS.ForEach(typeUrl =>
            {
                streamCloses[typeUrl] = new StrongBox<int>(0);
                streamOpens[typeUrl] = new StrongBox<int>(0);
                streamRequests[typeUrl] = new StrongBox<int>(0);
                streamResponses[typeUrl] = new StrongBox<int>(0);
            });

            var assertionErrors = new List<string>();
            var callbacks = Substitute.For<IDiscoveryServerCallbacks>();

            void ValidateTypeUrl(string typeUrl, string caller)
            {
                if (!Resources.TYPE_URLS.Contains(typeUrl))
                {
                    assertionErrors.Add($"{caller}#typeUrl => expected {Resources.ANY_TYPE_URL}, got {typeUrl}");
                }
            }

            callbacks
                .When(x => x.OnStreamClose(Arg.Any<long>(), Arg.Any<string>()))
                .Do(args =>
                {
                    var typeUrl = args.ArgAt<string>(1);
                    ValidateTypeUrl(typeUrl, "OnStreamClose");
                    Interlocked.Increment(ref streamCloses[typeUrl].Value);
                });

            callbacks
                .When(x => x.OnStreamOpen(Arg.Any<long>(), Arg.Any<string>()))
                .Do(args =>
                {
                    var typeUrl = args.ArgAt<string>(1);
                    ValidateTypeUrl(typeUrl, "OnStreamOpen");
                    Interlocked.Increment(ref streamOpens[typeUrl].Value);
                });

            callbacks
                .When(x => x.OnStreamRequest(Arg.Any<long>(), Arg.Any<DiscoveryRequest>()))
                .Do(args => Interlocked.Increment(ref streamRequests[args.ArgAt<DiscoveryRequest>(1).TypeUrl].Value));

            callbacks
                .When(x => x.OnStreamResponse(Arg.Any<long>(), Arg.Any<DiscoveryRequest>(), Arg.Any<DiscoveryResponse>()))
                .Do(args => Interlocked.Increment(ref streamResponses[args.ArgAt<DiscoveryRequest>(1).TypeUrl].Value));

            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher, callbacks)
                .ConfigureDiscoveryService<ClusterDiscoveryService>()
                .ConfigureDiscoveryService<EndpointDiscoveryService>()
                .ConfigureDiscoveryService<ListenerDiscoveryService>()
                .ConfigureDiscoveryService<RouteDiscoveryService>()
                .ConfigureDiscoveryService<SecretDiscoveryService>()
                .Build();
            await server.StartAsync();

            var channel = CreateGrpcChannel();
            var clusterClient = new ClusterDiscoveryServiceClient(channel);
            var endpointClient = new EndpointDiscoveryServiceClient(channel);
            var listenerClient = new ListenerDiscoveryServiceClient(channel);
            var routeClient = new RouteDiscoveryServiceClient(channel);
            var secretClient = new SecretDiscoveryServiceClient(channel);

            foreach (var typeUrl in Resources.TYPE_URLS)
            {

                Task<(List<string>, List<DiscoveryResponse>, bool, bool)> clientTask = null;
                AsyncDuplexStreamingCall<DiscoveryRequest, DiscoveryResponse> duplex = null;
                switch (typeUrl)
                {
                    case Resources.CLUSTER_TYPE_URL:
                        duplex = clusterClient.StreamClusters();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.ENDPOINT_TYPE_URL:
                        duplex = endpointClient.StreamEndpoints();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.LISTENER_TYPE_URL:
                        duplex = listenerClient.StreamListeners();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.ROUTE_TYPE_URL:
                        duplex = routeClient.StreamRoutes();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    case Resources.SECRET_TYPE_URL:
                        duplex = secretClient.StreamSecrets();
                        clientTask = Task.Run(async () => await HandleResponses(duplex.ResponseStream));
                        break;
                    default:
                        Assert.True(false, "Unsupported resource type: " + typeUrl);
                        break;
                }

                var discoveryRequest = new DiscoveryRequest
                {
                    Node = NODE,
                    TypeUrl = typeUrl
                };

                await duplex.RequestStream.WriteAsync(discoveryRequest);
                WaitUntil(ref streamOpens[typeUrl].Value, 1);
                streamOpens[typeUrl].Value.Should().Be(1);

                WaitUntil(ref streamRequests[typeUrl].Value, 1);
                streamRequests[typeUrl].Value.Should().Be(1);

                await duplex.RequestStream.CompleteAsync();

                WaitUntil(ref streamResponses[typeUrl].Value, 1);
                streamResponses[typeUrl].Value.Should().Be(1);

                WaitUntil(ref streamCloses[typeUrl].Value, 1);
                streamCloses[typeUrl].Value.Should().Be(1);
            }

            assertionErrors.Should().BeEmpty();
        }

        [Fact]
        public async Task TestCallbacksOnError()
        {
            int streamClosesWithErrors = 0;
            var callbacks = Substitute.For<IDiscoveryServerCallbacks>();

            callbacks
                .When(x => x.OnStreamCloseWithError(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<Exception>()))
                .Do(_ => Interlocked.Increment(ref streamClosesWithErrors));

            var configWatcher = new MockConfigWatcher(false, CreateResponses());
            using var server = DiscoveryServerBuilder
                .CreateFor(configWatcher, callbacks)
                .ConfigureDiscoveryService<AggregatedDiscoveryService>()
                .Build();

            await server.StartAsync();

            var client = new AggregatedDiscoveryServiceClient(CreateGrpcChannel());
            var ctx = new CancellationTokenSource();
            var duplex = client.StreamAggregatedResources(cancellationToken: ctx.Token);
            await duplex.RequestStream.WriteAsync(new DiscoveryRequest());
            ctx.Cancel();
            WaitUntil(ref streamClosesWithErrors, 1, TimeSpan.FromSeconds(1));
            streamClosesWithErrors.Should().Be(1);
        }

        private void WaitUntil(ref int streamOpens, int condition, TimeSpan timeSpan = default)
        {
            timeSpan = timeSpan == default ? TimeSpan.FromSeconds(1) : timeSpan;
            var trials = (int)(timeSpan.TotalMilliseconds / 50.0);
            while (streamOpens != condition && trials-- != 0)
            {
                Thread.Sleep(50);
            }
        }

        private async Task<(List<string>, List<DiscoveryResponse>, bool, bool)> HandleResponses(IAsyncStreamReader<DiscoveryResponse> responseStream, bool sendError = false)
        {
            var errors = new List<string>();
            var responses = new List<DiscoveryResponse>();
            int nonce = 0;
            bool completed = false;
            bool error = false;
            try
            {
                await foreach (var value in responseStream.ReadAllAsync())
                {
                    var nonceStr = Interlocked.Increment(ref nonce).ToString();

                    if (nonceStr != value.Nonce)
                    {
                        errors.Add(string.Format("Nonce => got {0}, wanted {1}", value.Nonce, nonce));
                    }

                    // Assert that the version is set.
                    if (string.IsNullOrEmpty(value.VersionInfo))
                    {
                        errors.Add("VersionInfo => got none, wanted non-empty");
                    }

                    // Assert that resources are non-empty.
                    if (!value.Resources.Any())
                    {
                        errors.Add("Resources => got none, wanted non-empty");
                    }

                    if (string.IsNullOrEmpty(value.TypeUrl))
                    {
                        errors.Add("TypeUrl => got none, wanted non-empty");
                    }

                    value.Resources.ToList().ForEach(r =>
                    {
                        if (value.TypeUrl != r.TypeUrl)
                        {
                            errors.Add(string.Format("TypeUrl => got {0}, wanted {1}", r.TypeUrl, value.TypeUrl));
                        }
                    });
                    responses.Add(value);
                    if (sendError)
                    {
                        throw new Exception();
                    }
                }
                completed = true;
            }
            catch (Exception)
            {
                error = true;
            }
            return (errors, responses, completed, error);
        }

        private static Dictionary<string, (string, IEnumerable<IMessage>)> CreateResponses()
        {
            return new Dictionary<string, (string, IEnumerable<IMessage>)>
            {
                { Resources.CLUSTER_TYPE_URL, (VERSION, new [] { CLUSTER })},
                { Resources.ENDPOINT_TYPE_URL, (VERSION, new[] { ENDPOINT })},
                { Resources.LISTENER_TYPE_URL, (VERSION, new[] { LISTENER })},
                { Resources.ROUTE_TYPE_URL, (VERSION, new[] { ROUTE })},
                { Resources.SECRET_TYPE_URL, (VERSION, new[] { SECRET })},
            };
        }


        private GrpcChannel CreateGrpcChannel(string host = "localhost", int port = 5000, bool useHttps = true)
        {
            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(httpClientHandler);

            return GrpcChannel.ForAddress($"{(useHttps ? "https" : "http")}://{host}:{port}", new GrpcChannelOptions { HttpClient = httpClient });
        }

    }
    public class MockConfigWatcher : IConfigWatcher
    {
        public readonly bool closeWatch;
        public readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        public readonly Dictionary<string, (string, IEnumerable<IMessage>)> responses;
        public readonly ConcurrentDictionary<string, ISet<string>> expectedKnownResources =
            new ConcurrentDictionary<string, ISet<string>>();

        public MockConfigWatcher(bool closeWatch, Dictionary<string, (string, IEnumerable<IMessage>)> responses)
        {
            this.closeWatch = closeWatch;
            this.responses = responses;
        }

        public Watch CreateWatch(
            bool ads,
            DiscoveryRequest request,
            ISet<string> knownResources,
            Action<Response> responseConsumer)
        {

            counts[request.TypeUrl] = counts.GetValueOrDefault(request.TypeUrl, 0) + 1;
            Watch watch = new Watch(ads, request, responseConsumer);

            if (responses.ContainsKey(request.TypeUrl))
            {
                Response response;

                lock (responses)
                {
                    var (version, resources) = responses[request.TypeUrl];
                    response = new Response(request, resources, version);
                }

                expectedKnownResources.TryAdd(
                    request.TypeUrl,
                    response.Resources.Select(Resources.GetResourceName).ToHashSet());

                try
                {
                    watch.Respond(response);
                }
                catch (WatchCancelledException e)
                {
                    Assert.True(false, "watch should not be cancelled" + e);
                }
            }
            else if (closeWatch)
            {
                watch.Cancel();
            }
            else
            {
                var expectedKnown = expectedKnownResources.GetValueOrDefault(request.TypeUrl, null);
                if (expectedKnown != null)
                {
                    expectedKnown.Should().BeEquivalentTo(knownResources, "unexpected known resources after sending all responses");
                }
            }

            return watch;
        }
    }

}