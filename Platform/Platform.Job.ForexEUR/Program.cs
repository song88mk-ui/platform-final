using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Job.ForexEUR.Repository;

namespace Platform.Job.ForexEUR
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
                    logging.AddSimpleConsole(console => { console.SingleLine = true; });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<IPlatformRepository, PlatformRepository>();
                    services.AddHostedService<Worker>();
                });
    }
}
