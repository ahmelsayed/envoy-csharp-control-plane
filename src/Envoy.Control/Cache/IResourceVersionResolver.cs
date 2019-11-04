using System.Collections.Generic;
using System.Linq;

namespace Envoy.Control.Cache
{
    public interface IResourceVersionResolver
    {
        string Version(IEnumerable<string> resourceNames);
        string Version() => Version(Enumerable.Empty<string>());
    }
}