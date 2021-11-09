using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.BetStatus.Service.Utility;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.IO;

namespace Platform.BetStatus.Service
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

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter()).WriteTo.Debug(outputTemplate: DateTime.Now.ToString())
                .WriteTo.File(Path.Combine(logSettings.DirectoryPath, "app.log")
                    , retainedFileCountLimit: logSettings.RetentionInDays
                    , rollingInterval: RollingInterval.Day
                    , outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ssK} {Level:w3} {RequestId} {Message:lj}{NewLine}")
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
