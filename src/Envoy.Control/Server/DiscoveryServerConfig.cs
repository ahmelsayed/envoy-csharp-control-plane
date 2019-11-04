using System.Collections.Generic;
using System.Linq;

namespace Envoy.Control.Server
{
    public class DiscoveryServerConfig
    {
        public IEnumerable<string> EnabledServices { get; set; } = Enumerable.Empty<string>();
    }
}