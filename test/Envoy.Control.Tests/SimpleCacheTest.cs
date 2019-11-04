using System;
using System.Collections.Generic;
using System.Linq;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Control.Cache;
using FluentAssertions;
using Xunit;

namespace Envoy.Control.Tests
{
    public class SimpleCacheTest
    {
        private static readonly string CLUSTER_NAME = "cluster0";
        private static readonly string SECONDARY_CLUSTER_NAME = "cluster1";
        private static readonly string LISTENER_NAME = "listener0";
        private static readonly string ROUTE_NAME = "route0";
        private static readonly string SECRET_NAME = "secret0";
        private static readonly string VERSION1 = Guid.NewGuid().ToString();
        private static readonly string VERSION2 = Guid.NewGuid().ToString();
        private static readonly ISet<string> EmptySet = new HashSet<string>();

        private static readonly Snapshot SNAPSHOT1 = new Snapshot
        (
            new[] { new Cluster { Name = CLUSTER_NAME } },
            new[] { new ClusterLoadAssignment() },
            new[] { new Listener { Name = LISTENER_NAME } },
            new[] { new RouteConfiguration { Name = ROUTE_NAME } },
            new[] { new Secret { Name = ROUTE_NAME } },
            VERSION1
        );

        private static readonly Snapshot SNAPSHOT2 = new Snapshot
        (
            new[] { new Cluster { Name = CLUSTER_NAME } },
            new[] { new ClusterLoadAssignment() },
            new[] { new Listener { Name = LISTENER_NAME } },
            new[] { new RouteConfiguration { Name = ROUTE_NAME } },
            new[] { new Secret { Name = ROUTE_NAME } },
            VERSION2
        );

        private static readonly Snapshot MULTIPLE_RESOURCES_SNAPSHOT2 = new Snapshot
        (
            new[]
            {
                new Cluster { Name = CLUSTER_NAME } ,
                new Cluster { Name = SECONDARY_CLUSTER_NAME }
            },
            new[]
            {
                new ClusterLoadAssignment { ClusterName = CLUSTER_NAME },
                new ClusterLoadAssignment { ClusterName = SECONDARY_CLUSTER_NAME},
            },
            new[] { new Listener { Name = LISTENER_NAME } },
            new[] { new RouteConfiguration { Name = ROUTE_NAME } },
            new[] { new Secret { Name = ROUTE_NAME } },
            VERSION2
        );

        [Fact]
        void InvalidNamesListShouldReturnWatcherWithNoResponseInAdsMode()
        {
            var cache = new SimpleCache<string>(_ => "key");
            cache.SetSnapshot("key", SNAPSHOT1);
            var responses = new List<Response>();

            var discoveryRequest = new DiscoveryRequest
            {
                Node = new Api.V2.Core.Node(),
                TypeUrl = Resources.ENDPOINT_TYPE_URL
            };

            discoveryRequest.ResourceNames.Add("none");

            var watch = cache.CreateWatch(
                true,
                discoveryRequest,
                EmptySet,
                responses.Add);

            AssertThatWatchIsOpenWithNoResponses(watch, responses);
        }

