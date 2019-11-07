using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Envoy.Api.V2;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Envoy.Control.Cache.Tests
{
    public class WatchTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AdsReturnsGivenValue(bool ads)
        {
            var watch = new Watch(ads, new DiscoveryRequest(), r => { });
            watch.Ads.Should().Be(ads);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsCancelledTrueAfterCancel(bool ads)
        {
            var watch = new Watch(ads, new DiscoveryRequest(), r => { });

            watch.IsCancelled.Should().BeFalse();

            watch.Cancel();

            watch.IsCancelled.Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CancelWithStopCallsStop(bool ads)
        {
            int stopCount = 0;

            var watch = new Watch(ads, new DiscoveryRequest(), r => { });

            watch.SetStop(() => Interlocked.Increment(ref stopCount));

            watch.IsCancelled.Should().BeFalse();

            watch.Cancel();
            watch.Cancel();

            stopCount.Should().Be(1);

            watch.IsCancelled.Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResponseHandlerExecutedForResponsesUntilCancelled(bool ads)
        {
            var response1 = new Response(
                new DiscoveryRequest(),
                Enumerable.Empty<IMessage>(),
                Guid.NewGuid().ToString());

            var response2 = new Response(
                new DiscoveryRequest(),
                Enumerable.Empty<IMessage>(),
                Guid.NewGuid().ToString());

            var response3 = new Response(
                new DiscoveryRequest(),
                Enumerable.Empty<IMessage>(),
                Guid.NewGuid().ToString());

            var responses = new List<Response>();

            var watch = new Watch(ads, new DiscoveryRequest(), responses.Add);

            try
            {
                watch.Respond(response1);
                watch.Respond(response2);
            }
            catch (WatchCancelledException e)
            {
                Assert.True(false, "watch should not be cancelled " + e);
            }

            watch.Cancel();
            Action act = () => watch.Respond(response3);
            act.Should().Throw<WatchCancelledException>();

            responses.Should().BeEquivalentTo(response1, response2);
        }
    }
}