using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Betting.API.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
namespace Platform.Betting.API.UnitTest
{
    [TestClass]
    public class UnitTestBetConfig
    {
        private readonly IBettingRepository repo;
        private IConfiguration _config;
        private readonly IMessageBroker messagebroker;
        public UnitTestBetConfig()
        {
            var services = new ServiceCollection();
            services.AddDbContext<BettingContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Betting"]));
            services.AddSingleton(Configuration);
            services.AddTransient<IBettingRepository, BettingRepository>();
            services.AddTransient<IMessageBroker, MessageBroker>();

            services.AddHttpClient();
            services.AddHttpClient("BackOffice", a =>
            {
                a.BaseAddress = new Uri(Configuration["API:BackOffice"]);
            });
            services.AddHttpClient("Song88Betting", a =>
            {
                a.BaseAddress = new Uri(Configuration["API:Song88Betting"]);
            });

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
        public void TestBetConfig()
        {
            BetConfigRequestDTO request = new BetConfigRequestDTO();
            request.CurrencyCode = "KRW";
            request.CompanyShare = 0.1m;
            request.ExchangeRate =1336.04M;
            request.ClientId = "5093617a-9367-eb11-89fb-00224815b68b";

            var data = repo.GetBetConfig(request);
        }
              

    }
}
