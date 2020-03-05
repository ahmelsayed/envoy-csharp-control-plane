using System;
using Envoy.Api.V2.Auth;
using Envoy.Control.Cache;
using Envoy.Control.Server;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace Envoy.Control.Runner
{
    class Program
    {
        public static void Main(string[] args)
        {
            // First create a cache object to hold the envoy configuration cache
            // the cache requires a node hash callback. It'll give you an envoy Node, and expects an Id.
            // this hard codes the id to "key", but it will change later on.
            var cache = new SimpleCache<string>(_ => "key");

            cache.SetSnapshot("key", Data.CreateBaseSnapshot());

            // Create a DiscoveryServer and give it the cache object
            // Select the type of server. https://www.envoyproxy.io/docs/envoy/latest/api-docs/xds_protocol
            var discoveryServer = DiscoveryServerBuilder
                .CreateFor(cache, 6000)
                .ConfigureDiscoveryService<ClusterDiscoveryService>()
                .ConfigureDiscoveryService<EndpointDiscoveryService>()
                .ConfigureDiscoveryService<RouteDiscoveryService>()
                .ConfigureDiscoveryService<ListenerDiscoveryService>()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            var restServer = WebHost.CreateDefaultBuilder(args)
                .Configure(_ => _.Run(async ctx =>
                {
                    var endpointRequest = await deserialize<AppEndpoint>(ctx.Response.Body);
                    if (valid(endpointRequest)) {
                        var result = Reconcile(cache, endpointRequest);
                        await ctx.Response.WriteAsync(result);
                    } else {
                        ctx.Response.StatusCode = 400;
                        await ctx.Response.WriteAsync("Invalid");
                    }


                    bool valid(AppEndpoint e)
                        => !string.IsNullOrEmpty(e.Ip) &&
                           !string.IsNullOrEmpty(e.Domain) &&
                           !string.IsNullOrEmpty(e.Name) &&
                           e.Port != 0;

                    async Task<T> deserialize<T>(Stream stream)
                    {
                        var str = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
                        return JsonConvert.DeserializeObject<T>(str);
                    }
                }))
                .Build();

            discoveryServer.RunAsync();
            restServer.Run();
        }

        public static string Reconcile(SimpleCache<string> cache, AppEndpoint appEndpoint)
        {
            var snapshot = cache.GetSnapshot("key");
            if (snapshot == null)
            {
                snapshot = Data.CreateBaseSnapshot();
            }

            var cluster = Data.CreateCluster(appEndpoint.Name!);
            var clusterLoadAssignment = Data.CreateClusterLoadAssignment(cluster.Name, new[] { (appEndpoint.Ip!, appEndpoint.Port) });
            var virtualHost = Data.CreateVirtualHost(appEndpoint.Name!, appEndpoint.Domain!, appEndpoint.Name!);
            var route = Data.UpdateRoutes(virtualHost, snapshot.Routes.Resources.Values.FirstOrDefault());
            var newSnapshot = new Snapshot(
                snapshot.Clusters.Resources.Values.AddOrUpdateInPlace(cluster, (a,b) => a.Name == b.Name),
                snapshot.Endpoints.Resources.Values.AddOrUpdateInPlace(clusterLoadAssignment, (a,b) => a.ClusterName == b.ClusterName),
                new[] { Data.CreateHTTPListener() },
                new[] { route },
                Enumerable.Empty<Secret>(),
                Guid.NewGuid().ToString()
            );

            newSnapshot.EnsureConsistent();
            cache.SetSnapshot("key", newSnapshot);
            return "Success";
        }
    }

    class AppEndpoint
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "ip")]
        public string? Ip { get; set; }

        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }

        [JsonProperty(PropertyName = "domain")]
        public string? Domain { get; set; }
    }
}
