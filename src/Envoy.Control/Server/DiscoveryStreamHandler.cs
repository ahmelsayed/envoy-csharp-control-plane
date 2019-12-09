using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Grpc.Core;

namespace Envoy.Control.Server
{
    public class DiscoveryStreamHandler : IDiscoveryStreamHandler
    {
        private long _streamCount = 0;
        private readonly IConfigWatcher _configWatcher;
        private readonly IImmutableList<IDiscoveryServerCallbacks> _callbacks;
        private readonly ConcurrentDictionary<string, DiscoveryResponse> _latestResponse = new ConcurrentDictionary<string, DiscoveryResponse>();

        public DiscoveryStreamHandler(IConfigWatcher configWatcher, IImmutableList<IDiscoveryServerCallbacks> callbacks)
        {
            _configWatcher = configWatcher;
            _callbacks = callbacks;
        }

        public Task HandleXdsStreams(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            string defaultTypeUrl)
            => HandleStream(requestStream, responseStream, false, defaultTypeUrl);

        public Task HandleAdsStreams(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream)
            => HandleStream(requestStream, responseStream, true, string.Empty);

        private async Task HandleStream(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            bool ads,
            string defaultTypeUrl)
        {
            var streamId = Interlocked.Increment(ref _streamCount);

            _callbacks.ForEach(cb => cb.OnStreamOpen(streamId, defaultTypeUrl));

            var streamHandler = ads
                ? new AdsDiscoveryRequestStreamHandler(requestStream, responseStream, streamId, _configWatcher, _callbacks)
                : new XdsDiscoveryRequestStreamHandler(requestStream, responseStream, defaultTypeUrl, streamId, _configWatcher, _callbacks) as BaseDiscoveryRequestStreamHandler;

            await streamHandler.RunAsync();
        }
    }
}