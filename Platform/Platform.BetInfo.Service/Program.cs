using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.BetInfo.Service.Repository;
using Platform.Shared.Data.Betting;
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
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddDbContextFactory<BettingContext>(options =>
                        options.UseSqlServer(hostContext.Configuration["ConnectionStrings:Betting"]));
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
