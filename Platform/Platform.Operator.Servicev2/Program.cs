using Microsoft.EntityFrameworkCore;
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
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;
using System;
using Serilog;

namespace Platform.Operator.Servicev2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((ctx, _, log) =>
                {
                    log.ReadFrom.Configuration(ctx.Configuration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<Instrumentation>();
                    services.AddTelemetry(new TelemetrySettings(
                        ServiceName: Instrumentation.ActivitySourceName,
                        ActivitySourceName: Instrumentation.ActivitySourceName,
                        MeterName: Instrumentation.MeterName
                    ));

                    services.AddHttpClient();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddDbContextFactory<PlatformContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("Platform")));
                    services.AddDbContextFactory<BettingContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("Betting")));
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
