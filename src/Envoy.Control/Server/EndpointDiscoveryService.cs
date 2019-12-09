using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Grpc.Core;

namespace Envoy.Control.Server
{
    public class EndpointDiscoveryService : Api.V2.EndpointDiscoveryService.EndpointDiscoveryServiceBase
    {
        private readonly IDiscoveryStreamHandler _streamHandler;

        public EndpointDiscoveryService(IDiscoveryStreamHandler streamHandler)
        {
            _streamHandler = streamHandler;
        }

        public override async Task StreamEndpoints(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            ServerCallContext context)
        {
            await _streamHandler.HandleXdsStreams(requestStream, responseStream, Resources.ENDPOINT_TYPE_URL);
        }
    }
}