using System;
using System.Linq;
using System.Threading.Tasks;
using Envoy.Api.V2;
using Envoy.Api.V2.Auth;
using Envoy.Api.V2.Core;
using Envoy.Api.V2.ListenerNS;
using Envoy.Api.V2.Route;
using Envoy.Config.Filter.Network.HttpConnectionManager.V2;
using Envoy.Control.Cache;
using Envoy.Control.Server;
using Google.Protobuf.WellKnownTypes;
using static Envoy.Api.V2.Cluster.Types;

namespace Envoy.Control.Runner
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            const string envoyNodeGroup = "key";

            var cache = new SimpleCache<string>(_ => envoyNodeGroup);
            cache.SetSnapshot(envoyNodeGroup, Data.BBCSnapshot);

            var discoveryServer = new DiscoveryServer(cache);
            discoveryServer.UseAggregatedDiscoveryService();
            var task = discoveryServer.RunAsync();

            Console.WriteLine("First configuration forwarding localhost:20000 to www.bbc.com");
            Console.WriteLine("Press enter to continue");
            Console.ReadKey();

            cache.SetSnapshot(envoyNodeGroup, Data.CNNSnapshot);
            Console.WriteLine("Second configuration forwarding localhost:20000 to www.cnn.org");
            Console.WriteLine("Press CTRL-C to exit");
            await task;
        }
    }
}
