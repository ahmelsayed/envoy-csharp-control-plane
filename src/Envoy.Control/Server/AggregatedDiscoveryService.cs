using System.Threading.Tasks;
using Envoy.Api.V2;
using Grpc.Core;

namespace Envoy.Control.Server
{
    public class AggregatedDiscoveryService : Service.Discovery.V2.AggregatedDiscoveryService.AggregatedDiscoveryServiceBase
    {
        private readonly IDiscoveryStreamHandler _streamHandler;

        public AggregatedDiscoveryService(IDiscoveryStreamHandler streamHandler)
        {
            this._streamHandler = streamHandler;
        }

        public override async Task StreamAggregatedResources(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            ServerCallContext context)
        {
            await this._streamHandler.HandleAdsStreams(requestStream, responseStream);
        }
    }
}