        [Fact]
        public void InvalidNamesListShouldReturnWatcherWithResponseInXdsMode()
        {
            var cache = new SimpleCache<string>(_ => "key");
            var responses = new List<Response>();

            cache.SetSnapshot("key", SNAPSHOT1);

            var discoveryRequest = new DiscoveryRequest
            {
                Node = new Api.V2.Core.Node(),
                TypeUrl = Resources.ENDPOINT_TYPE_URL
            };

            discoveryRequest.ResourceNames.Add("none");

            var watch = cache.CreateWatch(
                false,
                discoveryRequest,
                EmptySet,
                responses.Add);

            watch.IsCancelled.Should().BeFalse();
            responses.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SuccessfullyWatchAllResourceTypesWithSetBeforeWatch(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            cache.SetSnapshot("key", SNAPSHOT1);

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                var responses = new List<Response>();
                var discoveryRequest = new DiscoveryRequest
                {
                    Node = new Api.V2.Core.Node(),
                    TypeUrl = typeUrl
                };

                discoveryRequest.ResourceNames.AddRange(SNAPSHOT1.GetResources(typeUrl).Keys);

                var watch = cache.CreateWatch(
                    ads,
                    discoveryRequest,
                    EmptySet,
                    responses.Add);

                watch.Request.TypeUrl.Should().BeEquivalentTo(typeUrl);
                watch.Request.ResourceNames.Should().BeEquivalentTo(SNAPSHOT1.GetResources(typeUrl).Keys);

                AssertThatWatchReceivesSnapshot(watch, responses, SNAPSHOT1);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SuccessfullyWatchAllResourceTypesWithSetAfterWatch(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            var watches = Resources.TYPE_URLS
            .ToDictionary(k => k, typeUrl =>
            {
                var responses = new List<Response>();
                var discoveryRequest = new DiscoveryRequest
                {
                    Node = new Api.V2.Core.Node(),
                    TypeUrl = typeUrl
                };

                discoveryRequest.ResourceNames.AddRange(SNAPSHOT1.GetResources(typeUrl).Keys);

                var watch = cache.CreateWatch(
                    ads,
                    discoveryRequest,
                    EmptySet,
                    responses.Add);

                return new
                {
                    watch = watch,
                    responses = responses
                };
            });

            cache.SetSnapshot("key", SNAPSHOT1);

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                var value = watches.GetValueOrDefault(typeUrl);
                AssertThatWatchReceivesSnapshot(value.watch, value.responses, SNAPSHOT1);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SuccessfullyWatchAllResourceTypesWithSetBeforeWatchWithRequestVersion(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            cache.SetSnapshot("key", SNAPSHOT1);

            var watches = Resources.TYPE_URLS.ToDictionary(k => k,
            typeUrl =>
            {
                var responses = new List<Response>();
                var discoveryRequest = new DiscoveryRequest
                {
                    Node = new Api.V2.Core.Node(),
                    TypeUrl = typeUrl,
                    VersionInfo = SNAPSHOT1.GetVersion(typeUrl)
                };

                discoveryRequest.ResourceNames.AddRange(SNAPSHOT1.GetResources(typeUrl).Keys);

                var watch = cache.CreateWatch(
                    ads,
                    discoveryRequest,
                    SNAPSHOT2.GetResources(typeUrl).Keys.ToHashSet(),
                    responses.Add);

                return new
                {
                    watch = watch,
                    responses = responses
                };
            });

            // The request version matches the current snapshot version, so the watches shouldn't receive any responses.
            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                AssertThatWatchIsOpenWithNoResponses(watches[typeUrl].watch, watches[typeUrl].responses);
            }

            cache.SetSnapshot("key", SNAPSHOT2);

            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                AssertThatWatchReceivesSnapshot(watches[typeUrl].watch, watches[typeUrl].responses, SNAPSHOT2);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SuccessfullyWatchAllResourceTypesWithSetBeforeWatchWithSameRequestVersionNewResourceHints(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            cache.SetSnapshot("key", MULTIPLE_RESOURCES_SNAPSHOT2);

            // Set a watch for the current snapshot with the same version but with resource hints present
            // in the snapshot that the watch creator does not currently know about.
            //
            // Note how we're requesting the resources from MULTIPLE_RESOURCE_SNAPSHOT2 while claiming we
            // only know about the ones from SNAPSHOT2
            var watches = Resources.TYPE_URLS.ToDictionary(
                    typeUrl => typeUrl,
                    typeUrl =>
                    {
                        var responses = new List<Response>();
                        var discoveryRequest = new DiscoveryRequest
                        {
                            Node = new Api.V2.Core.Node(),
                            TypeUrl = typeUrl,
                            VersionInfo = MULTIPLE_RESOURCES_SNAPSHOT2.GetVersion(typeUrl)
                        };

                        discoveryRequest.ResourceNames.AddRange(MULTIPLE_RESOURCES_SNAPSHOT2.GetResources(typeUrl).Keys);

                        var watch = cache.CreateWatch(
                            ads,
                            discoveryRequest,
                            SNAPSHOT2.GetResources(typeUrl).Keys.ToHashSet(),
                            responses.Add);

                        return new
                        {
                            watch = watch,
                            responses = responses
                        };
                    });

            // The snapshot version matches for all resources, but for eds and cds there are new resources present
            // for the same version, so we expect the watches to trigger.
            AssertThatWatchReceivesSnapshot(
                watches[Resources.CLUSTER_TYPE_URL].watch,
                watches[Resources.CLUSTER_TYPE_URL].responses,
                MULTIPLE_RESOURCES_SNAPSHOT2);
            watches.Remove(Resources.CLUSTER_TYPE_URL);

            AssertThatWatchReceivesSnapshot(
                watches[Resources.ENDPOINT_TYPE_URL].watch,
                watches[Resources.ENDPOINT_TYPE_URL].responses,
                MULTIPLE_RESOURCES_SNAPSHOT2);
            watches.Remove(Resources.ENDPOINT_TYPE_URL);

            // Remaining watches should not trigger
            foreach (var w in watches.Values)
            {
                AssertThatWatchIsOpenWithNoResponses(w.watch, w.responses);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SuccessfullyWatchAllResourceTypesWithSetBeforeWatchWithSameRequestVersionNewResourceHintsNoChange(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            cache.SetSnapshot("key", SNAPSHOT2);

            // Set a watch for the current snapshot for the same version but with new resource hints not
            // present in the snapshot that the watch creator does not know about.
            //
            // Note that we're requesting the additional resources found in MULTIPLE_RESOURCE_SNAPSHOT2
            // while we only know about the resources found in SNAPSHOT2. Since SNAPSHOT2 is the current
            // snapshot, we have nothing to respond with for the new resources so we should not trigger
            // the watch.
            var watches = Resources.TYPE_URLS.ToDictionary(
                    typeUrl => typeUrl,
                    typeUrl =>
                    {
                        var responses = new List<Response>();
                        var discoveryRequest = new DiscoveryRequest
                        {
                            Node = new Api.V2.Core.Node(),
                            TypeUrl = typeUrl,
                            VersionInfo = SNAPSHOT2.GetVersion(typeUrl)
                        };

                        discoveryRequest.ResourceNames.AddRange(MULTIPLE_RESOURCES_SNAPSHOT2.GetResources(typeUrl).Keys);

                        var watch = cache.CreateWatch(
                            ads,
                            discoveryRequest,
                            SNAPSHOT2.GetResources(typeUrl).Keys.ToHashSet(),
                            responses.Add);

                        return new
                        {
                            watch = watch,
                            responses = responses
                        };
                    });

            // No watches should trigger since no new information will be returned
            foreach (var val in watches.Values)
            {
                AssertThatWatchIsOpenWithNoResponses(val.watch, val.responses);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetSnapshotWithVersionMatchingRequestShouldLeaveWatchOpenWithoutAdditionalResponse(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            cache.SetSnapshot("key", SNAPSHOT1);

            var watches = Resources.TYPE_URLS.ToDictionary(
                    typeUrl => typeUrl,
                    typeUrl =>
                    {
                        var responses = new List<Response>();
                        var discoveryRequest = new DiscoveryRequest
                        {
                            Node = new Api.V2.Core.Node(),
                            TypeUrl = typeUrl,
                            VersionInfo = SNAPSHOT1.GetVersion(typeUrl)
                        };

                        discoveryRequest.ResourceNames.AddRange(SNAPSHOT1.GetResources(typeUrl).Keys);

                        var watch = cache.CreateWatch(
                            ads,
                            discoveryRequest,
                            SNAPSHOT1.GetResources(typeUrl).Keys.ToHashSet(),
                            responses.Add);

                        return new
                        {
                            watch = watch,
                            responses = responses
                        };
                    });

            // The request version matches the current snapshot version, so the watches shouldn't receive any responses.
            foreach (string typeUrl in Resources.TYPE_URLS)
            {
                AssertThatWatchIsOpenWithNoResponses(watches[typeUrl].watch, watches[typeUrl].responses);
            }

            cache.SetSnapshot("key", SNAPSHOT1);

            // The request version still matches the current snapshot version, so the watches shouldn't receive any responses.
            foreach (var typeUrl in Resources.TYPE_URLS)
            {
                AssertThatWatchIsOpenWithNoResponses(watches[typeUrl].watch, watches[typeUrl].responses);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WatchesAreReleasedAfterCancel(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            var watches = Resources.TYPE_URLS.ToDictionary(
                    typeUrl => typeUrl,
                    typeUrl =>
                    {
                        var responses = new List<Response>();
                        var discoveryRequest = new DiscoveryRequest
                        {
                            Node = new Api.V2.Core.Node(),
                            TypeUrl = typeUrl,
                        };

                        discoveryRequest.ResourceNames.AddRange(SNAPSHOT1.GetResources(typeUrl).Keys);

                        var watch = cache.CreateWatch(
                            ads,
                            discoveryRequest,
                            EmptySet,
                            responses.Add);

                        return new
                        {
                            watch = watch,
                            responses = responses
                        };
                    });

            var statusInfo = cache.GetStatusInfo("key");

            statusInfo.NumWatches.Should().Be(watches.Count);

            watches.Values.ToList().ForEach(w => w.watch.Cancel());

            statusInfo.NumWatches.Should().Be(0);

            watches.Values.ToList().ForEach(w => w.watch.IsCancelled.Should().BeTrue());
        }

        [Fact]
        public void WatchIsLeftOpenIfNotRespondedImmediately()
        {
            var cache = new SimpleCache<string>(_ => "key");
            cache.SetSnapshot("key", new Snapshot(
                Enumerable.Empty<Cluster>(),
                Enumerable.Empty<ClusterLoadAssignment>(),
                Enumerable.Empty<Listener>(),
                Enumerable.Empty<RouteConfiguration>(),
                Enumerable.Empty<Secret>(),
                VERSION1));

            var responses = new List<Response>();
            var discoveryRequest = new DiscoveryRequest
            {
                Node = new Api.V2.Core.Node(),
                TypeUrl = Resources.ROUTE_TYPE_URL
            };
            discoveryRequest.ResourceNames.Add(ROUTE_NAME);

            var watch = cache.CreateWatch(
                true,
                discoveryRequest,
                new[] { ROUTE_NAME }.ToHashSet(),
                responses.Add);

            AssertThatWatchIsOpenWithNoResponses(watch, responses);
        }

        [Fact]
        public void GetSnapshot()
        {
            var cache = new SimpleCache<string>(_ => "key");
            cache.SetSnapshot("key", SNAPSHOT1);
            cache.GetSnapshot("key").Should().Be(SNAPSHOT1);
        }

        [Fact]
        public void ClearSnapshot()
        {
            var cache = new SimpleCache<string>(_ => "key");
            cache.SetSnapshot("key", SNAPSHOT1);
            cache.ClearSnapshot("key").Should().BeTrue();
            cache.GetSnapshot("key").Should().BeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ClearSnapshotWithWatches(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            cache.SetSnapshot("key", SNAPSHOT1);
            var discoveryRequest = new DiscoveryRequest
            {
                Node = new Api.V2.Core.Node(),
                TypeUrl = ""
            };

            var watch = cache.CreateWatch(
                ads,
                discoveryRequest,
                EmptySet,
                _ => { });

            // clearSnapshot should fail and the snapshot should be left untouched
            cache.ClearSnapshot("key").Should().BeFalse();
            cache.GetSnapshot("key").Should().Be(SNAPSHOT1);
            cache.GetStatusInfo("key").Should().NotBeNull();

            watch.Cancel();

            // now that the watch is gone we should be able to clear it
            cache.ClearSnapshot("key").Should().BeTrue();
            cache.GetSnapshot("key").Should().BeNull();
            cache.GetStatusInfo("key").Should().BeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Groups(bool ads)
        {
            var cache = new SimpleCache<string>(_ => "key");

            cache.Groups.Should().BeEmpty();
            var discoveryRequest = new DiscoveryRequest
            {
                Node = new Api.V2.Core.Node(),
                TypeUrl = ""
            };

            cache.CreateWatch(
                ads,
                discoveryRequest,
                EmptySet,
                r => { });

            cache.Groups.Should().BeEquivalentTo("key");
        }

        private static void AssertThatWatchIsOpenWithNoResponses(Watch watch, IEnumerable<Response> responses)
        {
            watch.IsCancelled.Should().BeFalse();
            responses.Should().BeEmpty();
        }

        private static void AssertThatWatchReceivesSnapshot(Watch watch, IEnumerable<Response> responses, Snapshot snapshot)
        {
            responses.Should().NotBeEmpty();

            Response response = responses.First();

            response.Should().NotBeNull();
            response.Version.Should().Be(snapshot.GetVersion(watch.Request.TypeUrl));
            // response.Resources.Select(new Message[0]))
            //     .containsExactlyElementsOf(snapshot.resources(watchAndTracker.watch.request().getTypeUrl()).values());
        }

    }
}
