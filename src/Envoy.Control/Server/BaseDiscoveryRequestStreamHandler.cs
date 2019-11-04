using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Envoy.Control.Server
{
    internal abstract class BaseDiscoveryRequestStreamHandler
    {
        private readonly string _defaultTypeUrl;
        private readonly IAsyncStreamReader<DiscoveryRequest> _requestStream;
        private readonly IAsyncStreamWriter<DiscoveryResponse> _responseStream;
        private readonly long _streamId;
        private readonly IConfigWatcher _configWatcher;
        private volatile bool _isClosing = false;
        private long streamNonce;
        private readonly AsyncLock _lock = new AsyncLock();

        public BaseDiscoveryRequestStreamHandler(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            string defaultTypeUrl,
            long streamId,
            IConfigWatcher configWatcher)
        {
            this._defaultTypeUrl = defaultTypeUrl;
            this._requestStream = requestStream;
            this._responseStream = responseStream;
            this._streamId = streamId;
            this._configWatcher = configWatcher;
        }

        public async Task RunAsync(CancellationToken token = default)
        {
            try
            {
                await foreach (var request in this._requestStream.ReadAllAsync())
                {
                    var requestTypeUrl = string.IsNullOrEmpty(request.TypeUrl)
                        ? this._defaultTypeUrl
                        : request.TypeUrl;

                    var nonce = request.ResponseNonce;

                    // if (LOGGER.isDebugEnabled())
                    // {
                    //     LOGGER.debug("[{}] request {}[{}] with nonce {} from version {}",
                    //         streamId,
                    //         requestTypeUrl,
                    //         String.join(", ", request.getResourceNamesList()),
                    //         nonce,
                    //         request.getVersionInfo());
                    // }

                    var latestResponse = this.GetLatestResponse(requestTypeUrl);
                    var resourceNonce = latestResponse == null ? null : latestResponse.Nonce;

                    if (string.IsNullOrEmpty(resourceNonce) || resourceNonce.Equals(nonce))
                    {
                        if (request.ErrorDetail == null && latestResponse != null)
                        {
                            var ackedResourcesForType = latestResponse.Resources
                                .Select(Resources.GetResourceName)
                                .ToHashSet();

                            this.SetAckedResources(requestTypeUrl, ackedResourcesForType);
                        }

                        this.ComputeWatch(requestTypeUrl, () => this._configWatcher.CreateWatch(
                            this.Ads,
                            request,
                            this.GetAckedResources(requestTypeUrl),
                            async r => await this.Send(r, requestTypeUrl)));
                    }
                }
                this._isClosing = true;
                this.Cancel();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                this._isClosing = true;
                this.Cancel();
                // log
            }
        }

        private async Task Send(Response response, string typeUrl)
        {
            try
            {
                var nonce = Interlocked.Increment(ref this.streamNonce);

                var resources = response.Resources.Select(Any.Pack);
                // var resource = Any.Pack(response.Resources.First());
                var discoveryResponse = new DiscoveryResponse
                {
                    VersionInfo = response.Version,
                    TypeUrl = typeUrl,
                    Nonce = nonce.ToString()
                };
                discoveryResponse.Resources.Add(resources);

                // LOGGER.debug("[{}] response {} with nonce {} version {}", streamId, typeUrl, nonce, response.version());

                // discoverySever.callbacks.forEach(cb->cb.onStreamResponse(streamId, response.request(), discoveryResponse));

                // Store the latest response *before* we send the response. This ensures that by the time the request
                // is processed the map is guaranteed to be updated. Doing it afterwards leads to a race conditions
                // which may see the incoming request arrive before the map is updated, failing the nonce check erroneously.
                this.SetLatestResponse(typeUrl, discoveryResponse);
                using (await this._lock.LockAsync())
                {
                    if (!this._isClosing)
                    {
                        await this._responseStream.WriteAsync(discoveryResponse);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public abstract void Cancel();

        public abstract bool Ads { get; }

        public abstract DiscoveryResponse GetLatestResponse(string typeUrl);

        public abstract void SetLatestResponse(string typeUrl, DiscoveryResponse response);

        public abstract ISet<string> GetAckedResources(string typeUrl);

        public abstract void SetAckedResources(string typeUrl, ISet<string> resources);

        public abstract void ComputeWatch(string typeUrl, Func<Watch> watchCreator);
    }
}