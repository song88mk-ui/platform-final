using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Betting.API.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Platform;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Betting.API.UnitTest
{
    [TestClass]
    public class UnitTestBetConfig
    {
        private readonly IBettingRepository repo;
        private IConfiguration _config;
        private readonly IMessageBroker messagebroker;
        protected readonly BettingContext _contextBetting;
        protected readonly PlatformContext _contextPlatform;
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
                a.BaseAddress = new Uri(Configuration["API:BackOfficeAPI"]);
            });


            var serviceProvider = services.BuildServiceProvider();
            repo = serviceProvider.GetService<IBettingRepository>();

            string BettingconnStr = Configuration["ConnectionStrings:Betting"];
            string PlatformDBconnStr = Configuration["ConnectionStrings:Platform"];

            var options = new DbContextOptionsBuilder<BettingContext>()
            .UseSqlServer(BettingconnStr)
            .Options;
            _contextBetting = new BettingContext(options);
            _contextBetting.Database.EnsureCreated();

            var options2 = new DbContextOptionsBuilder<PlatformContext>()
            .UseSqlServer(PlatformDBconnStr)
            .Options;
            _contextPlatform = new PlatformContext(options2);
            _contextPlatform.Database.EnsureCreated();
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
            var betConfig = _contextBetting.BetConfig.ToList();
            Guid guid = new Guid("F135F463-1B70-EB11-89FC-00224815B68B");
            var clients = _contextPlatform.Client.Where(x=>x.ClientId== guid).ToList();
            var currencies = _contextPlatform.ForexEUR.ToList();


            foreach (var client in clients)
            {
                foreach (var row in currencies)
                {
                    BetConfigRequestDTO request = new BetConfigRequestDTO();
                    request.CurrencyCode = row.CurrencyCode;
                    request.CompanyShare = client.EffectiveDate < DateTime.UtcNow && client.CurrentCompanyShare != client.CurrentCompanyShare ?
                                                 client.NextCompanyShare : client.CurrentCompanyShare; ;
                    request.ExchangeRate = row.ExchangeRate;
                    request.ClientId = client.ClientId.ToString();

                    var data = repo.GetBetConfig(request);
                    
                }
            }

            
            
        }
    }
}
