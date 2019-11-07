using System;
using Envoy.Api.V2;

namespace Envoy.Control.Server
{
    /**
 * {@code DiscoveryServerCallbacks} defines the callbacks that are exposed by the {@link DiscoveryServer}. The callbacks
 * give consumers the opportunity to add their own application-specific logs/metrics based
 */
    public interface IDiscoveryServerCallbacks
    {

        /**
         * {@code onStreamClose} is called just before the bi-directional gRPC stream is closed successfully for an envoy
         * instance.
         *
         * @param streamId an ID for this stream that is only unique to this discovery server instance
         * @param typeUrl the resource type of the stream, or {@link DiscoveryServer#ANY_TYPE_URL} for ADS
         */
        void OnStreamClose(long streamId, string typeUrl)
        {
        }

        /**
         * {@code onStreamCloseWithError} is called just before the bi-directional gRPC stream is closed for an envoy instance
         * due to some error that has occurred.
         *
         * @param streamId an ID for this stream that is only unique to this discovery server instance
         * @param typeUrl the resource type of the stream, or {@link DiscoveryServer#ANY_TYPE_URL} for ADS
         * @param error the error that caused the stream to close
         */
        void OnStreamCloseWithError(long streamId, string typeUrl, Exception error)
        {
        }

        /**
         * {@code onStreamOpen} is called when the bi-directional gRPC stream is opened for an envoy instance, before the
         * initial {@link DiscoveryRequest} is processed.
         *
         * @param streamId an ID for this stream that is only unique to this discovery server instance
         * @param typeUrl the resource type of the stream, or {@link DiscoveryServer#ANY_TYPE_URL} for ADS
         */
        void OnStreamOpen(long streamId, string typeUrl)
        {
        }

        /**
         * {@code onStreamRequest} is called for each {@link DiscoveryRequest} that is received on the stream.
         *
         * @param streamId an ID for this stream that is only unique to this discovery server instance
         * @param request the discovery request sent by the envoy instance
         *
         * @throws RequestException optionally can throw {@link RequestException} with custom status. That status
         *     will be returned to the client and the stream will be closed with error.
         */
        void OnStreamRequest(long streamId, DiscoveryRequest request)
        {
        }

        /**
         * {@code onStreamResponse} is called just before each {@link DiscoveryResponse} that is sent on the stream.
         *
         * @param streamId an ID for this stream that is only unique to this discovery server instance
         * @param request the discovery request sent by the envoy instance
         * @param response the discovery response sent by the discovery server
         */
        void OnStreamResponse(long streamId, DiscoveryRequest request, DiscoveryResponse response)
        {
        }
    }
}