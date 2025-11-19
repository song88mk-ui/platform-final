using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.CashOut.Service.Repository;
using Platform.Shared.Utility;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.IO;

namespace Platform.CashOut.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var outputTemplate = "{Timestamp:yyyy-MM-ddTHH:mm:ssfff zzz} [{Level:u3}] {Message:j}{NewLine:l}{Exception:l}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .WriteTo.Debug(outputTemplate: outputTemplate)
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
