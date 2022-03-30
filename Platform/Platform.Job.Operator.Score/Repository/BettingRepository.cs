using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

namespace Platform.Job.Operator.Score.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly IBetradarRepository _betradarRepository;
        private readonly ICricketScoreRepository _cricketScoreRepository;

        public List<BetMemory> _bets = new List<BetMemory>();
        public List<Tuple<long, int, string, List<BetMemory>>> _fixtureBetReference = new List<Tuple<long, int, string, List<BetMemory>>>();

        public BettingRepository(IServiceProvider serviceProvider, IConfiguration configuration, IMemCache memCache, IBetradarRepository betradarRepository, ICricketScoreRepository cricketScoreRepository)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _memCache = memCache;
            _betradarRepository = betradarRepository;
            _cricketScoreRepository = cricketScoreRepository;
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

        public void PreloadSettledBets()
        {
            Console.WriteLine($"{DateTime.UtcNow} - Preload Settled Bets : Start");

            _bets = new List<BetMemory>();
            _fixtureBetReference = new List<Tuple<long, int, string, List<BetMemory>>>();

            var clientCodes = string.IsNullOrEmpty(_configuration["Custom:ClientCodes"]) ? new List<string>() : _configuration["Custom:ClientCodes"].Split(",").ToList();

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BettingContext>();

            var settledStatus = new List<int> { (int)BetStatus.Won, (int)BetStatus.Lost, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost, (int)BetStatus.Void };

            Console.WriteLine($"{DateTime.UtcNow} - Getting Bets : Start");
            var bets = context.Bet.AsNoTracking().Where(c => clientCodes.Contains(c.ClientCode) && c.SettlementDate >= DateTime.UtcNow.AddHours(-5) && settledStatus.Contains(c.BetStatusId)).Select(s => new BetMemory
            {
                BetId = s.BetId,
                ClientId = s.ClientId,
                Endpoint = s.Endpoint,
            }).ToList();
            Console.WriteLine($"{DateTime.UtcNow} - Getting Bets : End");

            foreach (var bet in bets)
            {
                Console.WriteLine($"{DateTime.UtcNow} - Getting Bet Selections : Start");
                var betSelections = context.BetSelection.AsNoTracking().Where(c => c.BetId == bet.BetId).Select(s => new BetSelectionMemory
                {
                    BetId = s.BetId,
                    SportId = s.SportId,
                    FixtureId = s.FixtureId,
                    FixtureTypeId = s.FixtureTypeId,
                    BetSelectionStatusId = s.BetSelectionStatusId,
                    FixtureSpecifier = s.FixtureSpecifier,
                    SettlementScore = s.SettlementScore,
                }).ToList();
                Console.WriteLine($"{DateTime.UtcNow} - Getting Bet Selections : End");

                if (betSelections.Count == 0 || betSelections.Any(c => c.SportId > 1000 || c.FixtureTypeId == (int)FixtureType.Outright || !settledStatus.Contains(c.BetSelectionStatusId) ))
                    return;

                bet.BetSelections.AddRange(betSelections);

                _bets.Add(bet);

                Console.WriteLine($"{DateTime.UtcNow} - AddOrUpdate _fixtureBetReference : Start");
                foreach (var betSelection in betSelections)
                {
                    var copy = _fixtureBetReference.Where(c => c.Item1 == betSelection.FixtureId).FirstOrDefault();

                    if (copy != null)
                    {
                        if (copy.Item4.Any(c => c.BetId == bet.BetId)) continue;

                        copy.Item4.Add(bet);
                    }
                    else
                        _fixtureBetReference.Add(new Tuple<long, int, string, List<BetMemory>>(betSelection.FixtureId, betSelection.SportId, betSelection.FixtureSpecifier, new List<BetMemory> { bet }));
                }
                Console.WriteLine($"{DateTime.UtcNow} - AddOrUpdate _fixtureBetReference : End");

            }
            Console.WriteLine($"{DateTime.UtcNow} - Preload Settled Bets : End (Count: {_fixtureBetReference.Count})");

        }

        public async Task ProcessSettledBets()
        {
            Console.WriteLine($"{DateTime.UtcNow} - Process Settled Bets : Start");

            if (_fixtureBetReference.Any())
            {
                foreach (var fixtureBet in _fixtureBetReference)
                {
                    
                    var cacheKey = $"fs-score:{fixtureBet.Item3}";

                    var fsResult = _memCache.GetCache<string>(cacheKey);

                    if (fsResult == null)
                    {
                        var settlementScore = string.Empty;

                        if (fixtureBet.Item2 != 21)
                            settlementScore = (await _betradarRepository.GetScoreSummary(fixtureBet.Item3));
                        else
                            settlementScore = (await _cricketScoreRepository.GetScoreSummary(fixtureBet.Item3));


                        if (!string.IsNullOrEmpty(settlementScore) && fixtureBet.Item4.Any())
                        {
                            var clientIds = new List<string>();
                            foreach(var betMemory in fixtureBet.Item4)
                            {
                                if (clientIds.Contains(betMemory.ClientId))
                                    continue;

                                var selectionMemory = betMemory.BetSelections.Where(s => s.FixtureId == fixtureBet.Item1).FirstOrDefault();
                                if (selectionMemory != null && selectionMemory.SettlementScore != settlementScore)
                                {
                                    //call operator api
                                    var payload = new
                                    {
                                        FixtureId = fixtureBet.Item1,
                                        Score = settlementScore,
                                    };

                                    var path = "/v1/fixture/score";
                                    var response = GetHttpClient(betMemory.Endpoint, betMemory.ClientId).PostAsync($"{betMemory.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                                    if (response.IsSuccessStatusCode)
                                    {
                                        clientIds.Add(betMemory.ClientId);
                                        //add or update fixtureSpecifier memory cache
                                        AddOrUpdateFixtureSpecfier(fixtureBet.Item3);

                                    }

                                }
                            }
                        }

                        Console.WriteLine($"{DateTime.UtcNow} - Settlement Score : {fixtureBet.Item3} : {settlementScore} ");

                    }
                }
            }
            Console.WriteLine($"{DateTime.UtcNow} - Process Settled Bets : End");

        }

        private void AddOrUpdateFixtureSpecfier(string fixtureSpecifier)
        {
            var expiration = DateTime.UtcNow.AddHours(12);

            var cacheKey = $"fs-score:{fixtureSpecifier}";

            var fsResult = _memCache.GetCache<string>(cacheKey);

            if (string.IsNullOrEmpty(fsResult))
            {
                _memCache.AddOrUpdateCache(cacheKey, fixtureSpecifier, expiration);
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
        public string Endpoint { get; set; }

        public List<BetSelectionMemory> BetSelections { get; set; }
    }

    public class BetSelectionMemory
    {
        public Guid BetId { get; set; }
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public int FixtureTypeId { get; set; }
        public int BetSelectionStatusId { get; set; }
        public string FixtureSpecifier { get; set; }
        public string SettlementScore { get; set; }
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
