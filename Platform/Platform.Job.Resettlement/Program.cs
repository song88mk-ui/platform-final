using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Job.Resettlement.Repository;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;

namespace Platform.Job.Resettlement
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
                    services.AddSingleton<IPlatformRepository, PlatformRepository>();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddHostedService<Worker>();
                    services.AddDbContext<PlatformContext>(o => o.UseSqlServer(hostContext.Configuration["ConnectionStrings:Platform"], opt => opt.CommandTimeout((int)TimeSpan.FromMinutes(30).TotalSeconds)));
                }).UseWindowsService();
    }
}
