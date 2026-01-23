using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.Entities.Enums;
using System;
using System.Linq;
using System.Text.Json;
using System.Transactions;
using System.Threading;
using Platform.Shared.Providers;

namespace Platform.Placement.Servicev2.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly ILogger<BettingRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;

        public BettingRepository(IConfiguration configuration, ILogger<BettingRepository> logger, IMemCache memCache)
        {
            _logger = logger;
            _configuration = configuration;
            _memCache = memCache;
        }

        public void ProcesssBetPlacement(OnBetPlacement message, IBetProvider provider, IMessageBroker messageBroker, string correlationId, int reprocessAttempt = 0)
        {
            var payload = new OnBetPlacement();
            payload.IsBoosted = message.IsBoosted;
            payload.IsBetBuilder = message.IsBetBuilder;
            payload.BonusId = message.BonusId;
            payload.Odds = message.Odds;
            payload.UserAgent = message.UserAgent;
            payload.IsOGClient = message.IsOGClient;
            
            var key = $"dly:{message.Bet.BetId}";
            if(message.DelayedBetAcceptance != null)
            {
                var cache = _memCache.GetCache<OnBetPlacement>(key);
                if (cache == null)
                {
                    _logger.LogError($"Cache not found for {key}");
                    return;
                }
                _memCache.DeleteCache(key);
                var betAcceptance = message.DelayedBetAcceptance;
                message = cache;
                message.DelayedBetAcceptance = betAcceptance;
            }

            var providerResult = provider.ValidateAsync(message, CancellationToken.None).GetAwaiter().GetResult();

            if (providerResult.Acceptance == BetAcceptance.Accepted)
            {
                var isReprocess = false;

                try
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                        using (var context = new BettingContext(optionsBuilder.Options))
                        {
                            var bet = context.Bet.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

                            if (bet != null && bet.BetStatusId == (int)BetStatus.Waiting)
                            {
                                if (message.DelayedBetAcceptance != null)
                                {
                                    payload.IsBoosted = bet.BetTypeId == (int)BetType.PriceBoost;
                                    payload.IsBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder;
                                    payload.BonusId = bet.BonusId;
                                    payload.Odds = bet.Odds;
                                    payload.UserAgent = bet.UserAgent;
                                    payload.IsOGClient = bet.ClientType == ClientType.OG.ToString();
                                }

                                bet.Reason = $"{providerResult.Reason} ({provider.ChannelType.ToString()})";

                                var betEntity = context.Bet.Update(bet).Entity;
                                payload.Bet = betEntity;

                                context.BetAudit.Add(new BetAudit
                                {
                                    BetId = betEntity.BetId,
                                    UserId = betEntity.UserId,
                                    Stake = betEntity.Stake,
                                    TrueStake = betEntity.TrueStake,
                                    Odds = betEntity.Odds,
                                    OddsTypeId = betEntity.OddsTypeId,
                                    OddsTypeName = betEntity.OddsTypeName,
                                    Payout = betEntity.Payout,
                                    WinLoss = betEntity.WinLoss,
                                    BetStatusId = betEntity.BetStatusId,
                                    BetStatusName = betEntity.BetStatusName,
                                    Reason = betEntity.Reason,
                                    CurrencyCode = betEntity.CurrencyCode,
                                    LanguageCode = betEntity.LanguageCode,
                                    ExchangeRate = betEntity.ExchangeRate,
                                    IPAddress = betEntity.IPAddress,
                                    Channel = betEntity.Channel,
                                    ClientId = betEntity.ClientId,
                                    ClientIdRef = betEntity.ClientIdRef,
                                    ClientCode = betEntity.ClientCode,
                                    ClientName = betEntity.ClientName,
                                    LimitId = betEntity.LimitId,
                                    Endpoint = betEntity.Endpoint,
                                    OperatorToken = betEntity.OperatorToken,
                                    CompanyShare = betEntity.CompanyShare,
                                    IsResettled = betEntity.IsResettled,
                                    IsCashout = betEntity.IsCashout,
                                    BetDate = betEntity.BetDate,
                                    BetDateOffset = betEntity.BetDateOffset,
                                    SettlementDate = betEntity.SettlementDate,
                                    SettlementDateOffset = betEntity.SettlementDateOffset,
                                    StakeTax = betEntity.StakeTax,
                                    WinningsTax = betEntity.WinningsTax,
                                    TaxedStake = betEntity.TaxedStake,
                                    TaxedWinnings = betEntity.TaxedWinnings,
                                    ParlayBonus = betEntity.ParlayBonus,
                                    ParlayBonusWinnings = betEntity.ParlayBonusWinnings,
                                    StakeReal = betEntity.StakeReal,
                                    StakeBonus = betEntity.StakeBonus,
                                    StakeRealPayout = betEntity.StakeRealPayout,
                                    StakeBonusPayout = betEntity.StakeBonusPayout,
                                    IsFreeBet = betEntity.IsFreeBet,
                                    BetTypeId = betEntity.BetTypeId,
                                    BetTypeName = betEntity.BetTypeName,
                                    OddsRef = betEntity.OddsRef,
                                    IsAgent = betEntity.IsAgent
                                });

                                var betSelections = context.BetSelection.Where(c => c.BetId == betEntity.BetId).ToList();

                                foreach (var betSelection in betSelections)
                                {
                                    betSelection.BetSelectionStatusId = betEntity.BetStatusId;
                                    betSelection.BetSelectionStatusName = betEntity.BetStatusName;

                                    var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;

                                    var betSelectionExtension = new BetSelectionMessageExtenstion()
                                    {
                                        BetSelection = betSelectionEntity,
                                    };

                                    if(message.DelayedBetAcceptance == null)
                                    {
                                        betSelectionExtension.MarketSelectionCount = message.BetSelections.FirstOrDefault(c => c.BetSelection.BetId == betSelection.BetId && c.BetSelection.BetSelectionId == betSelection.BetSelectionId).MarketSelectionCount;
                                    }

                                    payload.BetSelections.Add(betSelectionExtension);

                                    context.BetSelectionAudit.Add(new BetSelectionAudit
                                    {
                                        BetId = betSelectionEntity.BetId,
                                        BetSelectionId = betSelectionEntity.BetSelectionId,
                                        UserId = betSelectionEntity.UserId,
                                        SportId = betSelectionEntity.SportId,
                                        SportName = betSelectionEntity.SportName,
                                        SegmentId = betSelectionEntity.SegmentId,
                                        SegmentName = betSelectionEntity.SegmentName,
                                        LeagueId = betSelectionEntity.LeagueId,
                                        LeagueName = betSelectionEntity.LeagueName,
                                        FixtureId = betSelectionEntity.FixtureId,
                                        FixtureName = betSelectionEntity.FixtureName,
                                        FixtureTypeId = betSelectionEntity.FixtureTypeId,
                                        FixtureTypeName = betSelectionEntity.FixtureTypeName,
                                        MarketTemplateId = betSelectionEntity.MarketTemplateId,
                                        MarketId = betSelectionEntity.MarketId,
                                        MarketName = betSelectionEntity.MarketName,
                                        SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
                                        SelectionId = betSelectionEntity.SelectionId,
                                        SelectionName = betSelectionEntity.SelectionName,
                                        FixtureStatusId = betSelectionEntity.FixtureStatusId,
                                        FixtureStatusName = betSelectionEntity.FixtureStatusName,
                                        Line = betSelectionEntity.Line,
                                        Odds = betSelectionEntity.Odds,
                                        OddsRef = betSelectionEntity.OddsRef,
                                        BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                                        BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                                        IsSettled = betSelectionEntity.IsSettled,
                                        IsResettled = betSelectionEntity.IsResettled,
                                        FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
                                        SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
                                        FixtureStartDate = betSelectionEntity.FixtureStartDate,
                                        BetScore = betSelectionEntity.BetScore,
                                        SettlementScore = betSelectionEntity.SettlementScore,
                                        Probability = betSelectionEntity.Probability,
                                        WonProbability = betSelectionEntity.WonProbability,
                                        LostProbability = betSelectionEntity.LostProbability,
                                        VoidProbability = betSelectionEntity.VoidProbability,
                                        HalfWonProbability = betSelectionEntity.HalfWonProbability,
                                        HalfLostProbability = betSelectionEntity.HalfLostProbability,
                                        CorrelationId = betSelectionEntity.CorrelationId,
                                        DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                                        IsBinary = betSelectionEntity.IsBinary
                                    });
                                }

                                var contextResult = context.SaveChanges();

                                //if (contextResult == 0)
                                //    Console.WriteLine($"{DateTime.UtcNow} - Not Saved - Accepted - {message.Bet.BetId.ToString().ToUpper()}");
                                //else
                                //    Console.WriteLine($"{DateTime.UtcNow} - Saved - Accepted - {message.Bet.BetId.ToString().ToUpper()}");

                                scope.Complete();

                                messageBroker.Publish(payload, "OnBetPlacement", "r.bet.operator", correlationId, 10);
                            }
                            else
                            {
                                isReprocess = true;
                                //Console.WriteLine($"{DateTime.UtcNow} - Bet Not Found - Accepted - {message.Bet.BetId.ToString().ToUpper()}");
                                //Console.WriteLine($"{DateTime.UtcNow} - Re-process Bet - Accepted - {message.Bet.BetId.ToString().ToUpper()}");
                            }
                        }
                    }

                    //Console.WriteLine($"{DateTime.UtcNow} - END - Accepted - {message.Bet.BetId.ToString().ToUpper()}");

                    if (isReprocess && reprocessAttempt < 1)
                        ProcesssBetPlacement(message, provider, messageBroker, correlationId, reprocessAttempt++);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bet placement");
                    //Console.WriteLine($"{DateTime.UtcNow} - END - Error Accepted - {message.Bet.BetId.ToString().ToUpper()} - {ex.Message}");

                    messageBroker.Publish(new OnBetStatus
                    {
                        ClientId = message.Bet.ClientId.ToString().ToUpper(),
                        UserId = message.Bet.UserId,
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        BetStatus = BetStatus.Rejected
                    }, "OnBetStatus", $"r.bet.betstatus.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                    messageBroker.Publish(new OnBetPlacementError
                    {
                        BetId = $"{message.Bet.BetId.ToString().ToUpper()} - Accepted Exception",
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        InnerException = ex.InnerException.ToString()
                    }, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

                    RejectBetOnError(message, messageBroker, correlationId, new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"10001 - {ex.Message}"), 2);
                }
            }
            else if(providerResult.Acceptance == BetAcceptance.Rejected)
            {
                try
                {
                    _logger.LogError($"ProcesssBetPlacement: {providerResult.Reason}");

                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                        using (var context = new BettingContext(optionsBuilder.Options))
                        {
                            var bet = context.Bet.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

                            if (bet != null && bet.BetStatusId == (int)BetStatus.Waiting)
                            {
                                var betStatus = BetStatus.Rejected;

                                bet.Payout = bet.TrueStake;
                                bet.BetStatusId = (int)betStatus;
                                bet.BetStatusName = betStatus.ToString();
                                bet.Reason = providerResult.Reason;
                                bet.TaxedStake = 0;
                                bet.TaxedWinnings = 0;

                                var betEntity = context.Bet.Update(bet).Entity;
                                payload.Bet = betEntity;

                                context.BetAudit.Add(new BetAudit
                                {
                                    BetId = betEntity.BetId,
                                    UserId = betEntity.UserId,
                                    Stake = betEntity.Stake,
                                    TrueStake = betEntity.TrueStake,
                                    Odds = betEntity.Odds,
                                    OddsTypeId = betEntity.OddsTypeId,
                                    OddsTypeName = betEntity.OddsTypeName,
                                    Payout = betEntity.Payout,
                                    WinLoss = betEntity.WinLoss,
                                    BetStatusId = betEntity.BetStatusId,
                                    BetStatusName = betEntity.BetStatusName,
                                    Reason = betEntity.Reason,
                                    CurrencyCode = betEntity.CurrencyCode,
                                    LanguageCode = betEntity.LanguageCode,
                                    ExchangeRate = betEntity.ExchangeRate,
                                    IPAddress = betEntity.IPAddress,
                                    Channel = betEntity.Channel,
                                    ClientId = betEntity.ClientId,
                                    ClientIdRef = betEntity.ClientIdRef,
                                    ClientCode = betEntity.ClientCode,
                                    ClientName = betEntity.ClientName,
                                    LimitId = betEntity.LimitId,
                                    Endpoint = betEntity.Endpoint,
                                    OperatorToken = betEntity.OperatorToken,
                                    CompanyShare = betEntity.CompanyShare,
                                    IsResettled = betEntity.IsResettled,
                                    IsCashout = betEntity.IsCashout,
                                    BetDate = betEntity.BetDate,
                                    BetDateOffset = betEntity.BetDateOffset,
                                    SettlementDate = betEntity.SettlementDate,
                                    SettlementDateOffset = betEntity.SettlementDateOffset,
                                    StakeTax = betEntity.StakeTax,
                                    WinningsTax = betEntity.WinningsTax,
                                    TaxedStake = betEntity.TaxedStake,
                                    TaxedWinnings = betEntity.TaxedWinnings,
                                    ParlayBonus = betEntity.ParlayBonus,
                                    ParlayBonusWinnings = betEntity.ParlayBonusWinnings,
                                    StakeReal = betEntity.StakeReal,
                                    StakeBonus = betEntity.StakeBonus,
                                    StakeRealPayout = betEntity.StakeRealPayout,
                                    StakeBonusPayout = betEntity.StakeBonusPayout,
                                    IsFreeBet = betEntity.IsFreeBet,
                                    BetTypeId = betEntity.BetTypeId,
                                    BetTypeName = betEntity.BetTypeName,
                                    OddsRef = betEntity.OddsRef,
                                    IsAgent = betEntity.IsAgent
                                });

                                var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                                foreach (var betSelection in betSelections)
                                {
                                    betSelection.BetSelectionStatusId = bet.BetStatusId;
                                    betSelection.BetSelectionStatusName = bet.BetStatusName;
                                    betSelection.IsSettled = true;

                                    var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                    
                                    var betSelectionExtension = new BetSelectionMessageExtenstion()
                                    {
                                        BetSelection = betSelectionEntity,
                                    };

                                    if (message.DelayedBetAcceptance == null)
                                    {
                                        betSelectionExtension.MarketSelectionCount = message.BetSelections.FirstOrDefault(c => c.BetSelection.BetId == betSelection.BetId && c.BetSelection.BetSelectionId == betSelection.BetSelectionId).MarketSelectionCount;
                                    }

                                    payload.BetSelections.Add(betSelectionExtension);


                                    context.BetSelectionAudit.Add(new BetSelectionAudit
                                    {
                                        BetId = betSelectionEntity.BetId,
                                        BetSelectionId = betSelectionEntity.BetSelectionId,
                                        UserId = betSelectionEntity.UserId,
                                        SportId = betSelectionEntity.SportId,
                                        SportName = betSelectionEntity.SportName,
                                        SegmentId = betSelectionEntity.SegmentId,
                                        SegmentName = betSelectionEntity.SegmentName,
                                        LeagueId = betSelectionEntity.LeagueId,
                                        LeagueName = betSelectionEntity.LeagueName,
                                        FixtureId = betSelectionEntity.FixtureId,
                                        FixtureName = betSelectionEntity.FixtureName,
                                        FixtureTypeId = betSelectionEntity.FixtureTypeId,
                                        FixtureTypeName = betSelectionEntity.FixtureTypeName,
                                        MarketTemplateId = betSelectionEntity.MarketTemplateId,
                                        MarketId = betSelectionEntity.MarketId,
                                        MarketName = betSelectionEntity.MarketName,
                                        SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
                                        SelectionId = betSelectionEntity.SelectionId,
                                        SelectionName = betSelectionEntity.SelectionName,
                                        FixtureStatusId = betSelectionEntity.FixtureStatusId,
                                        FixtureStatusName = betSelectionEntity.FixtureStatusName,
                                        Line = betSelectionEntity.Line,
                                        Odds = betSelectionEntity.Odds,
                                        OddsRef = betSelectionEntity.OddsRef,
                                        BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                                        BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                                        IsSettled = betSelectionEntity.IsSettled,
                                        IsResettled = betSelectionEntity.IsResettled,
                                        FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
                                        SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
                                        FixtureStartDate = betSelectionEntity.FixtureStartDate,
                                        BetScore = betSelectionEntity.BetScore,
                                        SettlementScore = betSelectionEntity.SettlementScore,
                                        Probability = betSelectionEntity.Probability,
                                        WonProbability = betSelectionEntity.WonProbability,
                                        LostProbability = betSelectionEntity.LostProbability,
                                        VoidProbability = betSelectionEntity.VoidProbability,
                                        HalfWonProbability = betSelectionEntity.HalfWonProbability,
                                        HalfLostProbability = betSelectionEntity.HalfLostProbability,
                                        CorrelationId = betSelectionEntity.CorrelationId,
                                        DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                                        IsBinary = betSelectionEntity.IsBinary,
                                    });
                                }

                                var isBoosted = message.DelayedBetAcceptance == null ? message.IsBoosted : bet.BetTypeId == (int)BetType.PriceBoost;
                                if (isBoosted)
                                {
                                    var betBoosted = context.BetPriceBoost.Where(c => c.BetId == bet.BetId).FirstOrDefault();
                                    betBoosted.BetStatusId = bet.BetStatusId;
                                    betBoosted.BetStatusName = bet.BetStatusName;

                                    context.BetPriceBoost.Update(betBoosted);
                                }

                                ProcessStakeFixtureLimit(payload, context);

                                context.SaveChanges();

                                scope.Complete();

                                messageBroker.Publish(new OnBetTimeout()
                                {
                                    BetId = bet.BetId
                                }, "OnBetTimeout", "r.bet.cancellation", correlationId);

                                //publish to reports
                                messageBroker.Publish(payload, "OnBetRejection", "r.bet.reports", correlationId);

                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                    UserId = payload.Bet.UserId,
                                    BetId = payload.Bet.BetId.ToString().ToUpper(),
                                    BetStatus = (BetStatus)payload.Bet.BetStatusId
                                }, "OnBetStatus", $"r.bet.betstatus.{payload.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);


                                //TODO: get the actual result for GTS market expired
                                // check if error code is due to Market/SOV expired in MTS. (-423)
                                var reason = providerResult.Reason.Split(" - Market/SOV expired in GTS,");

                                if (reason.First() == "-423")
                                {
                                    // prepare message
                                    var split = reason.Last().Split(" + ").First().Split(",");

                                    var selectionSpecifier = split[0].Replace(" Selection: ", string.Empty);
                                    var fixtureSpecifier = split[1].Replace(" Match: ", string.Empty);

                                    var betSelection = message.BetSelections.Select(s => s.BetSelection).FirstOrDefault(c => c.FixtureSpecifier == fixtureSpecifier && c.SelectionSpecifier == selectionSpecifier);

                                    messageBroker.Publish(new OnMarketDeactivationMesssage
                                    {
                                        SportId = betSelection.SportId,
                                        FixtureId = betSelection.FixtureId,
                                        FixtureSpecifier = betSelection.FixtureSpecifier,
                                        MarketId = betSelection.MarketId
                                    }, "OnMarketDeactivation", "r.oe.marketdeactivation", correlationId);
                                }
                            }
                            else
                            {
                                messageBroker.Publish(new OnBetPlacementError
                                {
                                    BetId = $"{message.Bet.BetId.ToString().ToUpper()} - Bet id is not found",
                                    Error = $"Bet might be stucked in waiting - {JsonSerializer.Serialize(providerResult)}",
                                    StackTrace = string.Empty,
                                    InnerException = string.Empty
                                }, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ProcesssBetPlacement Eror: {ex.Message}");

                    messageBroker.Publish(new OnBetStatus
                    {
                        ClientId = message.Bet.ClientId.ToString().ToUpper(),
                        UserId = message.Bet.UserId,
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        BetStatus = BetStatus.Rejected
                    }, "OnBetStatus", $"r.bet.betstatus.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                    messageBroker.Publish(new OnBetPlacementError
                    {
                        BetId = $"{message.Bet.BetId.ToString().ToUpper()} - Rejected Exception",
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        InnerException = ex.InnerException?.ToString() ?? string.Empty,
                    }, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

                    RejectBetOnError(message, messageBroker, correlationId, new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"10002 - {ex.Message}"), 2);
                }
            }
            else //Pending assessment/delay assessment
            {
                _memCache.AddOrUpdateCache(key, message, 1);
            }
        }

        private void RejectBetOnError(OnBetPlacement message, IMessageBroker messageBroker, string correlationId, Tuple<TicketAcceptance, string> result, int remainingRetries)
        {
            if (remainingRetries == 0) return;
            remainingRetries -= 1;

            var rejectionPayload = new OnBetPlacement();

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        var bet = context.Bet.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

                        if (bet != null && bet.BetStatusId == (int)BetStatus.Waiting)
                        {
                            var betStatus = BetStatus.Rejected;

                            bet.Payout = bet.TrueStake;
                            bet.BetStatusId = (int)betStatus;
                            bet.BetStatusName = betStatus.ToString();
                            bet.Reason = result.Item2;
                            bet.TaxedStake = 0;
                            bet.TaxedWinnings = 0;

                            var betEntity = context.Bet.Update(bet).Entity;
                            rejectionPayload.Bet = betEntity;

                            context.BetAudit.Add(new BetAudit
                            {
                                BetId = betEntity.BetId,
                                UserId = betEntity.UserId,
                                Stake = betEntity.Stake,
                                TrueStake = betEntity.TrueStake,
                                Odds = betEntity.Odds,
                                OddsTypeId = betEntity.OddsTypeId,
                                OddsTypeName = betEntity.OddsTypeName,
                                Payout = betEntity.Payout,
                                WinLoss = betEntity.WinLoss,
                                BetStatusId = betEntity.BetStatusId,
                                BetStatusName = betEntity.BetStatusName,
                                Reason = betEntity.Reason,
                                CurrencyCode = betEntity.CurrencyCode,
                                LanguageCode = betEntity.LanguageCode,
                                ExchangeRate = betEntity.ExchangeRate,
                                IPAddress = betEntity.IPAddress,
                                Channel = betEntity.Channel,
                                ClientId = betEntity.ClientId,
                                ClientIdRef = betEntity.ClientIdRef,
                                ClientCode = betEntity.ClientCode,
                                ClientName = betEntity.ClientName,
                                LimitId = betEntity.LimitId,
                                Endpoint = betEntity.Endpoint,
                                OperatorToken = betEntity.OperatorToken,
                                CompanyShare = betEntity.CompanyShare,
                                IsResettled = betEntity.IsResettled,
                                IsCashout = betEntity.IsCashout,
                                BetDate = betEntity.BetDate,
                                BetDateOffset = betEntity.BetDateOffset,
                                SettlementDate = betEntity.SettlementDate,
                                SettlementDateOffset = betEntity.SettlementDateOffset,
                                StakeTax = betEntity.StakeTax,
                                WinningsTax = betEntity.WinningsTax,
                                TaxedStake = betEntity.TaxedStake,
                                TaxedWinnings = betEntity.TaxedWinnings,
                                IsFreeBet = betEntity.IsFreeBet,
                                BetTypeId = betEntity.BetTypeId,
                                BetTypeName = betEntity.BetTypeName,
                                OddsRef = betEntity.OddsRef,
                                IsAgent = betEntity.IsAgent
                            });

                            var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                            foreach (var betSelection in betSelections)
                            {
                                betSelection.BetSelectionStatusId = bet.BetStatusId;
                                betSelection.BetSelectionStatusName = bet.BetStatusName;
                                betSelection.IsSettled = true;

                                var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                //rejectionPayload.BetSelections.Add(betSelectionEntity);
                                rejectionPayload.BetSelections.Add(new BetSelectionMessageExtenstion()
                                {
                                    BetSelection = betSelectionEntity,
                                    MarketSelectionCount = message.BetSelections.FirstOrDefault(c => c.BetSelection.BetId == betSelection.BetId && c.BetSelection.BetSelectionId == betSelection.BetSelectionId).MarketSelectionCount
                                });

                                context.BetSelectionAudit.Add(new BetSelectionAudit
                                {
                                    BetId = betSelectionEntity.BetId,
                                    BetSelectionId = betSelectionEntity.BetSelectionId,
                                    UserId = betSelectionEntity.UserId,
                                    SportId = betSelectionEntity.SportId,
                                    SportName = betSelectionEntity.SportName,
                                    SegmentId = betSelectionEntity.SegmentId,
                                    SegmentName = betSelectionEntity.SegmentName,
                                    LeagueId = betSelectionEntity.LeagueId,
                                    LeagueName = betSelectionEntity.LeagueName,
                                    FixtureId = betSelectionEntity.FixtureId,
                                    FixtureName = betSelectionEntity.FixtureName,
                                    FixtureTypeId = betSelectionEntity.FixtureTypeId,
                                    FixtureTypeName = betSelectionEntity.FixtureTypeName,
                                    MarketTemplateId = betSelectionEntity.MarketTemplateId,
                                    MarketId = betSelectionEntity.MarketId,
                                    MarketName = betSelectionEntity.MarketName,
                                    SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
                                    SelectionId = betSelectionEntity.SelectionId,
                                    SelectionName = betSelectionEntity.SelectionName,
                                    FixtureStatusId = betSelectionEntity.FixtureStatusId,
                                    FixtureStatusName = betSelectionEntity.FixtureStatusName,
                                    Line = betSelectionEntity.Line,
                                    Odds = betSelectionEntity.Odds,
                                    OddsRef = betSelectionEntity.OddsRef,
                                    BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                                    BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                                    IsSettled = betSelectionEntity.IsSettled,
                                    IsResettled = betSelectionEntity.IsResettled,
                                    FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
                                    SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
                                    FixtureStartDate = betSelectionEntity.FixtureStartDate,
                                    BetScore = betSelectionEntity.BetScore,
                                    SettlementScore = betSelectionEntity.SettlementScore,
                                    Probability = betSelectionEntity.Probability,
                                    WonProbability = betSelectionEntity.WonProbability,
                                    LostProbability = betSelectionEntity.LostProbability,
                                    VoidProbability = betSelectionEntity.VoidProbability,
                                    HalfWonProbability = betSelectionEntity.HalfWonProbability,
                                    HalfLostProbability = betSelectionEntity.HalfLostProbability,
                                    CorrelationId = betSelectionEntity.CorrelationId,
                                    DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                                    IsBinary = betSelectionEntity.IsBinary,
                                });
                            }

                            ProcessStakeFixtureLimit(rejectionPayload, context);

                            context.SaveChanges();

                            scope.Complete();

                            messageBroker.Publish(new OnBetTimeout()
                            {
                                BetId = bet.BetId
                            }, "OnBetTimeout", "r.bet.cancellation", correlationId);

                            //publish to reports
                            messageBroker.Publish(rejectionPayload, "OnBetRejection", "r.bet.reports", correlationId);

                            messageBroker.Publish(new OnBetStatus
                            {
                                ClientId = rejectionPayload.Bet.ClientId.ToString().ToUpper(),
                                UserId = rejectionPayload.Bet.UserId,
                                BetId = rejectionPayload.Bet.BetId.ToString().ToUpper(),
                                BetStatus = (BetStatus)rejectionPayload.Bet.BetStatusId
                            }, "OnBetStatus", "r.bet.betstatus", correlationId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RejectBetOnError(message, messageBroker, correlationId, new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"10003 - {ex.Message}"), remainingRetries);
            }
        }
        private static void ProcessStakeFixtureLimit(OnBetPlacement payload, BettingContext context)
        {
            if (payload.BetSelections.Count == 1)
            {
                var stakeLimitFixture = context.StakeLimitFixture.Find($"{payload.Bet.ClientCode}-{payload.Bet.UserId}", payload.BetSelections.First().BetSelection.FixtureId);

                if (stakeLimitFixture != null)
                {
                    stakeLimitFixture.Remaining += payload.Bet.TrueStake;

                    context.StakeLimitFixture.Update(stakeLimitFixture);
                }
            }
        }

    }
}
