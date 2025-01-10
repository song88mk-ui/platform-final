using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.API.Telemetry;
using Platform.Shared.Telemetry;
using System;

namespace Platform.API.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            Action<ResourceBuilder> resource = r => r.AddService(
                    serviceName: "Platform.API",
                    serviceVersion: "latest",
                    serviceInstanceId: Environment.MachineName);

            services.AddSingleton<Instrumentation>();

            if (Convert.ToBoolean(configuration["Telemetry:Enabled"]))
            {
                services
                    .AddOpenTelemetry()
                    .ConfigureResource(resource)
                    .WithTracing(tracing =>
                    {
                        tracing
                        .AddSource(Instrumentation.ACTIVITY_SOURCE_NAME)
                        .SetSampler(new AlwaysOnSampler())
                        .AddHttpClientInstrumentation(
                            (opts) => opts.FilterHttpRequestMessage =
                                (req) =>
                                {
                                    var uriString = req.RequestUri.ToString();
                                    var isAzureReq = uriString.Contains("azure.com") ||
                                                     uriString.Contains("visualstudio.com");
                                    return !isAzureReq;
                                }
                        )
                        .AddOtlpExporter();
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics
                        .AddMeter(Instrumentation.METER_NAME)
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddProcessInstrumentation()
                        .AddOtlpExporter();
                    });
            }

            services.AddApplicationInsightsTelemetry();
            services.AddApplicationInsightsTelemetryProcessor<IgnoreRequestsPathsProcessor>();

            return services;
        }
    }
}
