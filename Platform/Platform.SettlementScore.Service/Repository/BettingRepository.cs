using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace Platform.SettlementScore.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly IBetradarRepository _betradarRepository;
        private readonly ICricketScoreRepository _cricketScoreRepository;

        public List<BetMemory> _bets = new List<BetMemory>();
        public List<Tuple<long, List<Guid>>> _fixtureBetReference = new List<Tuple<long, List<Guid>>>();

        private readonly IMongoDatabase _db;
        private IMongoCollection<Fixture> fixtureCollection;

        public BettingRepository(IServiceProvider serviceProvider, IConfiguration configuration, IMemCache memCache, IBetradarRepository betradarRepository, ICricketScoreRepository cricketScoreRepository)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _memCache = memCache;
            _betradarRepository = betradarRepository;
            _cricketScoreRepository = cricketScoreRepository;

            var client = new MongoClient(_configuration["ConnectionStrings:MongoDB-Sportsbook"]);
            _db = client.GetDatabase("Sportsbook");

            fixtureCollection = _db.GetCollection<Fixture>("Fixture");
        }

        private HttpClient GetHttpClient(string endpoint, string clientId)
        {
            var result = new HttpClient
            {
                BaseAddress = new Uri(endpoint),
                Timeout = TimeSpan.FromSeconds(5)
            };

            result.DefaultRequestHeaders.Add("x-client-id", clientId);

            return result;
        }

        public void PreloadActiveBets()
        {
            _bets = new List<BetMemory>();
            _fixtureBetReference = new List<Tuple<long, List<Guid>>>();

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BettingContext>();

            var bets = context.Bet.AsNoTracking().Where(c => c.BetStatusId == (int)BetStatus.Running).Select(s => new BetMemory
            {
                BetId = s.BetId,
                ClientId = s.ClientId,
                UserId = s.UserId,
                BetStatusId = s.BetStatusId,
                TrueStake = s.TrueStake,
                Odds = s.Odds,
                StakeTax = s.StakeTax,
                WinningsTax = s.WinningsTax,
                Endpoint = s.Endpoint,
            }).ToList();

            foreach (var bet in bets)
            {
                var betSelections = context.BetSelection.AsNoTracking().Where(c => c.BetId == bet.BetId).Select(s => new BetSelectionMemory
                {
                    BetId = s.BetId,
                    BetSelectionId = s.BetSelectionId,
                    SportId = s.SportId,
                    FixtureId = s.FixtureId,
                    FixtureTypeId = s.FixtureTypeId,
                    MarketId = s.MarketId,
                    SelectionId = s.SelectionId,
                    BetSelectionStatusId = s.BetSelectionStatusId,
                }).ToList();

                if (betSelections.Count == 0 || betSelections.Any(c => c.SportId > 1000 || c.FixtureTypeId == (int)FixtureType.Outright || c.BetSelectionStatusId != (int)BetStatus.Running))
                    return;

                bet.BetSelections.AddRange(betSelections);

                _bets.Add(bet);

                foreach (var betSelection in betSelections)
                {
                    var copy = _fixtureBetReference.Where(c => c.Item1 == betSelection.FixtureId).FirstOrDefault();

                    if (copy != null)
                    {
                        if (copy.Item2.Any(c => c == bet.BetId)) continue;

                        copy.Item2.Add(bet.BetId);
                    }
                    else
                        _fixtureBetReference.Add(new Tuple<long, List<Guid>>(betSelection.FixtureId, new List<Guid> { bet.BetId }));
                }
            }
        }
        public void AddBet(OnBetPlacement message)
        {
            var bet = new BetMemory
            {
                BetId = message.Bet.BetId,
                ClientId = message.Bet.ClientId,
                UserId = message.Bet.UserId,
                BetStatusId = message.Bet.BetStatusId,
                TrueStake = message.Bet.TrueStake,
                Odds = message.Bet.Odds,
                StakeTax = message.Bet.StakeTax,
                WinningsTax = message.Bet.WinningsTax,
                Endpoint = message.Bet.Endpoint,
            };

            var betSelections = message.BetSelections.Select(s => new BetSelectionMemory
            {
                BetId = s.BetId,
                BetSelectionId = s.BetSelectionId,
                SportId = s.SportId,
                FixtureId = s.FixtureId,
                FixtureTypeId = s.FixtureTypeId,
                MarketId = s.MarketId,
                SelectionId = s.SelectionId,
                BetSelectionStatusId = s.BetSelectionStatusId,
            }).ToList();

            if (betSelections.Count == 0 || betSelections.Any(c => c.SportId > 1000 || c.FixtureTypeId == (int)FixtureType.Outright || c.BetSelectionStatusId != (int)BetStatus.Running))
                return;

            bet.BetSelections.AddRange(betSelections);

            _bets.Add(bet);

            foreach (var betSelection in betSelections)
            {
                var copy = _fixtureBetReference.Where(c => c.Item1 == betSelection.FixtureId).FirstOrDefault();

                if (copy != null)
                {
                    if (copy.Item2.Any(c => c == bet.BetId)) continue;

                    copy.Item2.Add(bet.BetId);
                }
                else
                    _fixtureBetReference.Add(new Tuple<long, List<Guid>>(betSelection.FixtureId, new List<Guid> { bet.BetId }));
            }

            if (_bets.Any(c => c.BetId == bet.BetId))
                Console.WriteLine($"{DateTime.UtcNow} - Add Bet : {bet.BetId.ToString().ToUpper()} : True");
            else
                Console.WriteLine($"{DateTime.UtcNow} - Add Bet : {bet.BetId.ToString().ToUpper()} : False");
        }

        public async Task ProcessBet(OnBetSettlement message)
        {
            var bet = _bets.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

            if (bet != null)
            {
                var fixtureIds = bet.BetSelections.Select(s => s.FixtureId).Distinct().ToList();

                var fixtureBetReference = _fixtureBetReference.Where(c => fixtureIds.Contains(c.Item1)).ToList();

                foreach (var fixtureBet in fixtureBetReference)
                {
                    var fixtureEntry = fixtureBet.Item1;

                    var filter = Builders<Fixture>.Filter.Eq("FiId", fixtureEntry);
                    var projection = Builders<Fixture>.Projection.Include(c => c.FiId).Include(c => c.FiStId).Include(c => c.IsSu).Include(c => c.FiSp).Include(c => c.SpId);
                    var fixtureResult = fixtureCollection.Find(filter).Project<FixtureSettlementFilter>(projection).FirstOrDefault();

                    var settlementScore = string.Empty;

                    if (fixtureResult != null)
                    {

                        if (fixtureResult.SpId != 21)
                            settlementScore = (await _betradarRepository.GetScoreSummary(fixtureResult.FiSp));
                        else
                            settlementScore = (await _cricketScoreRepository.GetScoreSummary(fixtureResult.FiSp));


                        if (!string.IsNullOrEmpty(settlementScore))
                        {
                            //TODO: update logic in sending message to operator
                            //call operator api
                            var payload = new 
                            {
                                BetId = bet.BetId,
                                BetStatus = bet.BetStatusId.ToString(),
                                UserId = bet.UserId
                            };
                            var path = "/v1/settlement/score";
                            var response = GetHttpClient(bet.Endpoint, bet.ClientId).PostAsync($"{bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                //var result = JsonSerializer.Deserialize<CashoutResultResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                            }
                        }

                        //remove _fixtureBetReference
                        if (fixtureResult.FiStId == (int)FixtureStatus.Closed || fixtureResult.FiStId == (int)FixtureStatus.Ended)
                            _fixtureBetReference.Remove(fixtureBet);

                    }
                }

                _bets = _bets.Where(c => c.BetId != message.Bet.BetId).ToList();

                if (_bets.Any(c => c.BetId == message.Bet.BetId))
                    Console.WriteLine($"{DateTime.UtcNow} - Remove Bet : {message.Bet.BetId.ToString().ToUpper()} : False");
                else
                    Console.WriteLine($"{DateTime.UtcNow} - Remove Bet : {message.Bet.BetId.ToString().ToUpper()} : True");
            }
        }
    }

    public class BetMemory
    {
        public BetMemory()
        {
            BetSelections = new List<BetSelectionMemory>();
        }

        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public int BetStatusId { get; set; }
        public decimal TrueStake { get; set; }
        public decimal Odds { get; set; }
        public decimal StakeTax { get; set; }
        public decimal WinningsTax { get; set; }
        public string Endpoint { get; set; }

        public List<BetSelectionMemory> BetSelections { get; set; }
    }

    public class BetSelectionMemory
    {
        public Guid BetId { get; set; }
        public Guid BetSelectionId { get; set; }
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public int FixtureTypeId { get; set; }
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public int BetSelectionStatusId { get; set; }
    }

    public class OnExternalOddsChangeMessage
    {
        public OnExternalOddsChangeMessage()
        {
            Markets = new List<ExternalMarketMessage>();
        }
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public int FixtureStatusId { get; set; }
        public FixtureType FixtureType { get; set; }
        public bool IsSuspended { get; set; }
        public List<ExternalMarketMessage> Markets { get; set; }
    }

    public class ExternalMarketMessage
    {
        public ExternalMarketMessage()
        {
            Selections = new List<ExternalSelectionMessage>();
        }
        public long MarketId { get; set; }
        public MarketStatus MarketStatus { get; set; }
        public List<ExternalSelectionMessage> Selections { get; set; }
    }

    public class ExternalSelectionMessage
    {
        public long SelectionId { get; set; }
        //public long MarketId { get; set; }
        //public int MarketStatusId { get; set; }
        public bool IsActive { get; set; }
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
    }
    public class Probabilities
    {
        public Guid BetSelectionId { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public double Probability { get; set; }
        public int CashOutStatusId { get; set; }
    }
}
