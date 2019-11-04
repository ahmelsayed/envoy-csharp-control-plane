using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Envoy.Service.Discovery.V2;
using Grpc.Core;

namespace Envoy.Control.Server
{
    internal class SecretDiscoveryService : Service.Discovery.V2.SecretDiscoveryService.SecretDiscoveryServiceBase
    {
        private readonly IDiscoveryStreamHandler _streamHandler;

        public SecretDiscoveryService(IDiscoveryStreamHandler streamHandler)
        {
            this._streamHandler = streamHandler;
        }

        public override async Task StreamSecrets(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            ServerCallContext context)
        {
            await this._streamHandler.HandleXdsStreams(requestStream, responseStream, Resources.SECRET_TYPE_URL);
        }
    }
}