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
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var filePath = configuration.GetSection("Logger").GetSection("DirectoryPath").Value;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .WriteTo.Logger(lc => lc.Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Information )
                    .WriteTo.File(Path.Combine(filePath, "app.log"),
                        outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ssK} {Level:w3} {Message}{NewLine}",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 15
                        ))
                .CreateLogger();


            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Service failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
