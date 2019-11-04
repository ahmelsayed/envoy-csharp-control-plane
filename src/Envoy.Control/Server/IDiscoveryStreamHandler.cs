using System.Threading.Tasks;
using Envoy.Api.V2;
using Grpc.Core;

namespace Envoy.Control.Server
{
    public interface IDiscoveryStreamHandler
    {
        Task HandleXdsStreams(IAsyncStreamReader<DiscoveryRequest> requestStream, IServerStreamWriter<DiscoveryResponse> responseStream, string defaultTypeUrl);
        Task HandleAdsStreams(IAsyncStreamReader<DiscoveryRequest> requestStream, IServerStreamWriter<DiscoveryResponse> responseStream);
    }
}