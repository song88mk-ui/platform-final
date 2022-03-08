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
        private IMongoCollection<Market> marketCollection;
        private IMongoCollection<Selection> selectionCollection;

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
            marketCollection = _db.GetCollection<Market>("Market");
            selectionCollection = _db.GetCollection<Selection>("Selection");
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
                WinningsTax = s.WinningsTax
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
                WinningsTax = message.Bet.WinningsTax
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
                            //call operator api
                        }

                        //remove _fixtureBetReference
                        if(fixtureResult.FiStId == (int)FixtureStatus.Closed || fixtureResult.FiStId == (int)FixtureStatus.Ended)
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


        private void PreloadMarket(long marketId, OnExternalOddsChangeMessage message)
        {
            var expiration = DateTime.UtcNow.AddHours(6);

            var marketKey = $"market:{marketId}";

            var marketResult = _memCache.GetCache<CashOutMarketFilter>(marketKey);

            if (marketResult == null)
            {
                var filter = Builders<Market>.Filter.Eq("MaId", marketId);
                var projection = Builders<Market>.Projection.Include(c => c.MaId).Include(c => c.MaStId);
                var result = marketCollection.Find(filter).Project<CashOutMarketFilter>(projection).FirstOrDefault();

                _memCache.AddOrUpdateCache(marketKey, result, expiration);
            }
            else
            {
                var market = message.Markets.Where(c => c.MarketId == marketId).FirstOrDefault();

                if (market != null)
                {
                    marketResult.MaStId = (int)market.MarketStatus;

                    _memCache.AddOrUpdateCache(marketKey, marketResult, expiration);
                }
            }
        }

        private void PreloadSelection(long marketId, long selectionId, OnExternalOddsChangeMessage message)
        {
            var expiration = DateTime.UtcNow.AddHours(6);

            var selectionKey = $"selection:{selectionId}";

            var selectionResult = _memCache.GetCache<CashOutSelectionFilter>(selectionKey);

            if (selectionResult == null)
            {
                var filter = Builders<Selection>.Filter.Eq("SeId", selectionId);
                var projection = Builders<Selection>.Projection.Include(c => c.SeId).Include(c => c.MaId).Include(c => c.MaStId).Include(c => c.IsAc).Include(c => c.Pr).Include(c => c.WoPr).Include(c => c.LoPr).Include(c => c.VoPr).Include(c => c.HaWoPr).Include(c => c.HaLoPr);
                var result = selectionCollection.Find(filter).Project<CashOutSelectionFilter>(projection).FirstOrDefault();

                _memCache.AddOrUpdateCache(selectionKey, result, expiration);
            }
            else
            {
                var market = message.Markets.Where(c => c.MarketId == marketId).FirstOrDefault();

                if (market != null)
                {
                    var selection = market.Selections.Where(c => c.SelectionId == selectionId).FirstOrDefault();

                    if (selection != null)
                    {
                        selectionResult.IsAc = selection.IsActive;
                        selectionResult.Pr = selection.Probability;
                        selectionResult.WoPr = selection.WonProbability;
                        selectionResult.LoPr = selection.LostProbability;
                        selectionResult.VoPr = selection.VoidProbability;
                        selectionResult.HaWoPr = selection.HalfWonProbability;
                        selectionResult.HaLoPr = selection.HalfLostProbability;

                        _memCache.AddOrUpdateCache(selectionKey, selectionResult, expiration);
                    }
                }
            }
        }

        //private OnCashOutChange GetCashOut(BetMemory bet, OnExternalOddsChangeMessage message)
        //{
        //    var taxedStake = bet.TrueStake - (bet.TrueStake / (1 + bet.StakeTax));
        //    var stake = bet.TrueStake - taxedStake;

        //    var probabilites = new List<Probabilities>();
        //    var hasProbabilityChanged = false;

        //    if (message.IsSuspended == true)
        //    {
        //        return new OnCashOutChange
        //        {
        //            ClientId = bet.ClientId,
        //            UserId = bet.UserId,
        //            BetId = bet.BetId.ToString().ToUpper(),
        //            CashOutStatus = CashOutStatus.Unavailable,
        //            CashOutStake = 0m
        //        };
        //    }

        //    foreach (var betSelection in bet.BetSelections)
        //    {
        //        var marketResult = _memCache.GetCache<CashOutMarketFilter>($"market:{betSelection.MarketId}");
        //        var selectionResult = _memCache.GetCache<CashOutSelectionFilter>($"selection:{betSelection.SelectionId}");

        //        if (marketResult == null || selectionResult == null || marketResult.MaStId != (int)MarketStatus.Active || selectionResult.IsAc == false)
        //        {
        //            return new OnCashOutChange
        //            {
        //                ClientId = bet.ClientId,
        //                UserId = bet.UserId,
        //                BetId = bet.BetId.ToString().ToUpper(),
        //                CashOutStatus = CashOutStatus.Unavailable,
        //                CashOutStake = 0m
        //            };
        //        }

        //        if (betSelection.Probability != selectionResult.Pr)
        //            hasProbabilityChanged = true;

        //        probabilites.Add(new Probabilities
        //        {
        //            BetSelectionId = betSelection.BetSelectionId,
        //            CashOutStatusId = 1,
        //            Probability = selectionResult.Pr,
        //            WonProbability = selectionResult.WoPr,
        //            LostProbability = selectionResult.LoPr,
        //            VoidProbability = selectionResult.VoPr,
        //            HalfWonProbability = selectionResult.HaWoPr,
        //            HalfLostProbability = selectionResult.HaLoPr
        //        });
        //    }

        //    if (bet.BetSelections.Count > 1)
        //    {
        //        var finalProbability = probabilites.Where(c => c.Probability > 0).Select(s => s.Probability).Aggregate((a, b) => a * b);

        //        var cashOutStake = (stake * bet.Odds) * Convert.ToDecimal(finalProbability);

        //        cashOutStake = cashOutStake - (cashOutStake * 0.10m);

        //        var taxedWinnings = (cashOutStake - stake) * bet.WinningsTax;

        //        if (cashOutStake > stake)
        //            cashOutStake = cashOutStake - taxedWinnings;
        //        else
        //            taxedWinnings = 0;

        //        var finalCashOutStake = cashOutStake;

        //        if (!hasProbabilityChanged)
        //            finalCashOutStake = stake - (stake * 0.10m);

        //        if (finalCashOutStake == 0 || finalCashOutStake > stake * bet.Odds)
        //        {
        //            return new OnCashOutChange
        //            {
        //                ClientId = bet.ClientId,
        //                UserId = bet.UserId,
        //                BetId = bet.BetId.ToString().ToUpper(),
        //                CashOutStatus = CashOutStatus.Unavailable,
        //                CashOutStake = 0m
        //            };
        //        }
        //        else
        //        {
        //            return new OnCashOutChange
        //            {
        //                ClientId = bet.ClientId,
        //                UserId = bet.UserId,
        //                BetId = bet.BetId.ToString().ToUpper(),
        //                CashOutStatus = CashOutStatus.Available,
        //                CashOutStake = Convert.ToDecimal(finalCashOutStake.ToString().TrimEnd('0').TrimEnd('.'))
        //            };
        //        }
        //    }
        //    else
        //    {
        //        decimal cashOutStake = 0m;

        //        if (probabilites.First().WonProbability > 0 ||
        //            probabilites.First().LostProbability > 0 ||
        //            probabilites.First().VoidProbability > 0 ||
        //            probabilites.First().HalfWonProbability > 0 ||
        //            probabilites.First().HalfLostProbability > 0)
        //        {
        //            var won = Convert.ToDecimal(probabilites.First().WonProbability) * (stake * bet.Odds);
        //            var lost = Convert.ToDecimal(probabilites.First().LostProbability) * (stake * 0);
        //            var voidd = Convert.ToDecimal(probabilites.First().VoidProbability) * (stake * 1);
        //            var halfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * (stake * ((bet.Odds + 1) / 2));
        //            var halfLost = Convert.ToDecimal(probabilites.First().HalfLostProbability) * (stake / 2);

        //            cashOutStake = won + lost + voidd + halfWon + halfLost;
        //        }
        //        else
        //            cashOutStake = (stake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);

        //        cashOutStake = cashOutStake - (cashOutStake * 0.10m);

        //        var taxedWinnings = (cashOutStake - stake) * bet.WinningsTax;

        //        if (cashOutStake > stake)
        //            cashOutStake = cashOutStake - taxedWinnings;
        //        else
        //            taxedWinnings = 0;

        //        var finalCashOutStake = cashOutStake;

        //        if (!hasProbabilityChanged)
        //            finalCashOutStake = stake - (stake * 0.10m);

        //        if (finalCashOutStake <= stake * bet.Odds)
        //        {
        //            return new OnCashOutChange
        //            {
        //                ClientId = bet.ClientId,
        //                UserId = bet.UserId,
        //                BetId = bet.BetId.ToString().ToUpper(),
        //                CashOutStatus = CashOutStatus.Available,
        //                CashOutStake = Convert.ToDecimal(finalCashOutStake.ToString().TrimEnd('0').TrimEnd('.'))
        //            };
        //        }
        //        else
        //        {
        //            return new OnCashOutChange
        //            {
        //                ClientId = bet.ClientId,
        //                UserId = bet.UserId,
        //                BetId = bet.BetId.ToString().ToUpper(),
        //                CashOutStatus = CashOutStatus.Unavailable,
        //                CashOutStake = 0m
        //            };
        //        }
        //    }
        //}
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
