using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Grpc.Core;

namespace Envoy.Control.Server
{
    internal class AdsDiscoveryRequestStreamHandler : BaseDiscoveryRequestStreamHandler
    {
        private readonly ConcurrentDictionary<string, Watch> _watches
            = new ConcurrentDictionary<string, Watch>();
        private readonly ConcurrentDictionary<string, DiscoveryResponse> _latestResponse
            = new ConcurrentDictionary<string, DiscoveryResponse>();
        private readonly ConcurrentDictionary<string, ISet<string>> _ackedResources
            = new ConcurrentDictionary<string, ISet<string>>();

        private readonly ISet<string> emptyHashSet = new HashSet<string>(0);

        public AdsDiscoveryRequestStreamHandler(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            long streamId,
            IConfigWatcher configWatcher,
            IEnumerable<IDiscoveryServerCallbacks> callbacks)
            : base(requestStream, responseStream, string.Empty, streamId, configWatcher, callbacks)
        {
        }

        public override bool Ads => true;

        public override void Cancel()
        {
            foreach (var watch in this._watches.Values)
                watch.Cancel();
        }

        public override void ComputeWatch(string typeUrl, Func<Watch> watchCreator)
        {
            this._watches.AddOrUpdate(typeUrl, _ => watchCreator(), (_, w) =>
            {
                w.Cancel();
                return watchCreator();
            });
        }

        public override ISet<string>? GetAckedResources(string typeUrl)
            => this._ackedResources.GetValueOrDefault(typeUrl, emptyHashSet);

        public override DiscoveryResponse? GetLatestResponse(string typeUrl)
            => this._latestResponse.GetValueOrDefault(typeUrl, null);

        public override void SetAckedResources(string typeUrl, ISet<string> resources)
            => this._ackedResources.AddOrUpdate(typeUrl, _ => resources, (_, __) => resources);

        public override void SetLatestResponse(string typeUrl, DiscoveryResponse response)
            => this._latestResponse.AddOrUpdate(typeUrl, _ => response, (_, __) => response);
    }
}