using System;
using System.Linq;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Api.V2.Core;
using Envoy.Control.Cache;
using FluentAssertions;
using Xunit;

namespace Envoy.Control.Cache.Tests
{
    public class CacheStatusInfoTests
    {
        static Random random = new Random();
        public void NodeGroupReturnsExpectedGroup()
        {
            var node = new Node
            {
                Id = Guid.NewGuid().ToString()
            };

            var info = new CacheStatusInfo<Node>(node);

            info.NodeGroup.Should().BeSameAs(node);
        }

        public void LastWatchRequestTimeReturns0IfNotSet()
        {
            var info = new CacheStatusInfo<Node>(new Node());

            info.LastWatchRequestTime.Should().Be(0);
        }

        public void LastWatchRequestTimeReturnsExpectedValueIfSet()
        {
            long lastWatchRequestTime = random.Next(10000, 50000);

            var info = new CacheStatusInfo<Node>(new Node());

            info.SetLastWatchRequestTime(lastWatchRequestTime);

            info.LastWatchRequestTime.Should().Be(lastWatchRequestTime);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NumWatchesReturnsExpectedSize(bool ads)
        {
            long watchId1 = random.Next(10000, 50000);
            long watchId2 = random.Next(50000, 100000);

            var info = new CacheStatusInfo<Node>(new Node());

            info.NumWatches.Should().Be(0);

            info.SetWatch(watchId1, new Watch(ads, new DiscoveryRequest(), r => { }));

            info.NumWatches.Should().Be(1);
            info.WatchIds.Should().BeEquivalentTo(watchId1);

            info.SetWatch(watchId2, new Watch(ads, new DiscoveryRequest(), r => { }));

            info.NumWatches.Should().Be(2);
            info.WatchIds.Should().BeEquivalentTo(watchId1, watchId2);

            info.RemoveWatch(watchId1);

            info.NumWatches.Should().Be(1);
            info.WatchIds.Should().BeEquivalentTo(watchId2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WatchesRemoveIfRemovesExpectedWatches(bool ads)
        {
            long watchId1 = random.Next(10000, 50000);
            long watchId2 = random.Next(50000, 100000);

            var info = new CacheStatusInfo<Node>(new Node());

            info.SetWatch(watchId1, new Watch(ads, new DiscoveryRequest(), r => { }));
            info.SetWatch(watchId2, new Watch(ads, new DiscoveryRequest(), r => { }));

            info.NumWatches.Should().Be(2);
            info.WatchIds.Should().BeEquivalentTo(watchId1, watchId2);

            info.WatchesRemoveIf((watchId, watch) => watchId == watchId1);

            info.NumWatches.Should().Be(1);
            info.WatchIds.Should().BeEquivalentTo(watchId2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConcurrentSetWatchAndRemove(bool ads)
        {
            var watchCount = 500;

            var info = new CacheStatusInfo<Node>(new Node());

            var watchIds = Enumerable.Range(0, watchCount).ToList();

            Parallel.ForEach(watchIds, watchId =>
            {
                var watch = new Watch(ads, new DiscoveryRequest(), r => { });
                info.SetWatch(watchId, watch);
            });

            info.WatchIds.Should().BeEquivalentTo(watchIds.ToArray());
            info.NumWatches.Should().Be(watchIds.Count);

            Parallel.ForEach(watchIds, id => info.RemoveWatch(id));

            info.WatchIds.Should().BeEmpty();
            info.NumWatches.Should().Be(0);
        }
    }
}