using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using Sportradar.MTS.SDK.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Transactions;

namespace Platform.Placement.Servicev2.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private IBuilderFactory _factory;
        private readonly IConfiguration _configuration;
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void ProcesssBetPlacement(OnBetPlacement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId, int reprocessAttempt = 0)
        {
            var payload = new OnBetPlacement();
            payload.IsBoosted = message.IsBoosted;
            payload.IsBetBuilder = message.IsBetBuilder;
            payload.BonusId = message.BonusId;
            payload.Odds = message.Odds;
            payload.UserAgent = message.UserAgent;
            payload.IsOGClient = message.IsOGClient;

            var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ?
                message.BetSelections.Any(c => c.BetSelection.SportId > 1000) ? new Tuple<TicketAcceptance, string>(TicketAcceptance.Accepted, "1024 - Accepted (MTS disabled for VS)") :
                ValidateToMTS(message, mts) : new Tuple<TicketAcceptance, string>(TicketAcceptance.Accepted, "1024 - Accepted (MTS disabled)");

            if (result.Item1 == TicketAcceptance.Accepted)
            {
                var isReprocess = false;
                //Console.WriteLine($"{DateTime.UtcNow} - BEG - Accepted - {message.Bet.BetId.ToString().ToUpper()}");

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
                                bet.Reason = $"{result.Item2} (MTS)";

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
                                    payload.BetSelections.Add(new BetSelectionMessageExtenstion()
                                    {
                                        BetSelection = betSelectionEntity,
                                        MarketSelectionCoumt = message.BetSelections.FirstOrDefault(c => c.BetSelection.BetId == betSelection.BetId && c.BetSelection.BetSelectionId == betSelection.BetSelectionId).MarketSelectionCoumt
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
                        ProcesssBetPlacement(message, mts, messageBroker, correlationId, reprocessAttempt++);
                }
                catch (Exception ex)
                {
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
            else
            {
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
                                    //payload.BetSelections.Add(betSelectionEntity);
                                    payload.BetSelections.Add(new BetSelectionMessageExtenstion()
                                    {
                                        BetSelection = betSelectionEntity,
                                        MarketSelectionCoumt = message.BetSelections.FirstOrDefault(c => c.BetSelection.BetId == betSelection.BetId && c.BetSelection.BetSelectionId == betSelection.BetSelectionId).MarketSelectionCoumt
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

                                if (message.IsBoosted == true)
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

                                //TODO: delete when TW is no longer the frontend
                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                    UserId = payload.Bet.UserId,
                                    BetId = payload.Bet.BetId.ToString().ToUpper(),
                                    BetStatus = (BetStatus)payload.Bet.BetStatusId
                                }, "OnBetStatus", "r.bet.betstatus", correlationId);

                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                    UserId = payload.Bet.UserId,
                                    BetId = payload.Bet.BetId.ToString().ToUpper(),
                                    BetStatus = (BetStatus)payload.Bet.BetStatusId
                                }, "OnBetStatus", $"r.bet.betstatus.{payload.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                                // check if error code is due to Market/SOV expired in MTS. (-423)
                                var reason = result.Item2.Split(" - Market/SOV expired in MTS,");

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
                                    Error = $"Bet might be stucked in waiting - {JsonSerializer.Serialize(result)}",
                                    StackTrace = string.Empty,
                                    InnerException = string.Empty
                                }, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
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
                        InnerException = ex.InnerException.ToString()
                    }, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

                    RejectBetOnError(message, messageBroker, correlationId, new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"10002 - {ex.Message}"), 2);
                }
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
                                    MarketSelectionCoumt = message.BetSelections.FirstOrDefault(c => c.BetSelection.BetId == betSelection.BetId && c.BetSelection.BetSelectionId == betSelection.BetSelectionId).MarketSelectionCoumt
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
        private Tuple<TicketAcceptance, string> ValidateToMTS(OnBetPlacement message, IMtsSdk mts)
        {
            try
            {
                _factory = mts.BuilderFactory;

                TicketAcceptance betStatus;
                string reason;

                var isValidIP = IPAddress.TryParse(message.Bet.IPAddress, out IPAddress ipAddress);

                if (!isValidIP)
                    ipAddress = IPAddress.Loopback;

                var bet = _factory.CreateTicketBuilder()
                    .SetTicketId(message.Bet.BetId.ToString().ToUpper())
                    .SetOddsChange(Sportradar.MTS.SDK.Entities.Enums.OddsChangeType.None)
                    .SetSender(_factory.CreateSenderBuilder()
                    .SetSenderChannel((SenderChannel)Enum.Parse(typeof(SenderChannel), message.Bet.Channel))
                    .SetLimitId(Convert.ToInt32(message.Bet.LimitId == "0" ? "2183" : message.Bet.LimitId))
                    .SetCurrency(message.Bet.CurrencyCode.ToString())
                    .SetEndCustomer(_factory.CreateEndCustomerBuilder()
                    .SetId($"{message.Bet.ClientCode}{message.Bet.UserId.Replace("-", "")}")
                    .SetConfidence(10000)
                    .SetIp(ipAddress)
                    .SetLanguageId(message.Bet.LanguageCode.ToString())
                    .SetDeviceId($"{message.Bet.ClientCode}{message.Bet.UserId.Replace("-", "")}")
                    .Build())
                    .Build())
                    .AddBet(BuildBet(message.Bet, message.BetSelections.Select(s => s.BetSelection).ToList(), message.IsBetBuilder, message.Odds))
                    .BuildTicket();

                var response = mts.SendTicketBlocking(bet);

                betStatus = response.Status;
                reason = $"{response.Reason.Code} - {response.Reason.Message}";

                return new Tuple<TicketAcceptance, string>(betStatus, reason);
            }
            catch (Exception ex)
            {
                return new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }
        private IBet BuildBet(Bet betData, List<BetSelection> betSelectionData, bool isBetBuilder, decimal calculatedOdds)
        {
            //var companyStake = (0.0001m * betData.ExchangeRate) * 10000;

            //if (betData.CompanyShare > 0)
            //    companyStake = (betData.TrueStake * betData.CompanyShare) * 10000;

            //if (betData.CompanyShare == 0)
            //    companyStake = betData.TrueStake * 10000;

            //var companyStake = betData.TrueStake * 10000;
            //var stake = (betData.TrueStake - (betData.TrueStake * betData.WinningsTax)) * 10000;

            var stake = 0m;
            var entireStake = betData.TrueStake * 10000;

            if (betData.ClientCode.ToUpper() == "RKP")
                stake = (betData.TrueStake * .10m) * 10000;
            else
                stake = betData.TrueStake * 10000;

            #region Get Parlay Bonus
            var bonus = 0m;
            var parlayBonus = 0m;
            var hasBonus = false;
            BetBonusDescription bonusDescription = BetBonusDescription.Other;
            // parlay bonus: bet should be Won by default

            var hasVirtualSport = betSelectionData.Any(s => s.SportId > 1000) ? true : false;

            if (isBetBuilder == false && hasVirtualSport == false && betSelectionData.Count >= 3 && betData.IsFreeBet == false)
            {
                var parlayConfig = GetParlayBetConfig(Guid.Parse(betData.ClientId), betData.CurrencyCode, betData.BetDate, betSelectionData, betData.Payout);

                if (parlayConfig.Item1 == true)
                {
                    parlayBonus = parlayConfig.Item2.ParlayBonus;
                    bonus = betData.Payout * parlayConfig.Item2.ParlayBonus;
                    bonus = bonus * 10000;

                    hasBonus = bonus == 0 ? false : true;
                    bonusDescription = BetBonusDescription.AccaBonus;
                }
            }

            if (betData.BetTypeId == (int)BetType.PriceBoost) //boosted bonus
            {
                var totalBoostedPayout = betData.Odds * stake;
                var totalOriginalPayout = betData.OddsRef * stake;

                bonus = bonus + (totalBoostedPayout - totalOriginalPayout);

                hasBonus = bonus == 0 ? false : true;
                bonusDescription = BetBonusDescription.OddsBooster;
            }

            #endregion

            var selection = hasBonus ? _factory.CreateBetBuilder()
                                    .SetBetId(betData.BetId.ToString().ToUpper())
                                    .SetBetBonus(Convert.ToInt64(bonus), BetBonusMode.All, BetBonusType.Total, bonusDescription, BetBonusPaidAs.Cash)
                                    .SetStake(Convert.ToInt64(stake), StakeType.Total)
                                    .SetEntireStake(Convert.ToInt64(entireStake), StakeType.Total)
                                    .AddSelectedSystem(betSelectionData.Count)
                                    :
                                        isBetBuilder ? _factory.CreateBetBuilder()
                                            .SetCustomBet(true)
                                            .SetCalculationOdds(Convert.ToInt32(calculatedOdds * 10000))
                                            .SetBetId(betData.BetId.ToString().ToUpper())
                                            .SetStake(Convert.ToInt64(stake), StakeType.Total)
                                            .SetEntireStake(Convert.ToInt64(entireStake), StakeType.Total)
                                            .AddSelectedSystem(betSelectionData.Count)
                                            :
                                                _factory.CreateBetBuilder()
                                                .SetBetId(betData.BetId.ToString().ToUpper())
                                                .SetStake(Convert.ToInt64(stake), StakeType.Total)
                                                .SetEntireStake(Convert.ToInt64(entireStake), StakeType.Total)
                                                .AddSelectedSystem(betSelectionData.Count);

            foreach (var betSelection in betSelectionData)
            {
                if (betData.BetTypeId == (int)BetType.PriceBoost)
                {
                    selection.AddSelection(_factory.CreateSelectionBuilder()
                                               .SetEventId(betSelection.FixtureSpecifier)
                                               .SetId(betSelection.SelectionSpecifier)
                                               .SetOdds(Convert.ToInt32(betSelection.OddsRef * 10000))
                                               .SetBoostedOdds(Convert.ToInt32(betSelection.Odds * 10000))
                                               .Build());
                }
                else
                {
                    selection.AddSelection(_factory.CreateSelectionBuilder()
                                               .SetEventId(betSelection.FixtureSpecifier)
                                               .SetId(betSelection.SelectionSpecifier)
                                               .SetOdds(Convert.ToInt32(betSelection.Odds * 10000))
                                               .Build());
                }

            }

            return selection.Build();
        }

        private Tuple<bool, ParlayBonusAudit> GetParlayBetConfig(Guid clientId, string currencyCode, DateTime betDate, List<BetSelection> selections, decimal payout)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    var minimumOdds = context.ParlayBonusAudit.Where(c => c.ClientId == clientId && c.TimeStamp < betDate
                        && c.CurrencyCode == currencyCode).OrderByDescending(o => o.TimeStamp).FirstOrDefault();

                    if (minimumOdds == null)
                        return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());

                    int wonSelectionCount = selections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Won && c.Odds >= minimumOdds.ParlayBonusMinOdds).Count();

                    if (wonSelectionCount >= 3)
                    {
                        var result = context.ParlayBonusAudit.Where(c => c.ClientId == clientId && c.TimeStamp < betDate
                          && c.CurrencyCode == currencyCode && wonSelectionCount == c.SelectionCount).OrderByDescending(o => o.TimeStamp).FirstOrDefault();


                        if (result == null)
                            return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());
                        else
                            return new Tuple<bool, ParlayBonusAudit>(true, result);
                        //{
                        //    var potentialParlayBonusWinnings = payout * result.ParlayBonus;

                        //    if (potentialParlayBonusWinnings > result.ParlayBonusMaxWinnings && result.ParlayBonusMaxWinnings > 0)
                        //        return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());
                        //    else
                        //        return new Tuple<bool, ParlayBonusAudit>(true, result);
                        //}
                    }
                    else
                        return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());
                }
            }
        }
    }
}
