using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.CacheSync.Service.Repository;
using Platform.Shared.Utility;

namespace Platform.CacheSync.Service
{
    public class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<ICacheSyncRepository, CacheSyncRepository>();
                    services.AddSingleton<ISportsbookRepository, SportsbookRepository>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
