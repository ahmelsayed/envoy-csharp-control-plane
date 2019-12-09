using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Grpc.Core;

namespace Envoy.Control.Server
{
    public class RouteDiscoveryService : Api.V2.RouteDiscoveryService.RouteDiscoveryServiceBase
    {
        private readonly IDiscoveryStreamHandler _streamHandler;

        public RouteDiscoveryService(IDiscoveryStreamHandler streamHandler)
        {
            _streamHandler = streamHandler;
        }

        public override async Task StreamRoutes(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            ServerCallContext context)
        {
            await _streamHandler.HandleXdsStreams(requestStream, responseStream, Resources.ROUTE_TYPE_URL);
        }
    }
}