using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Betting.API.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Text.Json;

namespace Platform.Betting.API.UnitTest
{
    [TestClass]
    public class UnitTestBetPlacement
    {
        private readonly IBettingRepository repo;
        private IConfiguration _config;
        private readonly IMessageBroker messagebroker;
        public UnitTestBetPlacement()
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

            messagebroker = serviceProvider.GetService<IMessageBroker>();

            messagebroker.Connect();
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

        public BetRequestDTO GetRequest()
        {
            BetRequestDTO request = new BetRequestDTO();

            request.BetDate = DateTime.Now;
            request.APIKey = "";
            request.OperatorToken = "CB2D9909-9467-EB11-89FB-00224815B68B";
            request.ClientId = "5093617A-9367-EB11-89FB-00224815B68B";
            request.ClientCode = "10C";
            request.LimitId = "2183";
            request.Endpoint = "https://preprod-operatorapi.song88.com";
            request.CompanyShare = 0.1m;
            request.ExchangeRate = 1336.98252800m;

            request.UserId = "CA2D9909-9467-EB11-89FB-00224815B68B";
            request.Stake = 1000M;
            request.Odds = 1.05m;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            request.LanguageCode = Shared.Enums.LanguageCode.EN;
            request.Channel = Shared.Enums.Channel.Internet;
            request.IPAddress = "";
            request.Selections = new System.Collections.Generic.List<BetSelectionRequestDTO>();
            BetSelectionRequestDTO newSelection = new BetSelectionRequestDTO();

            newSelection.SportId = 1;
            newSelection.SportName = "";
            newSelection.SegmentId = 1;
            newSelection.SegmentName = "";
            newSelection.LeagueId = 19;
            newSelection.LeagueName = "";
            newSelection.FixtureId = 805792717043966917;
            newSelection.FixtureName = "";
            newSelection.FixtureType = Shared.Enums.FixtureType.Match;
            newSelection.MarketTemplateId = "";
            newSelection.MarketId = 806266155319689250;
            newSelection.MarketName = "";
            newSelection.Line = "2.50";
            newSelection.SelectionTemplateId = "";
            newSelection.SelectionId = 806266155319689252;
            newSelection.SelectionName = "";
            newSelection.Odds = 1.71000000000000000000m;
            //newSelection.FixtureStatus = "";
            //newSelection.FixtureStartDate = "";

            newSelection.FixtureSpecifier = "sr:season:59630";
            newSelection.SelectionSpecifier = "uof:3/sr:sport:1/534/pre:outcometext:6170?variant=pre:markettext:62039";
            newSelection.CorrelationId = "";


            request.Selections.Add(newSelection);

            return request;
        }

        public BetRequestDTO GetRequestParlay()
        {
            BetRequestDTO request = new BetRequestDTO();

            request.BetDate = DateTime.Now;
            request.APIKey = "";
            request.OperatorToken = "CB2D9909-9467-EB11-89FB-00224815B68B";
            request.ClientId = "5093617A-9367-EB11-89FB-00224815B68B";
            request.ClientCode = "10C";
            request.LimitId = "2183";
            request.Endpoint = "https://preprod-operatorapi.song88.com";
            request.CompanyShare = 0.1m;
            request.ExchangeRate = 1336.98252800m;

            request.UserId = "CA2D9909-9467-EB11-89FB-00224815B68B";
            request.Stake = 1000M;
            request.Odds = 1.05m;
            
            request.LanguageCode = Shared.Enums.LanguageCode.EN;
            request.Channel = Shared.Enums.Channel.Internet;
            request.IPAddress = "";
            request.Selections = new System.Collections.Generic.List<BetSelectionRequestDTO>();
            BetSelectionRequestDTO newSelection = new BetSelectionRequestDTO();

            newSelection.SportId = 1;
            newSelection.SportName = "";
            newSelection.SegmentId = 1;
            newSelection.SegmentName = "";
            newSelection.LeagueId = 19;
            newSelection.LeagueName = "";
            newSelection.FixtureId = 805792717043966917;
            newSelection.FixtureName = "";
            newSelection.FixtureType = Shared.Enums.FixtureType.Match;
            newSelection.MarketTemplateId = "";
            newSelection.MarketId = 806266155319689250;
            newSelection.MarketName = "";
            newSelection.Line = "2.50";
            newSelection.SelectionTemplateId = "";
            newSelection.SelectionId = 806266155319689252;
            newSelection.SelectionName = "";
            newSelection.Odds = 1.21000000000000000000m;
            newSelection.BetScore = "0 - 0";
            newSelection.FixtureSpecifier = "sr:match:25547338";
            newSelection.SelectionSpecifier = "uof:3/sr:sport:1/18/13?total=2.5";
            newSelection.CorrelationId = "";

            request.Selections.Add(newSelection);

            newSelection = new BetSelectionRequestDTO();

            newSelection.SportId = 1;
            newSelection.SportName = "";
            newSelection.SegmentId = 1;
            newSelection.SegmentName = "";
            newSelection.LeagueId = 19;
            newSelection.LeagueName = "";
            newSelection.FixtureId = 805792717043966917;
            newSelection.FixtureName = "";
            newSelection.FixtureType = Shared.Enums.FixtureType.Match;
            newSelection.MarketTemplateId = "";
            newSelection.MarketId = 806266155319689250;
            newSelection.MarketName = "";
            newSelection.Line = "2.50";
            newSelection.SelectionTemplateId = "";
            newSelection.SelectionId = 806266155319689252;
            newSelection.SelectionName = "";
            newSelection.Odds = 1.71000000000000000000m;
            newSelection.BetScore = "0 - 0";
            newSelection.FixtureSpecifier = "sr:match:25547338";
            newSelection.SelectionSpecifier = "uof:3/sr:sport:1/18/13?total=2.5";
            newSelection.CorrelationId = "";

            request.Selections.Add(newSelection);

            return request;
        }

