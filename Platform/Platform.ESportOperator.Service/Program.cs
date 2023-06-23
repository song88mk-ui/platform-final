using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.ESportOperator.Service.Repository;
using Platform.Shared.Utility;

namespace Platform.ESportOperator.Service
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
                    services.AddHttpClient();
                    services.AddSingleton<IMessageBroker, MessageBroker>();
                    services.AddSingleton<IPlatformRepository, PlatformRepository>();
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
