using System;
using System.Threading;
using Envoy.Control.Cache;
using Envoy.Control.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Envoy.Control.Runner
{
    class Program
    {
        public static void Main(string[] args)
        {
            // First create a cache object to hold the envoy configuration cache
            // the cache requires a "hash" callback. It'll give you an envoy Node, and expects an Id.
            // this hard codes the id to "key", but it will change later on.
            var cache = new SimpleCache<string>(_ => "key");

            // set a config snapshot for nodes with id hash: "key"
            // You can see what the config snapshot is like in the Data folder
            // This particular snapshot tells envoy to listen on localhost:20000
            // and forward all traffic to www.bbc.com
            var cluster1 = Data.CreateCluster("app1", new[] { ("localhost", 8080) });
            var listener1 = Data.CreateListener("listener1", "0.0.0.0", 2020, cluster1.Name);

            var cluster2 = Data.CreateCluster("app2", new[] { ("localhost", 9090) });
            var listener2 = Data.CreateListener("listener2", "0.0.0.0", 2020, cluster2.Name);

            cache.SetSnapshot("key", Data.CreateSnapshot(cluster1, listener1));

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
            discoveryServer.RunAsync(cts.Token);

            int count = 0;
            var logger = DiscoveryServerLoggerFactory.CreateLogger("main");
            while (true)
            {
                logger.LogDebug("Press any key");
                Console.ReadKey();
                // Update the config to another config snapshot.
                // This one tells envoy to listen on localhost:20000 and forward to www.cnn.com
                if (count % 2 == 0)
                {
                    cache.SetSnapshot("key", Data.CreateSnapshot(cluster2, listener2));
                }
                else
                {
                    cache.SetSnapshot("key", Data.CreateSnapshot(cluster1, listener1));
                }
                count++;
            }
        }
    }
}
