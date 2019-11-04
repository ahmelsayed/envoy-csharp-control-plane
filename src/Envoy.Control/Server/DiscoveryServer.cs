using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Control.Cache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Envoy.Control.Server
{
    public class DiscoveryServer
    {
        readonly IConfigWatcher _configWatcher;
        private readonly ISet<string> _enabledServices = new HashSet<string>();

        public DiscoveryServer(IConfigWatcher configWatcher)
        {
            if (configWatcher == null)
            {
                throw new ArgumentNullException($"{nameof(configWatcher)} cannot be null");
            }

            this._configWatcher = configWatcher;
            DiscoveryServerStartup.ConfigWatcher = configWatcher;
        }

        public DiscoveryServer UseAggregatedDiscoveryService()
        {
            Console.WriteLine("using service");
            this._enabledServices.Add(nameof(AggregatedDiscoveryService));
            return this;
        }

        public DiscoveryServer UseClusterDiscoveryService()
        {
            this._enabledServices.Add(nameof(ClusterDiscoveryService));
            return this;
        }

        public DiscoveryServer UseEndpointDiscoveryService()
        {
            this._enabledServices.Add(nameof(EndpointDiscoveryService));
            return this;
        }

        public DiscoveryServer UseListenerDiscoveryService()
        {
            this._enabledServices.Add(nameof(ListenerDiscoveryService));
            return this;
        }

        public DiscoveryServer UseRouteDiscoveryService()
        {
            this._enabledServices.Add(nameof(RouteDiscoveryService));
            return this;
        }

        public DiscoveryServer UseSecretDiscoveryService()
        {
            this._enabledServices.Add(nameof(SecretDiscoveryService));
            return this;
        }

        public async Task RunAsync(CancellationToken token = default)
        {
            var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("https://+:5000", "http://+:5001");
                webBuilder.UseStartup<DiscoveryServerStartup>();
            }).Build();
            await host.RunAsync(token);
        }
    }
}