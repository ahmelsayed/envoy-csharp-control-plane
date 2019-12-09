# csharp-control-plane

A C# control plane for [Envoy Proxy](https://envoyproxy.io) based on the [java-control-plane](https://github.com/envoyproxy/java-control-plane)

```csharp
public static async Task MainAsync(string[] args)
{
    // First create a cache object to hold the envoy configuration cache
    // the cache requires a node hash callback. It'll give you an envoy Node, and expects an Id.
    // this hard codes the id to "key", but it will change later on.
    var cache = new SimpleCache<string>(_ => "key");

    cache.SetSnapshot("key", CreateSnapshot());

    // Create a DiscoveryServer and give it the cache object
    // Select the type of server. https://www.envoyproxy.io/docs/envoy/latest/api-docs/xds_protocol
    // This is using the ADS server
    var discoveryServer = DiscoveryServerBuilder
        .CreateFor(cache)
        .ConfigureDiscoveryService<AggregatedDiscoveryService>()
        .ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .Build();

    // Run the server
    var cts = new CancellationTokenSource();
    await discoveryServer.RunAsync(cts.Token);
}
```