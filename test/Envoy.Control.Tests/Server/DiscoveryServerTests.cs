using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            using var server = new DiscoveryServer(configWatcher);
            server.UseAggregatedDiscoveryService();
            await server.StartAsync();

            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(httpClientHandler);

            var channel = GrpcChannel.ForAddress("https://localhost:6000", new GrpcChannelOptions { HttpClient = httpClient });
            var client = new AggregatedDiscoveryServiceClient(channel);

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
            using var server = new DiscoveryServer(configWatcher);
            server
                .UseClusterDiscoveryService()
                .UseEndpointDiscoveryService()
                .UseListenerDiscoveryService()
                .UseRouteDiscoveryService()
                .UseSecretDiscoveryService();
            await server.StartAsync();

            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(httpClientHandler);

            var channel = GrpcChannel.ForAddress("https://localhost:6000", new GrpcChannelOptions { HttpClient = httpClient });
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
            using var server = new DiscoveryServer(configWatcher);
            server.UseAggregatedDiscoveryService();
            await server.StartAsync();

            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(httpClientHandler);

            var channel = GrpcChannel.ForAddress("https://localhost:6000", new GrpcChannelOptions { HttpClient = httpClient });
            var client = new AggregatedDiscoveryServiceClient(channel);

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
            using var server = new DiscoveryServer(configWatcher);
            server.UseAggregatedDiscoveryService();
            await server.StartAsync();

            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(httpClientHandler);

            var channel = GrpcChannel.ForAddress("https://localhost:6000", new GrpcChannelOptions { HttpClient = httpClient });
            var client = new AggregatedDiscoveryServiceClient(channel);

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
            using var server = new DiscoveryServer(configWatcher);
            server.UseAggregatedDiscoveryService();
            await server.StartAsync();

            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(httpClientHandler);

            var channel = GrpcChannel.ForAddress("https://localhost:6000", new GrpcChannelOptions { HttpClient = httpClient });
            var client = new AggregatedDiscoveryServiceClient(channel);

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
            using var server = new DiscoveryServer(configWatcher);
            server.UseAggregatedDiscoveryService();
            await server.StartAsync();

            var httpClientHandler = new HttpClientHandler();
            // Return `true` to allow certificates that are untrusted/invalid
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            var httpClient = new HttpClient(httpClientHandler);

            var channel = GrpcChannel.ForAddress("https://localhost:6000", new GrpcChannelOptions { HttpClient = httpClient });
            var client = new AggregatedDiscoveryServiceClient(channel);

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