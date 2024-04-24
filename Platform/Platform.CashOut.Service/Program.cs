using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.CashOut.Service.Repository;
using Platform.Shared.Utility;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace Platform.CashOut.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            var logSettings = new LogSettings();

            config.GetSection("Logger").Bind(logSettings);

            if (string.IsNullOrEmpty(logSettings.DirectoryPath) || string.IsNullOrWhiteSpace(logSettings.DirectoryPath))
            {
                logSettings.DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "fci");
            }

            var outputTemplate = "{Timestamp:yyyy-MM-ddTHH:mm:ssfff zzz} [{Level:u3}] {Message:j}{NewLine:l}{Exception:l}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: outputTemplate)
                .WriteTo.File(
                    Path.Combine(logSettings.DirectoryPath, $"logfile_.log"),
                    retainedFileCountLimit: logSettings.RetentionInDays,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: outputTemplate)
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
