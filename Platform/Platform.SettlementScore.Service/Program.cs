using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.SettlementScore.Service.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.SettlementScore.Service
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
                    services.AddSingleton<IMemCache, MemCache>();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IBettingRepository, BettingRepository>();
                    services.AddHostedService<Worker>();
                    services.AddDbContext<BettingContext>(o => o.UseSqlServer(hostContext.Configuration["ConnectionStrings:Betting"]));
                    services.AddHttpClient<IBetradarRepository, BetradarRepository>();
                }).UseWindowsService();
    }
}
