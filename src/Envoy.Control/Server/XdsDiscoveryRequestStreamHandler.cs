using System;
using System.Collections.Generic;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Grpc.Core;

namespace Envoy.Control.Server
{
    internal class XdsDiscoveryRequestStreamHandler : BaseDiscoveryRequestStreamHandler
    {
        private volatile Watch watch;
        private volatile DiscoveryResponse latestResponse;
        // ackedResources is only used in the same thread so it need not be volatile
        private ISet<String> ackedResources = new HashSet<string>(0);
        public XdsDiscoveryRequestStreamHandler(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            string defaultTypeUrl,
            long streamId,
            IConfigWatcher configWatcher)
            : base(requestStream, responseStream, defaultTypeUrl, streamId, configWatcher)
        {
        }

        public override bool Ads => false;

        public override void Cancel()
        {
            if (this.watch != null)
            {
                this.watch.Cancel();
            }
        }

        public override void ComputeWatch(string typeUrl, Func<Watch> watchCreator)
        {
            this.Cancel();
            this.watch = watchCreator();
        }

        public override ISet<string> GetAckedResources(string typeUrl)
        {
            return this.ackedResources;
        }

        public override DiscoveryResponse GetLatestResponse(string typeUrl)
            => this.latestResponse;

        public override void SetAckedResources(string typeUrl, ISet<string> resources)
        {
            this.ackedResources = resources;
        }

        public override void SetLatestResponse(string typeUrl, DiscoveryResponse response)
        {
            this.latestResponse = response;
        }
    }
}