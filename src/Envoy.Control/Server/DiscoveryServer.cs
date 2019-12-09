using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Envoy.Control.Cache;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Envoy.Control.Server
{
    public class DiscoveryServerBuilder
    {
        private const int DefaultPort = 5000;
        readonly IConfigWatcher _configWatcher;
        private readonly int _port;
        private readonly IImmutableList<IDiscoveryServerCallbacks> _callbacks;
        private IHost? _host = null;
        private Action<ILoggingBuilder>? _loggingAction;
        private readonly object _lock = new object();
        private readonly IList<Action<IEndpointRouteBuilder>> _enabledServices = new List<Action<IEndpointRouteBuilder>>();

        public static DiscoveryServerBuilder CreateFor(IConfigWatcher configWatcher, int port = DefaultPort)
            => CreateFor(configWatcher, Enumerable.Empty<IDiscoveryServerCallbacks>(), port);

        public static DiscoveryServerBuilder CreateFor(IConfigWatcher configWatcher, IDiscoveryServerCallbacks callbacks, int port = DefaultPort)
            => CreateFor(configWatcher, new[] { callbacks }, port);

        public static DiscoveryServerBuilder CreateFor(IConfigWatcher configWatcher, IEnumerable<IDiscoveryServerCallbacks> callbacks, int port = DefaultPort)
        {
            if (configWatcher == null)
            {
                throw new ArgumentNullException($"{nameof(callbacks)} cannot be null when creating a {nameof(DiscoveryServerBuilder)}");
            }

            if (callbacks == null)
            {
                throw new ArgumentNullException($"{nameof(callbacks)} cannot be null when creating a {nameof(DiscoveryServerBuilder)}");
            }

            return new DiscoveryServerBuilder(configWatcher, callbacks, port);
        }

        private DiscoveryServerBuilder(IConfigWatcher configWatcher, IEnumerable<IDiscoveryServerCallbacks> callbacks, int port)
        {
            _configWatcher = configWatcher;
            _callbacks = callbacks.ToImmutableList();
            _port = port;
        }

        public DiscoveryServerBuilder ConfigureDiscoveryService<T>() where T : class
        {
            _enabledServices.Add(e => e.MapGrpcService<T>());
            return this;
        }

        public DiscoveryServerBuilder ConfigureLogging(Action<ILoggingBuilder> action)
        {
            _loggingAction = action;
            return this;
        }

        public IHost Build()
        {
            return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                if (_loggingAction != null)
                {
                    DiscoveryServerLoggerFactory.SetLoggerFactory(LoggerFactory.Create(builder => _loggingAction(builder)));
                }
                logging.AddProvider(new DiscoveryLoggerProvider());
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IDiscoveryStreamHandler>(new DiscoveryStreamHandler(_configWatcher, _callbacks));
                services.AddGrpc();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .ConfigureKestrel(k =>
                    {
                        k.ListenAnyIP(_port, options =>
                        {
                            options.Protocols = HttpProtocols.Http2;
                            options.UseHttps();
                        });
                    })
                    .Configure((app) =>
                    {
                        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                        if (env.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }

                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            foreach (var action in _enabledServices)
                            {
                                action(endpoints);
                            }

                            endpoints.MapGet("/", async context =>
                            {
                                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                            });
                        });
                    });
            })
            .Build();
        }
    }
}