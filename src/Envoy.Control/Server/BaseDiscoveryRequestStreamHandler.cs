using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Control.Cache;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Envoy.Control.Server
{
    internal abstract class BaseDiscoveryRequestStreamHandler
    {
        private static readonly ILogger Logger = DiscoveryServerLoggerFactory.CreateLogger(nameof(BaseDiscoveryRequestStreamHandler));
        private readonly string _defaultTypeUrl;
        private readonly IAsyncStreamReader<DiscoveryRequest> _requestStream;
        private readonly IAsyncStreamWriter<DiscoveryResponse> _responseStream;
        private readonly long _streamId;
        private readonly IConfigWatcher _configWatcher;
        private readonly IEnumerable<IDiscoveryServerCallbacks> _callbacks;
        private volatile bool _isClosing = false;
        private long _streamNonce;
        private readonly AsyncLock _lock = new AsyncLock();

        public BaseDiscoveryRequestStreamHandler(
            IAsyncStreamReader<DiscoveryRequest> requestStream,
            IServerStreamWriter<DiscoveryResponse> responseStream,
            string defaultTypeUrl,
            long streamId,
            IConfigWatcher configWatcher,
            IEnumerable<IDiscoveryServerCallbacks> callbacks)
        {
            _defaultTypeUrl = defaultTypeUrl;
            _requestStream = requestStream;
            _responseStream = responseStream;
            _streamId = streamId;
            _configWatcher = configWatcher;
            _callbacks = callbacks;
        }

        public async Task RunAsync(CancellationToken token = default)
        {
            try
            {
                await foreach (var request in _requestStream.ReadAllAsync(token))
                {
                    var requestTypeUrl = string.IsNullOrEmpty(request.TypeUrl)
                        ? _defaultTypeUrl
                        : request.TypeUrl;

                    var nonce = request.ResponseNonce;


                    Logger.LogDebug("[{0}] request {1}[{2}] with nonce {3} from version {4}",
                        _streamId,
                        requestTypeUrl,
                        string.Join(", ", request.ResourceNames),
                        nonce,
                        request.VersionInfo);

                    _callbacks.ForEach(cb => cb.OnStreamRequest(_streamId, request));

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

                        this.ComputeWatch(requestTypeUrl, () => _configWatcher.CreateWatch(
                            this.Ads,
                            request,
                            this.GetAckedResources(requestTypeUrl),
                            async r => await this.Send(r, requestTypeUrl)));
                    }
                }
                _callbacks.ForEach(cb => cb.OnStreamClose(_streamId, _defaultTypeUrl));
                _isClosing = true;
                this.Cancel();
            }
            catch (Exception e)
            {
                _callbacks.ForEach(cb => cb.OnStreamCloseWithError(_streamId, _defaultTypeUrl, e));
                _isClosing = true;
                this.Cancel();
                // log
            }
        }

        private async Task Send(Response response, string typeUrl)
        {
            try
            {
                var nonce = Interlocked.Increment(ref _streamNonce);

                var resources = response.Resources.Select(Any.Pack);
                var discoveryResponse = new DiscoveryResponse
                {
                    VersionInfo = response.Version,
                    TypeUrl = typeUrl,
                    Nonce = nonce.ToString()
                };
                discoveryResponse.Resources.Add(resources);

                Logger.LogDebug("[{0}] response {1} with nonce {2} version {3}", _streamId, typeUrl, nonce, response.Version);

                _callbacks.ForEach(cb => cb.OnStreamResponse(_streamId, response.Request, discoveryResponse));

                // Store the latest response *before* we send the response. This ensures that by the time the request
                // is processed the map is guaranteed to be updated. Doing it afterwards leads to a race conditions
                // which may see the incoming request arrive before the map is updated, failing the nonce check erroneously.
                this.SetLatestResponse(typeUrl, discoveryResponse);
                using (await _lock.LockAsync())
                {
                    if (!_isClosing)
                    {
                        await _responseStream.WriteAsync(discoveryResponse);
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

        public abstract DiscoveryResponse? GetLatestResponse(string typeUrl);

        public abstract void SetLatestResponse(string typeUrl, DiscoveryResponse response);

        public abstract ISet<string>? GetAckedResources(string typeUrl);

        public abstract void SetAckedResources(string typeUrl, ISet<string> resources);

        public abstract void ComputeWatch(string typeUrl, Func<Watch> watchCreator);
    }
}