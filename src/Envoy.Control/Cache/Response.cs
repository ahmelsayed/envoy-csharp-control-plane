using System.Collections.Generic;
using Envoy.Api.V2;
using Google.Protobuf;

namespace Envoy.Control.Cache
{
    public class Response
    {
        public DiscoveryRequest Request { get; }
        public IEnumerable<IMessage> Resources { get; }
        public string Version { get; }

        public Response(DiscoveryRequest request, IEnumerable<IMessage> resources, string version)
        {
            this.Request = request;
            this.Resources = resources;
            this.Version = version;
        }
    }
}