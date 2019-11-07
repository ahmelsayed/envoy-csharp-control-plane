using System;
using System.Collections.Generic;
using System.Linq;
using Envoy.Api.V2;
using FluentAssertions;
using Xunit;

namespace Envoy.Control.Cache.Tests
{
    public class SnapshotResourcesTests
    {
        private static readonly string CLUSTER0_NAME = "cluster0";
        private static readonly string CLUSTER1_NAME = "cluster1";

        private static readonly Cluster CLUSTER0 = TestResources.CreateCluster(CLUSTER0_NAME);
        private static readonly Cluster CLUSTER1 = TestResources.CreateCluster(CLUSTER1_NAME);

        [Fact]
        public void CreateBuildsResourcesMapWithNameAndPopulatesVersion()
        {
            var version = Guid.NewGuid().ToString();

            var snapshot = new SnapshotResources<Cluster>(new[] { CLUSTER0, CLUSTER1 }, version);

            snapshot.Resources
                .Should()
                .Contain(new KeyValuePair<string, Cluster>(CLUSTER0_NAME, CLUSTER0))
                .And
                .Contain(new KeyValuePair<string, Cluster>(CLUSTER1_NAME, CLUSTER1))
                .And
                .HaveCount(2);

            snapshot.Version.Should().Be(version);
        }

        [Fact]
        public void PopulatesVersionWithSeparateVersionPerCluster()
        {
            var aggregateVersion = Guid.NewGuid().ToString();

            var versions = new Dictionary<string, string>
            {
                { CLUSTER0_NAME, Guid.NewGuid().ToString() },
                { CLUSTER1_NAME, Guid.NewGuid().ToString() }
            };

            var snapshot = new SnapshotResources<Cluster>(
                new[] { CLUSTER0, CLUSTER1 }, resourceNames =>
                {
                    if (resourceNames.Count() != 1 || !versions.ContainsKey(resourceNames.First()))
                    {
                        return aggregateVersion;
                    }
                    return versions[resourceNames.First()];
                });

            // when no resource name provided, the aggregated version should be returned
            snapshot.Version.Should().Be(aggregateVersion);

            // when one resource name is provided, the cluster version should be returned
            snapshot.GetVersion(new[] { CLUSTER0_NAME }).Should().Be(versions[CLUSTER0_NAME]);
            snapshot.GetVersion(new[] { CLUSTER1_NAME }).Should().Be(versions[CLUSTER1_NAME]);

            // when an unknown resource name is provided, the aggregated version should be returned
            snapshot.GetVersion(new[] { "unknown_cluster_name" }).Should().Be(aggregateVersion);

            // when multiple resource names are provided, the aggregated version should be returned
            snapshot.GetVersion(new[] { CLUSTER1_NAME, CLUSTER1_NAME }).Should().Be(aggregateVersion);
        }
    }
}