using Microsoft.Extensions.Logging;

namespace Envoy.Control.Server
{
    public class DiscoveryLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
            => DiscoveryServerLoggerFactory.CreateLogger(categoryName);

        public void Dispose()
        {}
    }
}