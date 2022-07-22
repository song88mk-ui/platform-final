using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Job.BetAudit.Tableau.Data;
using Platform.Job.BetAudit.Tableau.Data.Betting;
using Platform.Job.BetAudit.Tableau.Repository;

namespace Platform.Job.BetAudit.Tableau
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
                    services.AddSingleton<ITableauRepository, TableauRepository>();
                    services.AddDbContext<BettingContext>(o => o.UseSqlServer(hostContext.Configuration["ConnectionStrings:Betting"]));
                    services.AddDbContext<TableauContext>(o => o.UseSqlServer(hostContext.Configuration["ConnectionStrings:Tableau"]));
                    services.AddHostedService<Worker>();
                }).UseWindowsService();
    }
}
