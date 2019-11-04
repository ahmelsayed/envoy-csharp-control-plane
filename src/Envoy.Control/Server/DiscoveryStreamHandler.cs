using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, DiscoveryResponse> _latestResponse = new ConcurrentDictionary<string, DiscoveryResponse>();

        public DiscoveryStreamHandler(IConfigWatcher configWatcher)
        {
            this._configWatcher = configWatcher;
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
            var streamId = Interlocked.Increment(ref this._streamCount);

            var streamHandler = ads
                ? new AdsDiscoveryRequestStreamHandler(requestStream, responseStream, streamId, this._configWatcher)
                : new XdsDiscoveryRequestStreamHandler(requestStream, responseStream, defaultTypeUrl, streamId, this._configWatcher) as BaseDiscoveryRequestStreamHandler;

            await streamHandler.RunAsync();
        }
    }
}