using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Platform.WebSocket.API.Telemetry;
using System;

namespace Platform.WebSocket.API.Extensions
{
    public static class TelemetryExtension
    {
        private static IConfiguration _configuration;

        public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            _configuration = configuration;

            Action<ResourceBuilder> resource = r => r.AddService(
                    serviceName: "Platform.WebSocket.API",
                    serviceNamespace: _configuration["Telemetry:Environment"] ?? "dev",
                    serviceVersion: "latest",
                    serviceInstanceId: Environment.MachineName);

            services.AddSingleton<Instrumentation>();

            if (Convert.ToBoolean(_configuration["Telemetry:Enabled"]))
            {
                services.AddOpenTelemetry()
                    .ConfigureResource(resource)
                    .WithTracing(tracing =>
                    {
                        tracing
                        .AddSource(Instrumentation.ActivitySourceName)
                        .SetSampler(new AlwaysOnSampler())
                        .AddOtlpExporter(exporter =>
                        {
                            exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                            exporter.Endpoint = new Uri(_configuration["Telemetry:Collector"]+"/v1/traces");
                        });
                    });
            }

            return services;
        }
    }
}
