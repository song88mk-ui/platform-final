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
using System.Threading;
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
        public void ProcesssBetPlacement(OnBetPlacement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {
            //Console.WriteLine($"{DateTime.UtcNow} - BEG - {message.Bet.BetId.ToString().ToUpper()}");

            var payload = new OnBetPlacement();

            var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ?
                message.BetSelections.Any(c => c.SportId > 1000) ? new Tuple<TicketAcceptance, string>(TicketAcceptance.Accepted, "1024 - Accepted (MTS disabled for VS)") :
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
                                    TaxedWinnings = betEntity.TaxedWinnings
                                });

                                var betSelections = context.BetSelection.Where(c => c.BetId == betEntity.BetId).ToList();

                                foreach (var betSelection in betSelections)
                                {
                                    betSelection.BetSelectionStatusId = betEntity.BetStatusId;
                                    betSelection.BetSelectionStatusName = betEntity.BetStatusName;

                                    var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                    payload.BetSelections.Add(betSelectionEntity);

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
                                        CorrelationId = betSelectionEntity.CorrelationId
                                    });
                                }

                                var contextResult = context.SaveChanges();

                                //if (contextResult == 0)
                                //    Console.WriteLine($"{DateTime.UtcNow} - Not Saved - Accepted - {message.Bet.BetId.ToString().ToUpper()}");
                                //else
                                //    Console.WriteLine($"{DateTime.UtcNow} - Saved - Accepted - {message.Bet.BetId.ToString().ToUpper()}");

                                scope.Complete();

                                messageBroker.Publish(payload, "OnBetPlacement", "r.bet.operator", correlationId);
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

                    if (isReprocess)
                        ProcesssBetPlacement(message, mts, messageBroker, correlationId);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"{DateTime.UtcNow} - END - Error Accepted - {message.Bet.BetId.ToString().ToUpper()} - {ex.Message}");

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
                //Console.WriteLine($"{DateTime.UtcNow} - BEG - Rejected - {message.Bet.BetId.ToString().ToUpper()}");

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
                                    TaxedWinnings = betEntity.TaxedWinnings
                                });

                                var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                                foreach (var betSelection in betSelections)
                                {
                                    betSelection.BetSelectionStatusId = bet.BetStatusId;
                                    betSelection.BetSelectionStatusName = bet.BetStatusName;
                                    betSelection.IsSettled = true;

                                    var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                    payload.BetSelections.Add(betSelectionEntity);

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
                                        CorrelationId = betSelectionEntity.CorrelationId
                                    });
                                }

                                ProcessStakeFixtureLimit(payload, context);

                                context.SaveChanges();

                                scope.Complete();

                                messageBroker.Publish(payload, "OnBetTimeout", "r.bet.cancellation", correlationId);

                                //publish to reports
                                messageBroker.Publish(payload, "OnBetRejection", "r.bet.reports", correlationId);

                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                    UserId = payload.Bet.UserId,
                                    BetId = payload.Bet.BetId.ToString().ToUpper(),
                                    BetStatus = (BetStatus)payload.Bet.BetStatusId
                                }, "OnBetStatus", "r.bet.betstatus", correlationId);
                            }
                        }
                    }

                    //Console.WriteLine($"{DateTime.UtcNow} - END - Rejected - {message.Bet.BetId.ToString().ToUpper()}");
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"{DateTime.UtcNow} - END - Error Rejected - {message.Bet.BetId.ToString().ToUpper()} - {ex.Message}");

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
                                TaxedWinnings = betEntity.TaxedWinnings
                            });

                            var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                            foreach (var betSelection in betSelections)
                            {
                                betSelection.BetSelectionStatusId = bet.BetStatusId;
                                betSelection.BetSelectionStatusName = bet.BetStatusName;
                                betSelection.IsSettled = true;

                                var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                rejectionPayload.BetSelections.Add(betSelectionEntity);

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
                                    CorrelationId = betSelectionEntity.CorrelationId
                                });
                            }

                            ProcessStakeFixtureLimit(rejectionPayload, context);

                            context.SaveChanges();

                            scope.Complete();

                            messageBroker.Publish(rejectionPayload, "OnBetTimeout", "r.bet.cancellation", correlationId);

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
                var stakeLimitFixture = context.StakeLimitFixture.Find($"{payload.Bet.ClientCode}-{payload.Bet.UserId}", payload.BetSelections.First().FixtureId);

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
                    .AddBet(BuildBet(message.Bet, message.BetSelections))
                    //.SetTestSource(message.IsTestAccount)
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
        private IBet BuildBet(Bet betData, List<BetSelection> betSelectionData)
        {
            //var companyStake = (0.0001m * betData.ExchangeRate) * 10000;

            //if (betData.CompanyShare > 0)
            //    companyStake = (betData.TrueStake * betData.CompanyShare) * 10000;

            //if (betData.CompanyShare == 0)
            //    companyStake = betData.TrueStake * 10000;

            //var companyStake = betData.TrueStake * 10000;
            //var stake = (betData.TrueStake - (betData.TrueStake * betData.WinningsTax)) * 10000;

            var stake = betData.TrueStake * 10000;

            var selection = _factory.CreateBetBuilder()
                                    .SetBetId(betData.BetId.ToString().ToUpper())
                                    .SetStake(Convert.ToInt64(stake), StakeType.Total)
                                    .SetEntireStake(Convert.ToInt64(stake), StakeType.Total)
                                    .AddSelectedSystem(betSelectionData.Count);

            foreach (var betSelection in betSelectionData)
            {
                selection.AddSelection(_factory.CreateSelectionBuilder()
                                               .SetEventId(betSelection.FixtureSpecifier)
                                               .SetId(betSelection.SelectionSpecifier)
                                               .SetOdds(Convert.ToInt32(betSelection.Odds * 10000))
                                               .Build());
            }

            return selection.Build();
        }
    }
}
