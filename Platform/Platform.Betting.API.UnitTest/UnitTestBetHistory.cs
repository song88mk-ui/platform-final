using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Betting.API.Data;
using Platform.Betting.API.Repository;
using Platform.Shared.Models;
using System;
using System.Configuration;
namespace Platform.Betting.API.UnitTest
{
    [TestClass]
    public class UnitTestBetHistory
    {
        private readonly IBettingRepository repo;
        private IConfiguration _config;

        public UnitTestBetHistory()
        {
            var services = new ServiceCollection();
            services.AddDbContext<BettingContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Betting"]));
            services.AddTransient<IBettingRepository, BettingRepository>();
            var serviceProvider = services.BuildServiceProvider();
            repo = serviceProvider.GetService<IBettingRepository>();
        }


        public IConfiguration Configuration
        {
            get
            {
                if (_config == null)
                {
                    var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
                    _config = builder.Build();
                }

                return _config;
            }
        }

        [TestMethod]
        public void TestBetHistory()
        {
            BetHistoryRequestDTO request = new BetHistoryRequestDTO();
            request.BetStatusFilter = Shared.Enums.BetStatusFilter.All;
            request.ClientId = "5093617A-9367-EB11-89FB-00224815B68B";
            request.UserId = "CA2D9909-9467-EB11-89FB-00224815B68B";
            request.Page = 1;
            request.PageSize = 50;
            request.FromDate = DateTime.Parse("1/1/2021");
            request.ToDate = DateTime.Parse("3/1/2021");

            var data = repo.GetBetHistory(request);
        }

        [TestMethod]
        public void TestBetHistoryIsCurrentTrue()
        {
            BetHistoryRequestDTO request = new BetHistoryRequestDTO();
            request.BetStatusFilter = Shared.Enums.BetStatusFilter.All;
            request.ClientId = "5093617A-9367-EB11-89FB-00224815B68B";
            request.UserId = "CA2D9909-9467-EB11-89FB-00224815B68B";
            request.Page = 1;
            request.IsCurrent = true;
            request.PageSize = 50;
            request.FromDate = DateTime.Parse("1/1/2021");
            request.ToDate = DateTime.Parse("3/1/2021");

            var data = repo.GetBetHistory(request);
        }

        [TestMethod]
        public void TestBetHistoryIsCurrentFalse()
        {
            BetHistoryRequestDTO request = new BetHistoryRequestDTO();
            request.BetStatusFilter = Shared.Enums.BetStatusFilter.All;
            request.ClientId = "5093617A-9367-EB11-89FB-00224815B68B";
            request.UserId = "CA2D9909-9467-EB11-89FB-00224815B68B";
            request.Page = 1;
            request.IsCurrent = false;
            request.PageSize = 50;
            request.FromDate = DateTime.Parse("1/1/2021");
            request.ToDate = DateTime.Parse("3/1/2021");

            var data = repo.GetBetHistory(request);
        }

    }
}
