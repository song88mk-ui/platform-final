using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.Operator.Servicev2.Repository;
using Platform.Operator.Servicev2.Telemetry;
using Platform.Shared.Utility;
using System;

namespace Platform.Operator.Servicev2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var sp = services.BuildServiceProvider();
                    var configuration = sp.GetService<IConfiguration>();
                    var logger = sp.GetRequiredService<ILogger<Instrumentation>>();

                    Action<ResourceBuilder> resource = r => r.AddService(
                        serviceName: "Platform.Operator.Servicev2",
                        serviceVersion: "latest",
                        serviceInstanceId: Environment.MachineName);

                    services.AddSingleton<Instrumentation>();

                    if (Convert.ToBoolean(configuration["Telemetry:Enabled"]))
                    {
                        AppContext.SetSwitch(
                                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                        var metricsPort = configuration.GetValue<int>("Telemetry:MetricsPort", 8080);
                        logger.LogInformation("Setting up Open Telemetry: Prometheus endpoint on port {metricsPort}", metricsPort);

                        services
                          .AddOpenTelemetry()
                          .ConfigureResource(resource)
                          .WithTracing(tracing =>
                          {
                              tracing
                              .AddSource(Instrumentation.ActivitySourceName)
                              .SetSampler(new AlwaysOnSampler())
                              .AddSqlClientInstrumentation()
                              .AddOtlpExporter();
                          })
                          .WithMetrics(metrics =>
                          {
                              metrics
                              .AddMeter(Instrumentation.MeterName)
                              .AddRuntimeInstrumentation()
                            .AddPrometheusHttpListener(opts => opts.UriPrefixes = new string[] { $"http://+:{metricsPort}" });
                          });
                    }

                    services.AddHttpClient();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
