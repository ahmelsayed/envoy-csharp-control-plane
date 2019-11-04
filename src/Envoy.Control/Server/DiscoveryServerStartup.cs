using System;
using System.Collections.Generic;
using System.Linq;
using Envoy.Control.Cache;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Envoy.Control.Server
{
    internal class DiscoveryServerStartup
    {
        private readonly DiscoveryServerConfig _configuration;
        internal static IConfigWatcher ConfigWatcher;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public DiscoveryServerStartup(IConfiguration configruation)
        {
            this._configuration = new DiscoveryServerConfig();
            configruation.Bind(this._configuration);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (DiscoveryServerStartup.ConfigWatcher != null)
            {
                services.AddSingleton<IConfigWatcher>(DiscoveryServerStartup.ConfigWatcher);
            }

            services.AddSingleton<IDiscoveryStreamHandler, DiscoveryStreamHandler>();
            services.AddGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.UseDiscoveryServerServices(new[]
                {
                    nameof(AggregatedDiscoveryService),
                    nameof(ClusterDiscoveryService),
                    nameof(EndpointDiscoveryService),
                    nameof(ListenerDiscoveryService),
                    nameof(RouteDiscoveryService),
                    nameof(SecretDiscoveryService)
                });

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }

    public static class EndpointRouteBuilderExtensions
    {
        public static void UseDiscoveryServerServices(this IEndpointRouteBuilder endpoints, IEnumerable<string> enabledServices)
        {
            if (enabledServices.Any(s => Is(s, nameof(AggregatedDiscoveryService))))
            {
                endpoints.MapGrpcService<AggregatedDiscoveryService>();
            }

            if (enabledServices.Any(s => Is(s, nameof(ClusterDiscoveryService))))
            {
                endpoints.MapGrpcService<ClusterDiscoveryService>();
            }

            if (enabledServices.Any(s => Is(s, nameof(EndpointDiscoveryService))))
            {
                endpoints.MapGrpcService<EndpointDiscoveryService>();
            }

            if (enabledServices.Any(s => Is(s, nameof(ListenerDiscoveryService))))
            {
                endpoints.MapGrpcService<ListenerDiscoveryService>();
            }

            if (enabledServices.Any(s => Is(s, nameof(RouteDiscoveryService))))
            {
                endpoints.MapGrpcService<RouteDiscoveryService>();
            }

            if (enabledServices.Any(s => Is(s, nameof(SecretDiscoveryService))))
            {
                endpoints.MapGrpcService<SecretDiscoveryService>();
            }

            bool Is(string a, string b)
                => a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }
    }
}