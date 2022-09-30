using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.BetBuilder.Service.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.BetBuilder.Service
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
                    services.AddSingleton<ISportsbookRepository, SportsbookRepository>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
