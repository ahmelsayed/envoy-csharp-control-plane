using System;
using System.Collections.Generic;
using System.Linq;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using FluentAssertions;
using Google.Protobuf;
using Xunit;
using static Envoy.Control.Cache.Resources;

namespace Envoy.Control.Cache.Tests
{
    public class SnapshotTests
    {
        private static readonly Random random = new Random();
        private static readonly bool Ads = random.Next() % 2 == 0;
        private static readonly string CLUSTER_NAME = "cluster0";
        private static readonly string LISTENER_NAME = "listener0";
        private static readonly string ROUTE_NAME = "route0";
        private static readonly string SECRET_NAME = "secret0";

        private static readonly uint ENDPOINT_PORT = (uint)random.Next(10000, 20000);
        private static readonly uint LISTENER_PORT = (uint)random.Next(20000, 30000);

        private static readonly Cluster CLUSTER = TestResources.CreateCluster(CLUSTER_NAME);
        private static readonly ClusterLoadAssignment ENDPOINT = TestResources.CreateEndpoint(CLUSTER_NAME, ENDPOINT_PORT);
        private static readonly Listener LISTENER = TestResources.CreateListener(Ads, LISTENER_NAME, LISTENER_PORT, ROUTE_NAME);
        private static readonly RouteConfiguration ROUTE = TestResources.CreateRoute(ROUTE_NAME, CLUSTER_NAME);
        private static readonly Secret SECRET = TestResources.CreateSecret(SECRET_NAME);

        [Fact]
        public void CreateSingleVersionSetsResourcesCorrectly()
        {
            var version = Guid.NewGuid().ToString();

            var snapshot = new Snapshot(
                new[] { CLUSTER },
                new[] { ENDPOINT },
                new[] { LISTENER },
                new[] { ROUTE },
                new[] { SECRET },
                version);

            snapshot.Clusters.Resources
                .Should()
                .Contain(new KeyValuePair<string, Cluster>(CLUSTER_NAME, CLUSTER))
                .And
                .HaveCount(1);

            snapshot.Endpoints.Resources
                .Should()
                .Contain(new KeyValuePair<string, ClusterLoadAssignment>(CLUSTER_NAME, ENDPOINT))
                .And
                .HaveCount(1);

            snapshot.Listeners.Resources
                .Should()
                .Contain(new KeyValuePair<string, Listener>(LISTENER_NAME, LISTENER))
                .And
                .HaveCount(1);

            snapshot.Routes.Resources
                .Should()
                .Contain(new KeyValuePair<string, RouteConfiguration>(ROUTE_NAME, ROUTE))
                .And
                .HaveCount(1);

            snapshot.Clusters.Version.Should().Be(version);
            snapshot.Endpoints.Version.Should().Be(version);
            snapshot.Listeners.Version.Should().Be(version);
            snapshot.Routes.Version.Should().Be(version);
        }

        [Fact]
        public void CreateSeparateVersionsSetsResourcesCorrectly()
        {
            string clustersVersion = Guid.NewGuid().ToString();
            string endpointsVersion = Guid.NewGuid().ToString();
            string listenersVersion = Guid.NewGuid().ToString();
            string routesVersion = Guid.NewGuid().ToString();
            string secretsVersion = Guid.NewGuid().ToString();

            var snapshot = new Snapshot(
                new[] { CLUSTER }, clustersVersion,
                new[] { ENDPOINT }, endpointsVersion,
                new[] { LISTENER }, listenersVersion,
                new[] { ROUTE }, routesVersion,
                new[] { SECRET }, secretsVersion
                );

            snapshot.Clusters.Resources
                .Should()
                .Contain(new KeyValuePair<string, Cluster>(CLUSTER_NAME, CLUSTER))
                .And
                .HaveCount(1);

            snapshot.Endpoints.Resources
                .Should()
                .Contain(new KeyValuePair<string, ClusterLoadAssignment>(CLUSTER_NAME, ENDPOINT))
                .And
                .HaveCount(1);

            snapshot.Listeners.Resources
                .Should()
                .Contain(new KeyValuePair<string, Listener>(LISTENER_NAME, LISTENER))
                .And
                .HaveCount(1);

            snapshot.Routes.Resources
                .Should()
                .Contain(new KeyValuePair<string, RouteConfiguration>(ROUTE_NAME, ROUTE))
                .And
                .HaveCount(1);

            snapshot.Clusters.Version.Should().Be(clustersVersion);
            snapshot.Endpoints.Version.Should().Be(endpointsVersion);
            snapshot.Listeners.Version.Should().Be(listenersVersion);
            snapshot.Routes.Version.Should().Be(routesVersion);
        }

