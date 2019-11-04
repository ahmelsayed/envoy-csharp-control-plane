using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Grpc.Core;

namespace Envoy.Control.Server
{
    public class ListenerDiscoveryService : Api.V2.ListenerDiscoveryService.ListenerDiscoveryServiceBase
    {
        private readonly IDiscoveryStreamHandler _streamHandler;

        public ListenerDiscoveryService(IDiscoveryStreamHandler streamHandler)
        {
            this._streamHandler = streamHandler;
        }

        public override async Task StreamListeners(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            ServerCallContext context)
        {
            await this._streamHandler.HandleXdsStreams(requestStream, responseStream, Resources.LISTENER_TYPE_URL);
        }
    }
}