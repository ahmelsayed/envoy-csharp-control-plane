using Microsoft.Extensions.Logging;

namespace Envoy.Control
{
    public static class DiscoveryServerLoggerFactory
    {
        private static ILoggerFactory _factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        public static void SetLoggerFactory(ILoggerFactory factory)
        {
            _factory.Dispose();
            _factory = factory;
        }

        public static ILogger CreateLogger(string categoryName)
            => _factory.CreateLogger(categoryName);
    }
}