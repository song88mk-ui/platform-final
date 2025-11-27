using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.Shared.Telemetry;
using System;

namespace Platform.Shared.Extensions
{
    public class TelemetrySettings
    {
        public string ServiceName { get; set; }
        public string ActivitySourceName { get; set; }
        public string MeterName { get; set; }
        public int MetricsPort { get; set; }

        public TelemetrySettings(string ServiceName, string ActivitySourceName, string MeterName)
        {
            this.ServiceName = ServiceName;
            this.ActivitySourceName = ActivitySourceName;
            this.MeterName = MeterName;
            this.MetricsPort = 8080;
        }
    }

    public static class TelemetryExtension
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services, TelemetrySettings settings)
        {
            var sp = services.BuildServiceProvider();

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(settings.ServiceName);
            var configuration = sp.GetService<IConfiguration>();
            var telemetryEnabled = configuration?.GetValue<bool>("Telemetry:Enabled") ?? false;

            // No need to register anything if telemetry is disabled.
            if (!telemetryEnabled) {
                logger.LogInformation("Open Telemetry is disabled for {ServiceName}", settings.ServiceName);
                return services;
            }

            var metricsPort = configuration.GetValue<int>("Telemetry:MetricsPort", settings.MetricsPort);

            logger.LogInformation(
                "Setting up Open Telemetry for {ServiceName}: Prometheus endpoint on port {MetricsPort}",
                settings.ServiceName,
                metricsPort);

            services
                .AddOpenTelemetry()
                .ConfigureResource(r => r
                    .AddService(
                        serviceName: settings.ServiceName,
                        serviceVersion: "latest",
                        serviceInstanceId: Environment.MachineName
                    )
                )
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(settings.ActivitySourceName)
                        .SetSampler(new AlwaysOnSampler())
                        .AddOtlpExporter();
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddMeter(settings.MeterName)
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddPrometheusHttpListener(opts => opts.UriPrefixes = new string[] { $"http://+:{metricsPort}" });
                });

            return services;
        }
    }
}
