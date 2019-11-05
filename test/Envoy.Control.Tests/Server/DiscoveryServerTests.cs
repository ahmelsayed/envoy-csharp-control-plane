using System;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.Core;
using Envoy.Control.Cache.Tests;

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

        // @Rule
        // public readonly GrpcServerRule grpcServer = new GrpcServerRule().directExecutor();

        // @Test
        public void testAggregatedHandler()
        {
            MockConfigWatcher configWatcher = new MockConfigWatcher(false, createResponses());
            DiscoveryServer server = new DiscoveryServer(configWatcher);

            grpcServer.getServiceRegistry().addService(server.getAggregatedDiscoveryServiceImpl());

            AggregatedDiscoveryServiceStub stub = AggregatedDiscoveryServiceGrpc.newStub(grpcServer.getChannel());

            MockDiscoveryResponseObserver responseObserver = new MockDiscoveryResponseObserver();

            StreamObserver<DiscoveryRequest> requestObserver = stub.streamAggregatedResources(responseObserver);

            requestObserver.onNext(DiscoveryRequest.newBuilder()
            .setNode(NODE)
            .setTypeUrl(Resources.LISTENER_TYPE_URL)
            .build());

            requestObserver.onNext(DiscoveryRequest.newBuilder()
                .setNode(NODE)
                .setTypeUrl(Resources.CLUSTER_TYPE_URL)
                .build());

            requestObserver.onNext(DiscoveryRequest.newBuilder()
                .setNode(NODE)
                .setTypeUrl(Resources.ENDPOINT_TYPE_URL)
                .addResourceNames(CLUSTER_NAME)
                .build());

            requestObserver.onNext(DiscoveryRequest.newBuilder()
                .setNode(NODE)
                .setTypeUrl(Resources.ROUTE_TYPE_URL)
                .addResourceNames(ROUTE_NAME)
                .build());

            requestObserver.onNext(DiscoveryRequest.newBuilder()
                .setNode(NODE)
                .setTypeUrl(Resources.SECRET_TYPE_URL)
                .addResourceNames(SECRET_NAME)
                .build());

            requestObserver.onCompleted();

            if (!responseObserver.completedLatch.await(1, TimeUnit.SECONDS) || responseObserver.error.get())
            {
                fail(format("failed to complete request before timeout, error = %b", responseObserver.error.get()));
            }

            responseObserver.assertThatNoErrors();

            for (String typeUrl : Resources.TYPE_URLS)
            {
                assertThat(configWatcher.counts).containsEntry(typeUrl, 1);
            }

            assertThat(configWatcher.counts).hasSize(Resources.TYPE_URLS.size());

            for (String typeUrl : Resources.TYPE_URLS)
            {
                assertThat(responseObserver.responses).haveAtLeastOne(new Condition<>(
                    r->r.getTypeUrl().equals(typeUrl) && r.getVersionInfo().equals(VERSION),
                    "missing expected response of type %s", typeUrl));
            }
        }

    }
}