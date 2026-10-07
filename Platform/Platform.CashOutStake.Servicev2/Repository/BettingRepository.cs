using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Platform.Shared.Calculators;
using Platform.Shared.Data.Betting;
using Platform.Shared.Extensions;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Collections.Concurrent;
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
        private readonly ILogger<BettingRepository> _logger;

        public List<BetMemory> _bets = new List<BetMemory>();
        public List<Tuple<long, List<Guid>>> _fixtureBetReference = new List<Tuple<long, List<Guid>>>();

        private readonly IMongoDatabase _db;
        private IMongoCollection<Market> marketCollection;
        private IMongoCollection<Selection> selectionCollection;

        // GM-901: a failed market/selection lookup (Mongo unreachable) used to leave nothing
        // cached, so every later message for the same bet hit Mongo again and blocked the
        // RabbitMQ consumer for the full server-selection timeout. These track the last failure
        // per id so PreloadMarket/PreloadSelection can skip retrying until the backoff elapses.
        // A market/selection Mongo doesn't have is backed off the same way.
        private readonly ConcurrentDictionary<long, DateTime> _failedMarketLookups = new ConcurrentDictionary<long, DateTime>();
        private readonly ConcurrentDictionary<long, DateTime> _failedSelectionLookups = new ConcurrentDictionary<long, DateTime>();
        private static readonly TimeSpan FailedLookupBackoff = TimeSpan.FromSeconds(30);

        public BettingRepository(IServiceProvider serviceProvider, IConfiguration configuration, IMemCache memCache, ILogger<BettingRepository> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _memCache = memCache;
            _logger = logger;

            var mongoConnectionString = _configuration["ConnectionStrings:MongoDB-Sportsbook"];

            if (string.IsNullOrWhiteSpace(mongoConnectionString))
                throw new InvalidOperationException("ConnectionStrings:MongoDB-Sportsbook is not configured.");

            var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);

            // GM-901: the driver's default server-selection timeout is 30s, so every lookup
            // against an unreachable Mongo blocked the RabbitMQ consumer for 30s. Only apply the
            // shorter default when the connection string doesn't already specify one, so a
            // caller (e.g. a test) can still opt into a different value via the URI.
            if (!mongoConnectionString.Contains("serverSelectionTimeoutMS", StringComparison.OrdinalIgnoreCase))
                mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            var client = new MongoClient(mongoSettings);
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

            var bets = context.Bet.AsNoTracking().Where(c => c.BetTypeId != (int)BetType.BetBuilder && !betPriceBoosts.Contains(c.BetId) && c.BetStatusId == (int)BetStatus.Running && c.IsFreeBet == false).Select(s => new BetMemory
            {
                BetId = s.BetId,
                ClientCode = s.ClientCode,
                ClientId = s.ClientId,
                UserId = s.UserId,
                BetStatusId = s.BetStatusId,
                TrueStake = s.TrueStake,
                Stake = s.Stake,
                StakeBonus = s.StakeBonus,
                Odds = s.Odds,
                StakeTax = s.StakeTax,
                WinningsTax = s.WinningsTax,
                OddsTypeId = s.OddsTypeId,
                IsPartialCashout = s.IsPartialCashout
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

                // GM-864: this used to be `return`, which stopped preloading the entire bet
                // list at the first excluded bet - every running bet after it in the query got
                // no cashout offer at all until re-added by odds change or replacement.
                if (betSelections.Count == 0 || betSelections.Any(c => c.SportId.IsVirtualSportId() || c.FixtureTypeId == (int)FixtureType.Outright)) //|| c.BetSelectionStatusId != (int)BetStatus.Running
                    continue;

                bet.BetSelections.AddRange(betSelections);

                if (bet.IsPartialCashout)
                {
                    var partialCashoutAudit = context.PartialCashoutAudit.AsNoTracking().Where(c => c.BetId == bet.BetId).Select(s => new PartialCashoutAuditMemory
                    {
                        BetId = bet.BetId,
                        CashoutAmount = s.CashoutAmount,
                        CashoutDate = s.CashoutDate,
                        CashoutPercent = s.CashoutPercent,
                        OriginalStake = s.OriginalStake,
                        PostStake = s.PostStake,
                        PostTrueStake = s.PostTrueStake,
                        PreStake = s.PreStake,
                        PreTrueStake = s.PreTrueStake,
                        StakeUsed = s.StakeUsed,
                        TrueStake = s.TrueStake,
                        TrueStakeUsed = s.TrueStakeUsed,
                    }).OrderBy(o => o.CashoutDate).ToList();

                    if (partialCashoutAudit.Any())
                        bet.PartialCashoutAudit.AddRange(partialCashoutAudit);

                }

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
            if (message.Bet.BetTypeId == (int)BetType.BetBuilder)
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
                StakeBonus = message.Bet.StakeBonus,
                Odds = message.Bet.Odds,
                StakeTax = message.Bet.StakeTax,
                WinningsTax = message.Bet.WinningsTax,
                OddsTypeId = message.Bet.OddsTypeId
            };

            var betSelections = message.BetSelections.Select(s => s.BetSelection).Select(s => new BetSelectionMemory
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

            if (betSelections.Count == 0 || betSelections.Any(c => c.SportId.IsVirtualSportId() || c.FixtureTypeId == (int)FixtureType.Outright)) // || c.BetSelectionStatusId != (int)BetStatus.Running
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
                _logger.LogInformation("{action} - {betId} - {result}", "Add Bet", bet.BetId, true);
            else
                _logger.LogInformation("{action} - {betId} - {result}", "Add Bet", bet.BetId, false);
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
                    _logger.LogInformation("{action} - {betId} - {result}", "Remove Bet", message.Bet.BetId, false);
                else
                    _logger.LogInformation("{action} - {betId} - {result}", "Remove Bet", message.Bet.BetId, true);
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

        public void ProcessPartialCashOut(OnBetSettlementPartial message, IMessageBroker messageBroker)
        {
            var bet = _bets.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

            if (bet != null)
            {
                var cashoutHistory = bet.PartialCashoutAudit;
                var lastCashout = cashoutHistory.LastOrDefault();

                var cashoutAmount = message.CashoutStake;
                var stakeUsed = message.Stake;
                var trueStakeUsed = message.TrueStake;

                cashoutHistory.Add(new PartialCashoutAuditMemory
                {
                    BetId = bet.BetId,
                    CashoutAmount = cashoutAmount,
                    CashoutDate = DateTime.UtcNow,
                    CashoutPercent = message.CashoutStakePercent,
                    OriginalStake = lastCashout != null ? lastCashout.OriginalStake : bet.Stake,
                    TrueStake = lastCashout != null ? lastCashout.TrueStake : bet.TrueStake,
                    StakeUsed = stakeUsed,
                    PreStake = bet.Stake,
                    PostStake = bet.Stake - stakeUsed,
                    TrueStakeUsed = trueStakeUsed,
                    PreTrueStake = bet.TrueStake,
                    PostTrueStake = bet.TrueStake - trueStakeUsed,
                });

                bet.Stake = bet.Stake - stakeUsed;
                bet.TrueStake = bet.TrueStake - trueStakeUsed;
                bet.IsPartialCashout = true;

                var xmessage = new OnExternalOddsChangeMessage
                {
                    IsSuspended = false,
                };

                foreach (var betSelection in bet.BetSelections)
                {
                    PreloadMarket(betSelection.MarketId, xmessage, true);
                    PreloadSelection(betSelection.MarketId, betSelection.SelectionId, xmessage, true);
                }

                var payload = GetCashOut(bet, xmessage);

                messageBroker.Publish(payload, "OnCashOutChange", $"r.bet.cashoutchange.{bet.ClientCode.ToString().ToLower()}", Guid.NewGuid().ToString());

                _logger.LogInformation("{action} - {@cashoutHistory}", "Process Partial CashOut", cashoutHistory);
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

                _logger.LogInformation("{action} - {@payload}", "OddsChange", payload);

                messageBroker.Publish(payload, "OnCashOutChange", $"r.bet.cashoutchange.{bet.ClientCode.ToString().ToLower()}", Guid.NewGuid().ToString());
            }
        }

        // Mongo lookups behind a virtual seam so tests can drive the failure and not-found paths
        // without a live Mongo.
        protected virtual CashOutMarketFilter FindMarket(long marketId)
        {
            var filter = Builders<Market>.Filter.Eq("MaId", marketId);
            var projection = Builders<Market>.Projection.Include(c => c.Id).Include(c => c.MaId).Include(c => c.MaStId);
            return marketCollection.Find(filter).Project<CashOutMarketFilter>(projection).FirstOrDefault();
        }
        protected virtual CashOutSelectionFilter FindSelection(long selectionId)
        {
            var filter = Builders<Selection>.Filter.Eq("SeId", selectionId);
            var projection = Builders<Selection>.Projection.Include(c => c.Id).Include(c => c.SeId).Include(c => c.MaId).Include(c => c.MaStId).Include(c => c.IsAc).Include(c => c.Pr).Include(c => c.WoPr).Include(c => c.LoPr).Include(c => c.VoPr).Include(c => c.HaWoPr).Include(c => c.HaLoPr).Include(c => c.SeStId);
            return selectionCollection.Find(filter).Project<CashOutSelectionFilter>(projection).FirstOrDefault();
        }
        private void PreloadMarket(long marketId, OnExternalOddsChangeMessage message, bool isPartial = false)
        {
            var expiration = DateTime.UtcNow.AddHours(6);

            var marketKey = $"market:{marketId}";

            var marketResult = _memCache.GetCache<CashOutMarketFilter>(marketKey);

            if (marketResult == null)
            {
                if (_failedMarketLookups.TryGetValue(marketId, out var lastFailure) && DateTime.UtcNow - lastFailure < FailedLookupBackoff)
                    return;

                CashOutMarketFilter result;

                try
                {
                    result = FindMarket(marketId);
                }
                catch (Exception ex)
                {
                    _failedMarketLookups[marketId] = DateTime.UtcNow;
                    _logger.LogError(ex, "Failed to preload market {MarketId} from MongoDB", marketId);
                    return;
                }

                // MemCache cannot store null (MemoryCache.Set throws), so a market Mongo doesn't
                // have is backed off like a failure but logged as what it is.
                if (result == null)
                {
                    _failedMarketLookups[marketId] = DateTime.UtcNow;
                    _logger.LogWarning("Market {MarketId} not found in MongoDB", marketId);
                    return;
                }

                _memCache.AddOrUpdateCache(marketKey, result, expiration);
                _failedMarketLookups.TryRemove(marketId, out _);
            }
            else if (isPartial == false)
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
        private void PreloadSelection(long marketId, long selectionId, OnExternalOddsChangeMessage message, bool isPartial = false)
        {
            var expiration = DateTime.UtcNow.AddHours(6);

            var selectionKey = $"selection:{selectionId}";

            var selectionResult = _memCache.GetCache<CashOutSelectionFilter>(selectionKey);

            if (selectionResult == null)
            {
                if (_failedSelectionLookups.TryGetValue(selectionId, out var lastFailure) && DateTime.UtcNow - lastFailure < FailedLookupBackoff)
                    return;

                CashOutSelectionFilter result;

                try
                {
                    result = FindSelection(selectionId);
                }
                catch (Exception ex)
                {
                    _failedSelectionLookups[selectionId] = DateTime.UtcNow;
                    _logger.LogError(ex, "Failed to preload selection {SelectionId} from MongoDB", selectionId);
                    return;
                }

                if (result == null)
                {
                    _failedSelectionLookups[selectionId] = DateTime.UtcNow;
                    _logger.LogWarning("Selection {SelectionId} not found in MongoDB", selectionId);
                    return;
                }

                _memCache.AddOrUpdateCache(selectionKey, result, expiration);
                _failedSelectionLookups.TryRemove(selectionId, out _);
            }
            else if (isPartial == false)
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
            // The stake/tax breakdown and the value formula itself now live in
            // CashOutCalculator (Platform.Shared) - see the call below. This method only
            // determines eligibility and collects each selection's probability.
            var probabilites = new List<CashOutSelectionInput>();
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

                probabilites.Add(new CashOutSelectionInput
                {
                    BetSelectionId = betSelection.BetSelectionId,
                    Probability = selectionResult.Pr,
                    WonProbability = selectionResult.WoPr,
                    LostProbability = selectionResult.LoPr,
                    VoidProbability = selectionResult.VoPr,
                    HalfWonProbability = selectionResult.HaWoPr,
                    HalfLostProbability = selectionResult.HaLoPr,
                    BetSelectionStatusId = betSelection.BetSelectionStatusId,
                    Odds = betSelection.Odds,
                    DeadHeatFactor = betSelection.DeadHeatFactor,
                    IsBinary = betSelection.IsBinary
                });
            }

            var calculation = CashOutCalculator.Calculate(new CashOutCalculationInput
            {
                TrueStake = bet.TrueStake,
                Stake = bet.Stake,
                StakeBonus = bet.StakeBonus,
                StakeTax = bet.StakeTax,
                WinningsTax = bet.WinningsTax,
                OddsTypeId = bet.OddsTypeId,
                Odds = bet.Odds,
                HasProbabilityChanged = hasProbabilityChanged,
                Selections = probabilites
            });

            if (calculation.CashOutStatus != CashOutStatus.Available)
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

            return new OnCashOutChange
            {
                ClientId = bet.ClientId,
                UserId = bet.UserId,
                BetId = bet.BetId.ToString().ToUpper(),
                CashOutStatus = CashOutStatus.Available,
                // Preserves this service's existing (pre-GM-866) rounding behaviour - trims
                // trailing zeros via a string round-trip - unchanged, since GM-866 did not
                // flag this and Betting.API does not do it, so unifying it was not in scope.
                CashOutStake = Convert.ToDecimal(calculation.CashOutStake.ToString().TrimEnd('0').TrimEnd('.'))
            };
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
            PartialCashoutAudit = new List<PartialCashoutAuditMemory>();
        }

        public Guid BetId { get; set; }
        public string ClientCode { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public int BetStatusId { get; set; }
        public decimal TrueStake { get; set; }
        public decimal Stake { get; set; }
        public decimal StakeBonus { get; set; }
        public decimal Odds { get; set; }
        public decimal StakeTax { get; set; }
        public decimal WinningsTax { get; set; }
        public int OddsTypeId { get; set; }
        public bool IsPartialCashout { get; set; }
        public List<BetSelectionMemory> BetSelections { get; set; }
        public List<PartialCashoutAuditMemory> PartialCashoutAudit { get; set; }
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

    public class PartialCashoutAuditMemory
    {
        public Guid BetId { get; set; }
        public decimal OriginalStake { get; set; }
        public decimal StakeUsed { get; set; }
        public decimal PreStake { get; set; }
        public decimal PostStake { get; set; }
        public decimal TrueStake { get; set; }
        public decimal TrueStakeUsed { get; set; }
        public decimal PreTrueStake { get; set; }
        public decimal PostTrueStake { get; set; }
        public decimal CashoutPercent { get; set; }
        public decimal CashoutAmount { get; set; }
        public DateTime CashoutDate { get; set; }
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
    public class OnExternalBetSettlementMessage
    {
        public long MarketId { get; set; }
        public long SelectionId { get; set; }
        public BetStatus SelectionStatus { get; set; }
        public decimal DeadHeatFactor { get; set; }
    }
}
