using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;

namespace Platform.CashOutStake.Servicev2.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;

        public List<BetMemory> _bets = new List<BetMemory>();
        public List<Tuple<long, List<Guid>>> _fixtureBetReference = new List<Tuple<long, List<Guid>>>();

        private readonly IMongoDatabase _db;
        private IMongoCollection<Market> marketCollection;
        private IMongoCollection<Selection> selectionCollection;

        public BettingRepository(IServiceProvider serviceProvider, IConfiguration configuration, IMemCache memCache)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _memCache = memCache;

            var client = new MongoClient(_configuration["ConnectionStrings:MongoDB-Sportsbook"]);
            _db = client.GetDatabase("Sportsbook");

            marketCollection = _db.GetCollection<Market>("Market");
            selectionCollection = _db.GetCollection<Selection>("Selection");
        }

        public void PreloadActiveBets()
        {
            _bets = new List<BetMemory>();
            _fixtureBetReference = new List<Tuple<long, List<Guid>>>();

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BettingContext>();

            var betPriceBoosts = context.BetPriceBoost.AsNoTracking().Where(c => c.BetStatusId == (int)BetStatus.Running).Select(s => s.BetId).ToList();

            var bets = context.Bet.AsNoTracking().Where(c => !betPriceBoosts.Contains(c.BetId) && c.BetStatusId == (int)BetStatus.Running && c.IsFreeBet == false).Select(s => new BetMemory
            {
                BetId = s.BetId,
                ClientCode = s.ClientCode,
                ClientId = s.ClientId,
                UserId = s.UserId,
                BetStatusId = s.BetStatusId,
                TrueStake = s.TrueStake,
                Stake = s.Stake,
                Odds = s.Odds,
                StakeTax = s.StakeTax,
                WinningsTax = s.WinningsTax,
                OddsTypeId = s.OddsTypeId
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
                    Probability = s.Probability,
                    WonProbability = s.WonProbability,
                    LostProbability = s.LostProbability,
                    VoidProbability = s.VoidProbability,
                    HalfWonProbability = s.HalfWonProbability,
                    HalfLostProbability = s.HalfLostProbability,
                    Odds = s.Odds,
                    DeadHeatFactor = s.DeadHeatFactor,
                    IsBinary = s.IsBinary
                }).ToList();

                if (betSelections.Count == 0 || betSelections.Any(c => c.SportId > 1000 || c.FixtureTypeId == (int)FixtureType.Outright)) //|| c.BetSelectionStatusId != (int)BetStatus.Running
                    return;

                bet.BetSelections.AddRange(betSelections);

                _bets.Add(bet);

                foreach (var betSelection in betSelections.Where(s => s.BetSelectionStatusId == (int)BetStatus.Running).ToList())
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
            if (message.Bet.IsFreeBet == true)
                return;
            if (message.IsBoosted == true)
                return;

            var bet = new BetMemory
            {
                BetId = message.Bet.BetId,
                ClientCode = message.Bet.ClientCode,
                ClientId = message.Bet.ClientId,
                UserId = message.Bet.UserId,
                BetStatusId = message.Bet.BetStatusId,
                TrueStake = message.Bet.TrueStake,
                Stake = message.Bet.Stake,
                Odds = message.Bet.Odds,
                StakeTax = message.Bet.StakeTax,
                WinningsTax = message.Bet.WinningsTax,
                OddsTypeId = message.Bet.OddsTypeId
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
                Probability = s.Probability,
                WonProbability = s.WonProbability,
                LostProbability = s.LostProbability,
                VoidProbability = s.VoidProbability,
                HalfWonProbability = s.HalfWonProbability,
                HalfLostProbability = s.HalfLostProbability,
                Odds = s.Odds,
                DeadHeatFactor = s.DeadHeatFactor,
                IsBinary = s.IsBinary
            }).ToList();

            if (betSelections.Count == 0 || betSelections.Any(c => c.SportId > 1000 || c.FixtureTypeId == (int)FixtureType.Outright)) // || c.BetSelectionStatusId != (int)BetStatus.Running
                return;

            bet.BetSelections.AddRange(betSelections);

            _bets.Add(bet);

            foreach (var betSelection in betSelections.Where(s => s.BetSelectionStatusId == (int)BetStatus.Running).ToList())
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
        public void RemoveBet(OnBetSettlement message)
        {
            var bet = _bets.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

            if (bet != null)
            {
                foreach (var betSelection in bet.BetSelections)
                {
                    var fixtureBetReference = _fixtureBetReference.Where(c => c.Item1 == betSelection.FixtureId).FirstOrDefault();

                    if (fixtureBetReference != null)
                    {
                        var fixtureEntry = fixtureBetReference.Item1;
                        var betEntries = fixtureBetReference.Item2.Where(c => c != message.Bet.BetId).ToList();

                        if (betEntries.Count == 0)
                            _fixtureBetReference.Remove(fixtureBetReference);
                        else
                        {
                            _fixtureBetReference.Remove(fixtureBetReference);
                            _fixtureBetReference.Add(new Tuple<long, List<Guid>>(fixtureEntry, betEntries));
                        }
                    }
                }

                _bets = _bets.Where(c => c.BetId != message.Bet.BetId).ToList();

                if (_bets.Any(c => c.BetId == message.Bet.BetId))
                    Console.WriteLine($"{DateTime.UtcNow} - Remove Bet : {message.Bet.BetId.ToString().ToUpper()} : False");
                else
                    Console.WriteLine($"{DateTime.UtcNow} - Remove Bet : {message.Bet.BetId.ToString().ToUpper()} : True");
            }
        }
        public void ProcessOddsChange(OnExternalOddsChangeMessage message, IMessageBroker messageBroker)
        {
            var fixtureBetReference = _fixtureBetReference.Where(c => c.Item1 == message.FixtureId).FirstOrDefault();

            if (fixtureBetReference != null)
            {
                foreach (var bet in fixtureBetReference.Item2)
                    ProcessCashOut(bet, message, messageBroker);
            }
        }

        private void ProcessCashOut(Guid betId, OnExternalOddsChangeMessage message, IMessageBroker messageBroker)
        {
            var bet = _bets.Where(c => c.BetId == betId).FirstOrDefault();

            if (bet != null)
            {
                foreach (var betSelection in bet.BetSelections)
                {
                    PreloadMarket(betSelection.MarketId, message);
                    PreloadSelection(betSelection.MarketId, betSelection.SelectionId, message);
                }

                var payload = GetCashOut(bet, message);

                Console.WriteLine($"{DateTime.UtcNow} - OddsChange : {JsonSerializer.Serialize(payload)}");

                //TODO: delete when TW is no longer the frontend
                messageBroker.Publish(payload, "OnCashOutChange", $"r.bet.betstatus", Guid.NewGuid().ToString());

                messageBroker.Publish(payload, "OnCashOutChange", $"r.bet.cashoutchange.{bet.ClientCode.ToString().ToLower()}", Guid.NewGuid().ToString());
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
        private void UpdateMarketCache(OnExternalBetSettlementMessage message)
        {
            var expiration = DateTime.UtcNow.AddHours(6);

            var marketKey = $"market:{message.MarketId}";

            var marketResult = _memCache.GetCache<CashOutMarketFilter>(marketKey);

            if (marketResult != null)
            {
                marketResult.MaStId = (int)MarketStatus.Settled;

                _memCache.AddOrUpdateCache(marketKey, marketResult, expiration);
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
                var projection = Builders<Selection>.Projection.Include(c => c.SeId).Include(c => c.MaId).Include(c => c.MaStId).Include(c => c.IsAc).Include(c => c.Pr).Include(c => c.WoPr).Include(c => c.LoPr).Include(c => c.VoPr).Include(c => c.HaWoPr).Include(c => c.HaLoPr).Include(c => c.SeStId);
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
        private void UpdateSelectionCache(OnExternalBetSettlementMessage message)
        {
            var expiration = DateTime.UtcNow.AddHours(6);

            var selectionKey = $"selection:{message.SelectionId}";

            var selectionResult = _memCache.GetCache<CashOutSelectionFilter>(selectionKey);

            if (selectionResult != null)
            {
                var validStatus = new List<BetStatus> { BetStatus.Won, BetStatus.HalfWon, BetStatus.HalfLost, BetStatus.Void };

                if (validStatus.Contains(message.SelectionStatus))
                {
                    selectionResult.Pr = 1;

                    _memCache.AddOrUpdateCache(selectionKey, selectionResult, expiration);
                }
            }
        }
        private OnCashOutChange GetCashOut(BetMemory bet, OnExternalOddsChangeMessage message)
        {
            var taxedStake = bet.TrueStake - (bet.TrueStake / (1 + bet.StakeTax));
            var stake = bet.TrueStake - taxedStake;

            var probabilites = new List<Probabilities>();
            var hasProbabilityChanged = false;

            if (message.IsSuspended == true)
            {
                return new OnCashOutChange
                {
                    ClientId = bet.ClientId,
                    UserId = bet.UserId,
                    BetId = bet.BetId.ToString().ToUpper(),
                    CashOutStatus = CashOutStatus.Unavailable,
                    CashOutStake = 0m
                };
            }

            foreach (var betSelection in bet.BetSelections)
            {
                var marketResult = _memCache.GetCache<CashOutMarketFilter>($"market:{betSelection.MarketId}");
                var selectionResult = _memCache.GetCache<CashOutSelectionFilter>($"selection:{betSelection.SelectionId}");

                if (marketResult == null 
                    || selectionResult == null 
                    || selectionResult.SeStId == (int)BetStatus.Lost
                    || selectionResult.SeStId == (int)BetStatus.Cancelled
                    || (selectionResult.IsAc == false && selectionResult.MaStId == (int)MarketStatus.Active)
                    || (message.IsSuspended == true && message.FixtureStatusId < 2))
                {
                    return new OnCashOutChange
                    {
                        ClientId = bet.ClientId,
                        UserId = bet.UserId,
                        BetId = bet.BetId.ToString().ToUpper(),
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    };
                }

                if (betSelection.Probability != selectionResult.Pr)
                    hasProbabilityChanged = true;

                probabilites.Add(new Probabilities
                {
                    BetSelectionId = betSelection.BetSelectionId,
                    CashOutStatusId = 1,
                    Probability = selectionResult.Pr,
                    WonProbability = selectionResult.WoPr,
                    LostProbability = selectionResult.LoPr,
                    VoidProbability = selectionResult.VoPr,
                    HalfWonProbability = selectionResult.HaWoPr,
                    HalfLostProbability = selectionResult.HaLoPr,
                    BetSelectionStatusId = betSelection.BetSelectionStatusId
                });
            }

            if (bet.BetSelections.Count > 1)
            {
                var parlayProbabilities = new List<Probabilities>();
                var validStatus = new List<int> { (int)BetStatus.Won, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost, (int)BetStatus.Void };

                foreach (var probability in probabilites)
                {
                    if (validStatus.Contains(probability.BetSelectionStatusId))
                    {
                        probability.Probability = 1;
                        parlayProbabilities.Add(probability);
                    }
                    else
                        parlayProbabilities.Add(probability);
                }

                var finalProbability = parlayProbabilities.Where(c => c.Probability > 0).Select(s => s.Probability).Aggregate((a, b) => a * b);
                var totalOdds = GetTotalOdds(bet.BetSelections.Select(ss => new Tuple<BetStatus, decimal, decimal>((BetStatus)ss.BetSelectionStatusId, ss.Odds, ss.DeadHeatFactor)).ToList());

                var cashOutStake = (stake * totalOdds) * Convert.ToDecimal(finalProbability);

                cashOutStake = cashOutStake - (cashOutStake * 0.10m);

                var taxedWinnings = (cashOutStake - stake) * bet.WinningsTax;

                if (cashOutStake > stake)
                    cashOutStake = cashOutStake - taxedWinnings;
                else
                    taxedWinnings = 0;

                var finalCashOutStake = cashOutStake;

                if (!hasProbabilityChanged)
                    finalCashOutStake = stake - (stake * 0.10m);

                if (finalCashOutStake == 0 || finalCashOutStake > stake * totalOdds)
                {
                    return new OnCashOutChange
                    {
                        ClientId = bet.ClientId,
                        UserId = bet.UserId,
                        BetId = bet.BetId.ToString().ToUpper(),
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    };
                }
                else
                {
                    return new OnCashOutChange
                    {
                        ClientId = bet.ClientId,
                        UserId = bet.UserId,
                        BetId = bet.BetId.ToString().ToUpper(),
                        CashOutStatus = CashOutStatus.Available,
                        CashOutStake = Convert.ToDecimal(finalCashOutStake.ToString().TrimEnd('0').TrimEnd('.'))
                    };
                }
            }
            else
            {
                decimal cashOutStake = 0m;

                if (probabilites.First().WonProbability > 0 ||
                    probabilites.First().LostProbability > 0 ||
                    probabilites.First().VoidProbability > 0 ||
                    probabilites.First().HalfWonProbability > 0 ||
                    probabilites.First().HalfLostProbability > 0)
                {
                    var won = 0m;
                    var halfWon = 0m;

                    if (OddsTypeHelper.GetOddsByOddsType(bet.OddsTypeId, bet.BetSelections.First().Odds, bet.BetSelections.First().IsBinary) < 0 && (bet.OddsTypeId == (int)OddsType.Indonesian || bet.OddsTypeId == (int)OddsType.Malaysian))
                    {
                        won = Convert.ToDecimal(probabilites.First().WonProbability) * (stake + bet.Stake);
                        halfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * ((bet.Stake / 2) + stake);
                    }
                    else
                    {
                        won = Convert.ToDecimal(probabilites.First().WonProbability) * (stake * bet.Odds);
                        halfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * (stake * ((bet.Odds + 1) / 2));
                    }

                    var lost = Convert.ToDecimal(probabilites.First().LostProbability) * (stake * 0);
                    var voidd = Convert.ToDecimal(probabilites.First().VoidProbability) * (stake * 1);
                    var halfLost = Convert.ToDecimal(probabilites.First().HalfLostProbability) * (stake / 2);

                    cashOutStake = won + lost + voidd + halfWon + halfLost;
                }
                else
                {
                    if (OddsTypeHelper.GetOddsByOddsType(bet.OddsTypeId, bet.BetSelections.First().Odds, bet.BetSelections.First().IsBinary) < 0 && (bet.OddsTypeId == (int)OddsType.Indonesian || bet.OddsTypeId == (int)OddsType.Malaysian))
                        cashOutStake = (stake + bet.Stake) * Convert.ToDecimal(probabilites.First().Probability);
                    else
                        cashOutStake = (stake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);
                }


                cashOutStake = cashOutStake - (cashOutStake * 0.10m);

                var taxedWinnings = (cashOutStake - stake) * bet.WinningsTax;

                if (cashOutStake > stake)
                    cashOutStake = cashOutStake - taxedWinnings;
                else
                    taxedWinnings = 0;

                var finalCashOutStake = cashOutStake;

                if (!hasProbabilityChanged)
                    finalCashOutStake = stake - (stake * 0.10m);

                if (OddsTypeHelper.GetOddsByOddsType(bet.OddsTypeId, bet.BetSelections.First().Odds, bet.BetSelections.First().IsBinary) < 0 && (bet.OddsTypeId == (int)OddsType.Indonesian || bet.OddsTypeId == (int)OddsType.Malaysian))
                {
                    if (finalCashOutStake <= stake + bet.Stake)
                    {
                        return new OnCashOutChange
                        {
                            ClientId = bet.ClientId,
                            UserId = bet.UserId,
                            BetId = bet.BetId.ToString().ToUpper(),
                            CashOutStatus = CashOutStatus.Available,
                            CashOutStake = Convert.ToDecimal(finalCashOutStake.ToString().TrimEnd('0').TrimEnd('.'))
                        };
                    }
                    else
                    {
                        return new OnCashOutChange
                        {
                            ClientId = bet.ClientId,
                            UserId = bet.UserId,
                            BetId = bet.BetId.ToString().ToUpper(),
                            CashOutStatus = CashOutStatus.Unavailable,
                            CashOutStake = 0m
                        };
                    }
                }
                else
                {
                    if (finalCashOutStake <= stake * bet.Odds)
                    {
                        return new OnCashOutChange
                        {
                            ClientId = bet.ClientId,
                            UserId = bet.UserId,
                            BetId = bet.BetId.ToString().ToUpper(),
                            CashOutStatus = CashOutStatus.Available,
                            CashOutStake = Convert.ToDecimal(finalCashOutStake.ToString().TrimEnd('0').TrimEnd('.'))
                        };
                    }
                    else
                    {
                        return new OnCashOutChange
                        {
                            ClientId = bet.ClientId,
                            UserId = bet.UserId,
                            BetId = bet.BetId.ToString().ToUpper(),
                            CashOutStatus = CashOutStatus.Unavailable,
                            CashOutStake = 0m
                        };
                    }
                }
            }
        }
        private static decimal GetTotalOdds(List<Tuple<BetStatus, decimal, decimal>> selections)
        {
            var values = new List<decimal>();

            foreach (var selection in selections)
            {
                switch (selection.Item1)
                {
                    case BetStatus.Lost:
                        values.Add(0m);
                        break;
                    case BetStatus.HalfWon:
                        values.Add((selection.Item2 + 1) / 2);
                        break;
                    case BetStatus.HalfLost:
                        values.Add(0.5m);
                        break;
                    case BetStatus.Won:
                        values.Add(selection.Item2 * selection.Item3);
                        break;
                    case BetStatus.Void:
                        values.Add(1m);
                        break;
                    case BetStatus.Cancelled:
                        values.Add(1m);
                        break;
                    case BetStatus.Running:
                        values.Add(selection.Item2);
                        break;
                }
            }

            return values.Aggregate((a, b) => a * b);
        }

        public void ProcessOnBetSettlement(OnExternalBetSettlementMessage onBetSettlement, IMessageBroker messageBroker)
        {
            UpdateMarketCache(onBetSettlement);
            UpdateSelectionCache(onBetSettlement);
        }
    }

    public class BetMemory
    {
        public BetMemory()
        {
            BetSelections = new List<BetSelectionMemory>();
        }

        public Guid BetId { get; set; }
        public string ClientCode { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public int BetStatusId { get; set; }
        public decimal TrueStake { get; set; }
        public decimal Stake { get; set; }
        public decimal Odds { get; set; }
        public decimal StakeTax { get; set; }
        public decimal WinningsTax { get; set; }
        public int OddsTypeId { get; set; }
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
        public double Probability { get; set; }
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal Odds { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal DeadHeatFactor { get; set; }
        public bool IsBinary { get; set; }
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
        public int BetSelectionStatusId { get; set; }
    }

    public class OnExternalBetSettlementMessage
    {
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public BetStatus SelectionStatus { get; set; }
        public decimal DeadHeatFactor { get; set; }
    }
}
