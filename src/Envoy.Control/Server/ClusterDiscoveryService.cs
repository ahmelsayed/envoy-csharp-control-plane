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
            await foreach (var request in requestStream.ReadAllAsync())
            {
                System.Console.WriteLine(request.TypeUrl);
                var discoveryResponse = new DiscoveryResponse
                {
                    VersionInfo = "1",
                    TypeUrl = request.TypeUrl,
                    Nonce = Interlocked.Increment(ref this.ticket).ToString()
                };
                discoveryResponse.Resources.Add(Any.Pack(new Cluster
                {
                    Name = "cluster" + this.ticket,
                    ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds(5)),
                    // Type = DiscoveryType.Static,
                    // DnsLookupFamily = DnsLookupFamily.V4Only,
                    // LbPolicy = LbPolicy.RoundRobin,
                    // TlsContext = new UpstreamTlsContext
                    // {
                    //     Sni = "www.google.com"
                    // }
                }));
                await responseStream.WriteAsync(discoveryResponse);
            }
            // await this._streamHandler.HandleXdsStreams(requestStream, responseStream, Resources.CLUSTER_TYPE_URL);
        }
    }
}