        [TestMethod]
        public void TestBetPlacement_KRW_Success()
        {
            BetRequestDTO request = GetRequest();

            request.Stake = 1000.70M;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            var defaultJSON = JsonSerializer.Serialize(request);
            var data = repo.PlaceBetWithValidation(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Success);
        }

        [TestMethod]
        public void TestBetPlacement_KRW_Maximum_Payout_Must_Be_Equal_Or_Lower()
        {
            BetRequestDTO request = GetRequest();

            request.Stake = 1000M;
            request.Odds = 51m;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            var defaultJSON = JsonSerializer.Serialize(request);
            var data = repo.PlaceBet(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Maximum_Payout_Must_Be_Equal_Or_Lower);
        }
        


        [TestMethod]
        public void TestBetPlacement_KRW_Stake_Must_Be_Equal_Or_Higher_To_Min_Stake()
        {
            BetRequestDTO request = GetRequest();
            request.Stake = 99M;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            var data = repo.PlaceBet(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake);
        }

        [TestMethod]
        public void TestBetPlacement_KRW_Stake_Must_Be_Equal_Or_Higher_To_Max_Stake()
        {
            BetRequestDTO request = GetRequest();

            request.Stake = 1000001M;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            var defaultJSON = JsonSerializer.Serialize(request);
            var data = repo.PlaceBet(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake);
        }


        #region Parlay

        [TestMethod]
        public void TestBetPlacement_KRW_Success_PARLAY()
        {
            BetRequestDTO request = GetRequestParlay();

            request.Stake = 1000M;
            var defaultJSON = JsonSerializer.Serialize(request);
            var data = repo.PlaceBet(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Success);
        }

        [TestMethod]
        public void TestBetPlacement_KRW_Maximum_Payout_Must_Be_Equal_Or_Lower_PARLAY()
        {
            BetRequestDTO request = GetRequestParlay();

            request.Stake = 1000M;
            request.Odds = 51m;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            var defaultJSON = JsonSerializer.Serialize(request);
            var data = repo.PlaceBet(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Maximum_Payout_Must_Be_Equal_Or_Lower);
        }



        [TestMethod]
        public void TestBetPlacement_KRW_Stake_Must_Be_Equal_Or_Higher_To_Min_Stake_PARLAY()
        {
            BetRequestDTO request = GetRequestParlay();
            request.Stake = 99M;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            var data = repo.PlaceBet(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake);
        }

        [TestMethod]
        public void TestBetPlacement_KRW_Stake_Must_Be_Equal_Or_Higher_To_Max_Stake_PARLAY()
        {
            BetRequestDTO request = GetRequestParlay();

            request.Stake = 1000001M;
            request.CurrencyCode = Shared.Enums.CurrencyCode.KRW;
            var defaultJSON = JsonSerializer.Serialize(request);
            var data = repo.PlaceBet(request);

            Assert.IsTrue(data.Result.StatusCode == Shared.Enums.StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake);
        }

        #endregion
    }
}
