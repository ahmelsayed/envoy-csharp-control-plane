using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Envoy.Api.V2;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using static Envoy.Api.V2.Cluster.Types;

namespace Envoy.Control.Cache.Tests
{
    public class ResourcesTests
    {
        private static readonly string CLUSTER_NAME = "cluster0";
        private static readonly string LISTENER_NAME = "listener0";
        private static readonly string ROUTE_NAME = "route0";
        private static readonly Random random = new Random();
        private static readonly bool Ads = random.Next() % 2 == 0;
        private static readonly uint ENDPOINT_PORT = (uint)random.Next(10000, 20000);
        private static readonly uint LISTENER_PORT = (uint)random.Next(20000, 30000);
        private static readonly Cluster CLUSTER = TestResources.CreateCluster(CLUSTER_NAME);
        private static readonly ClusterLoadAssignment ENDPOINT = TestResources.CreateEndpoint(CLUSTER_NAME, ENDPOINT_PORT);
        private static readonly Listener LISTENER = TestResources.CreateListener(Ads, LISTENER_NAME, LISTENER_PORT, ROUTE_NAME);
        private static readonly RouteConfiguration ROUTE = TestResources.CreateRoute(ROUTE_NAME, CLUSTER_NAME);

        [Fact]
        public void GetResourceNameReturnsExpectedNameForValidResourceMessage()
        {
            var cases = new Dictionary<IMessage, string>
            {
                { CLUSTER, CLUSTER_NAME },
                { ENDPOINT, CLUSTER_NAME },
                { LISTENER, LISTENER_NAME },
                { ROUTE, ROUTE_NAME }
            }.ToImmutableDictionary();

            foreach (var c in cases)
            {
                Resources.GetResourceName(c.Key).Should().Be(c.Value);
            }
        }

        [Fact]
        public void GetResourceNameReturnsEmptyStringForNonResourceMessage()
        {
            IMessage message = new Google.Type.Color();

            Resources.GetResourceName(message).Should().BeEmpty();
        }

        [Fact]
        public void GetResourceNameAnyThrowsOnBadClass()
        {
            Action act = () => Resources.GetResourceName(new Any { TypeUrl = "garbage" });
            act.Should().Throw<InvalidOperationException>().WithMessage("cannot unpack non-xDS message type: garbage");
        }

        [Fact]
        public void GetResourceReferencesReturnsExpectedReferencesForValidResourceMessages()
        {
            var clusterServiceName = "clusterWithServiceName0";
            var clusterWithServiceName = new Cluster
            {
                Name = CLUSTER_NAME,
                Type = DiscoveryType.Eds,
                EdsClusterConfig = new EdsClusterConfig
                {
                    ServiceName = clusterServiceName
                },
            };

            var cases = new Dictionary<IEnumerable<IMessage>, ISet<string>>
            {
                    { new[] { CLUSTER }, new HashSet<string>{ CLUSTER_NAME} },
                    { new[] { clusterWithServiceName }, new HashSet<string>{clusterServiceName} },
                    { new[] { ENDPOINT }, new HashSet<string>() },
                    { new[] { LISTENER }, new HashSet<string>{ROUTE_NAME}},
                    { new[] { ROUTE }, new HashSet<string>() },
                    { new IMessage[] { CLUSTER, ENDPOINT, LISTENER, ROUTE }, new HashSet<string> { CLUSTER_NAME, ROUTE_NAME}}
            };

            foreach (var c in cases)
            {
                Resources.GetResourceReferences(c.Key).Should().BeEquivalentTo(c.Value);
            }
        }
    }
}