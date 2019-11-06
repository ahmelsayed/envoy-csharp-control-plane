using System;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Control.Cache;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using static Envoy.Api.V2.Cluster.Types;

namespace Envoy.Control.Server
{
    public class ClusterDiscoveryService : Api.V2.ClusterDiscoveryService.ClusterDiscoveryServiceBase
    {
        private readonly IDiscoveryStreamHandler _streamHandler;

        public ClusterDiscoveryService(IDiscoveryStreamHandler streamHandler)
        {
            this._streamHandler = streamHandler;
        }

        long ticket = 0;
        public override async Task StreamClusters(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            ServerCallContext context)
        {
            await this._streamHandler.HandleXdsStreams(requestStream, responseStream, Resources.CLUSTER_TYPE_URL);
        }
    }
}