        [Fact]
        public void ResourcesReturnsExpectedResources()
        {
            Snapshot snapshot = new Snapshot(
                new[] { CLUSTER },
                new[] { ENDPOINT },
                new[] { LISTENER },
                new[] { ROUTE },
                new[] { SECRET },
                Guid.NewGuid().ToString());

            // We have to do some lame casting to appease java's compiler, otherwise it fails to compile due to limitations with
            // generic type constraints.

            snapshot.GetResources(CLUSTER_TYPE_URL)
            .Should()
                .Contain(new KeyValuePair<string, IMessage>(CLUSTER_NAME, CLUSTER))
                .And
                .HaveCount(1);

            snapshot.GetResources(ENDPOINT_TYPE_URL)
                .Should()
                .Contain(new KeyValuePair<string, IMessage>(CLUSTER_NAME, ENDPOINT))
                .And
                .HaveCount(1);

            snapshot.GetResources(LISTENER_TYPE_URL)
                .Should()
                .Contain(new KeyValuePair<string, IMessage>(LISTENER_NAME, LISTENER))
                .And
                .HaveCount(1);

            snapshot.GetResources(ROUTE_TYPE_URL)
                .Should()
                .Contain(new KeyValuePair<string, IMessage>(ROUTE_NAME, ROUTE))
                .And
                .HaveCount(1);

            snapshot.GetResources(null).Should().BeEmpty();
            snapshot.GetResources("").Should().BeEmpty();
            snapshot.GetResources(Guid.NewGuid().ToString()).Should().BeEmpty();
        }

        [Fact]
        public void VersionReturnsExpectedVersion()
        {
            var version = Guid.NewGuid().ToString();

            var snapshot = new Snapshot(
                new[] { CLUSTER },
                new[] { ENDPOINT },
                new[] { LISTENER },
                new[] { ROUTE },
                new[] { SECRET },
                version);

            snapshot.GetVersion(CLUSTER_TYPE_URL).Should().Be(version);
            snapshot.GetVersion(ENDPOINT_TYPE_URL).Should().Be(version);
            snapshot.GetVersion(LISTENER_TYPE_URL).Should().Be(version);
            snapshot.GetVersion(ROUTE_TYPE_URL).Should().Be(version);

            snapshot.GetVersion(null).Should().BeEmpty();
            snapshot.GetVersion("").Should().BeEmpty();
            snapshot.GetVersion(Guid.NewGuid().ToString()).Should().BeEmpty();
        }

        [Fact]
        public void EnsureConsistentReturnsWithoutExceptionForConsistentSnapshot()
        {
            var snapshot = new Snapshot(
                new[] { CLUSTER },
                new[] { ENDPOINT },
                new[] { LISTENER },
                new[] { ROUTE },
                new[] { SECRET },
                Guid.NewGuid().ToString());

            snapshot.EnsureConsistent();
        }

        [Fact]
        public void EnsureConsistentThrowsIfEndpointOrRouteRefCountMismatch()
        {
            var snapshot1 = new Snapshot(
                new[] { CLUSTER },
                Enumerable.Empty<ClusterLoadAssignment>(),
                new[] { LISTENER },
                new[] { ROUTE },
                new[] { SECRET },
                Guid.NewGuid().ToString());

            Action act = () => snapshot1.EnsureConsistent();
            act.Should().Throw<SnapshotConsistencyException>()
                .WithMessage(string.Format(
                    "Mismatched {0} -> {1} reference and resource lengths, [{2}] != 0",
                    CLUSTER_TYPE_URL,
                    ENDPOINT_TYPE_URL,
                    CLUSTER_NAME));

            var snapshot2 = new Snapshot(
                new[] { CLUSTER },
                new[] { ENDPOINT },
                new[] { LISTENER },
                Enumerable.Empty<RouteConfiguration>(),
                new[] { SECRET },
                Guid.NewGuid().ToString());

            Action act2 = () => snapshot2.EnsureConsistent();

            act2.Should().Throw<SnapshotConsistencyException>()
                .WithMessage(string.Format(
                    "Mismatched {0} -> {1} reference and resource lengths, [{2}] != 0",
                    LISTENER_TYPE_URL,
                    ROUTE_TYPE_URL,
                    ROUTE_NAME));
        }

        [Fact]
        public void EnsureConsistentThrowsIfEndpointOrRouteNamesMismatch()
        {
            var otherClusterName = "someothercluster0";
            var otherRouteName = "someotherroute0";

            var snapshot1 = new Snapshot(
                new[] { CLUSTER },
                new[] { TestResources.CreateEndpoint(otherClusterName, ENDPOINT_PORT) },
                new[] { LISTENER },
                new[] { ROUTE },
                new[] { SECRET },
                Guid.NewGuid().ToString());

            Action act = () => snapshot1.EnsureConsistent();

            act.Should().Throw<SnapshotConsistencyException>()
                .WithMessage(string.Format(
                    "{0} named '{1}', referenced by a {2}, not listed in [{3}]",
                    ENDPOINT_TYPE_URL,
                    CLUSTER_NAME,
                    CLUSTER_TYPE_URL,
                    otherClusterName));

            var snapshot2 = new Snapshot(
                new[] { CLUSTER },
                new[] { ENDPOINT },
                new[] { LISTENER },
                new[] { TestResources.CreateRoute(otherRouteName, CLUSTER_NAME) },
                new[] { SECRET },
                Guid.NewGuid().ToString());

            Action act2 = () => snapshot2.EnsureConsistent();
            act2.Should().Throw<SnapshotConsistencyException>()
                .WithMessage(string.Format(
                    "{0} named '{1}', referenced by a {2}, not listed in [{3}]",
                    ROUTE_TYPE_URL,
                    ROUTE_NAME,
                    LISTENER_TYPE_URL,
                    otherRouteName));
        }
    }
}