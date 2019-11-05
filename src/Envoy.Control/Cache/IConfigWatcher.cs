using System;
using System.Collections.Generic;
using Envoy.Api.V2;

namespace Envoy.Control.Cache
{
    public interface IConfigWatcher
    {
        Watch CreateWatch(bool ads, DiscoveryRequest request, ISet<string>? knownResourceNames, Action<Response> responseCallback);
    }
}