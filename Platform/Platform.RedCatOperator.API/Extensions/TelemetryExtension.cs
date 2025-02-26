using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.RedCatOperator.API.Telemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

namespace Platform.RedCatOperator.API.Extensions
{
    public static class TelemetryExtension
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<Instrumentation>();

						if (Convert.ToBoolean(configuration["Telemetry:Enabled"]))
            {
                services
                .AddOpenTelemetry()
                .ConfigureResource(r => r
                    .AddService(
                        serviceName: "Platform.RedCatOperator.API",
                        serviceVersion: "latest",
                        serviceInstanceId: Environment.MachineName
                    )
                )
                .WithTracing(tracing =>
                {
                    tracing
                    .AddSource(Instrumentation.ActivitySourceName)
                    .SetSampler(new AlwaysOnSampler())
                    .AddOtlpExporter();
                })
                .WithMetrics(metrics =>
                {
                    metrics
                    .AddMeter(Instrumentation.MeterName)
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter();
                });
            }

            return services;
        }

    }
}
