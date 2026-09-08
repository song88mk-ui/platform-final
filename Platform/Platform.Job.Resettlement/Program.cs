using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Job.Resettlement.Repository;
using Platform.Job.Resettlement.Telemetry;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;
using Serilog;
using System;

namespace Platform.Job.Resettlement
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

                    services.AddSingleton<IPlatformRepository, PlatformRepository>();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddHostedService<Worker>();
                    services.AddDbContext<PlatformContext>(o =>
                            o.UseSqlServer(hostContext.Configuration["ConnectionStrings:Platform"],
                                opt => opt.CommandTimeout((int)TimeSpan.FromMinutes(30).TotalSeconds)));
                });
    }
}
