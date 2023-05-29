using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Platform.Settlement.Service.Repository;
using Platform.Settlement.Service.Telemetry;
using Platform.Shared.Utility;
using System;
using System.Reflection;

namespace Platform.Settlement.Service
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
                    var configuration = services.BuildServiceProvider().GetService<IConfiguration>();

                    Action<ResourceBuilder> resource = r => r.AddService(
                        serviceName: String.Format("{0}/{1}", configuration["Telemetry:Environment"], "Platform.Settlement.Service"),
                        serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "latest",
                        serviceInstanceId: Environment.MachineName);

                    services.AddSingleton<Instrumentation>();

                    if (Convert.ToBoolean(configuration["Telemetry:Enabled"]))
                    {
                      services
                        .AddOpenTelemetry()
                        .ConfigureResource(resource)
                        .WithTracing(t =>
                        {
                          t
                            .AddSource(Instrumentation.ActivitySourceName)
                            .SetSampler(new AlwaysOnSampler())
                            .AddSqlClientInstrumentation()
                            .AddOtlpExporter(o =>
                            {
                              o.Protocol = OtlpExportProtocol.HttpProtobuf;
                              o.Endpoint = new Uri(configuration["Telemetry:Collector"]+"/v1/traces");
                            });
                        })
                        .WithMetrics(m =>
                        {
                          m
                            .AddMeter(Instrumentation.MeterName)
                            .AddRuntimeInstrumentation()
                            .AddOtlpExporter(o =>
                            {
                              o.Protocol = OtlpExportProtocol.HttpProtobuf;
                              o.Endpoint = new Uri(configuration["Telemetry:Collector"]+"/v1/metrics");
                            });
                        });
                    }

                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
