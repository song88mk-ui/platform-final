using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.BetInfo.Service.Repository;
using Platform.Shared.Utility;

namespace Platform.BetInfo.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSimpleConsole(opts => { opts.SingleLine = true; });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
