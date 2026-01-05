using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Provider.Service.Repository;
using Platform.Shared.Providers;
using Platform.Shared.Utility;
using Serilog;
using System;
using System.IO;

namespace Platform.Provider.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((context, _, serilog) =>
                {
                    serilog.ReadFrom.Configuration(context.Configuration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddBettingProviders();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
