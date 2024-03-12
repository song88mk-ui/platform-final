using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using Sportradar.MTS.SDK.Entities.Internal.REST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Transactions;
using static App.Metrics.Health.HealthCheck;

namespace Platform.CashOut.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private IBuilderFactory _factory;
        private readonly IConfiguration _configuration;
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private void ProcessCashOutSettlement(OnCashoutBetSettlement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {
            var bet = new Bet();

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        var settledDate = DateTime.UtcNow;
                        bet = context.Bet.Where(c => c.BetId == message.BetId).FirstOrDefault();

                        if (bet != null)
                        {
                            if (bet.BetStatusId != (int)BetStatus.Running || bet.IsFreeBet)
                            {

                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = message.ClientId.ToString().ToUpper(),
                                    UserId = message.UserId,
                                    BetId = message.BetId.ToString().ToUpper(),
                                    BetStatus = (BetStatus)bet.BetStatusId,
                                    Message = "Cashout request has been declined"
                                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                //messageBroker.Publish(new OnCashOutResult
                                //{
                                //    ClientCode = bet.ClientCode.ToUpper(),
                                //    UserId = bet.UserId,
                                //    BetId = bet.BetId.ToString().ToUpper(),
                                //    BetStatus = (BetStatus)bet.BetStatusId,
                                //    CashOutStatus = CashOutStatus.Unavailable,
                                //    CashOutStake = 0,
                                //    Message = "Cashout request has been declined",
                                //    ClientId = bet.ClientId,
                                //    Endpoint = message.Endpoint
                                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
                                return;
                            }

                            var cashoutHistory = context.CashOutHistory.AsNoTracking().Where(c => c.BetId == bet.BetId).OrderByDescending(o => o.CashoutDate).ToList();

                            var isPublish = false;
                            var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ?
                                SendCashoutToMTS(message, mts) : new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Accepted, "1024 - Cashout Accepted (MTS disabled)");

                            if (result.Item1 == CashoutAcceptance.Accepted)
                            {
                                var payload = new OnBetSettlement();
                                
                                payload.NonTaxedPayout = message.NonTaxedPayout;
                                if (cashoutHistory.Any())
                                    payload.NonTaxedPayout = payload.NonTaxedPayout + cashoutHistory.Sum(c => c.NonTaxedPayout);

                                var selections = context.BetSelection.Where(c => c.BetId == message.BetId).ToList();
                                if (selections.Count == 0)
                                {
                                    scope.Complete();
                                    return;
                                }

                                var filteredSelections = selections.Where(c => c.BetId == message.BetId).ToList();

                                if (filteredSelections.Count == 0)
                                {
                                    scope.Complete();
                                    return;
                                }

                                bet.Reason = $"{result.Item2} (MTS)";

                                isPublish = SettleBet(message, bet, isPublish, payload, settledDate, context, selections, filteredSelections, cashoutHistory);

                                context.SaveChanges();

                                scope.Complete();

                                if (isPublish)
                                {
                                    messageBroker.Publish(payload, "OnBetFullCashOut", "r.bet.operator", correlationId, 10);

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = message.ClientId.ToString().ToUpper(),
                                        UserId = message.UserId,
                                        BetId = message.BetId.ToString().ToUpper(),
                                        BetStatus = (BetStatus)payload.Bet.BetStatusId,
                                        Message = "Cashout request has been accepted"
                                    }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                    //messageBroker.Publish(new OnCashOutResult
                                    //{
                                    //    ClientCode = message.ClientCode.ToString().ToUpper(),
                                    //    UserId = message.UserId,
                                    //    BetId = message.BetId.ToString().ToUpper(),
                                    //    BetStatus = (BetStatus)payload.Bet.BetStatusId,
                                    //    CashOutStatus = CashOutStatus.Unavailable,
                                    //    CashOutStake = 0,
                                    //    Message = "Cashout request has been accepted",
                                    //    ClientId = message.ClientId.ToString().ToUpper(),
                                    //    Endpoint = message.Endpoint
                                    //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
                                }
                            }
                            else
                            {

                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = message.ClientId.ToString().ToUpper(),
                                    UserId = message.UserId,
                                    BetId = message.BetId.ToString().ToUpper(),
                                    BetStatus = BetStatus.Running,
                                    Message = JsonSerializer.Serialize(result),
                                }, "OnBetStatus", $"r.bet.cashout.error", correlationId);

                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = message.ClientId.ToString().ToUpper(),
                                    UserId = message.UserId,
                                    BetId = message.BetId.ToString().ToUpper(),
                                    BetStatus = BetStatus.Running,
                                    Message = "Cashout request has been declined"
                                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                //messageBroker.Publish(new OnCashOutResult
                                //{
                                //    ClientCode = bet.ClientCode.ToUpper(),
                                //    UserId = bet.UserId,
                                //    BetId = bet.BetId.ToString().ToUpper(),
                                //    BetStatus = (BetStatus)bet.BetStatusId,
                                //    CashOutStatus = CashOutStatus.Unavailable,
                                //    CashOutStake = 0,
                                //    Message = "Cashout request has been declined",
                                //    ClientId = message.ClientId.ToString().ToUpper(),
                                //    Endpoint = message.Endpoint
                                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                var errorMessage = new
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Payload = message
                };

                messageBroker.Publish(new OnBetStatus
                {
                    ClientId = message.ClientId.ToString().ToUpper(),
                    UserId = message.UserId,
                    BetId = message.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Running,
                    Message = JsonSerializer.Serialize(errorMessage),
                }, "OnBetStatus", $"r.bet.cashout.error", correlationId);

                messageBroker.Publish(new OnBetStatus
                {
                    ClientId = message.ClientId.ToString().ToUpper(),
                    UserId = message.UserId,
                    BetId = message.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Running,
                    Message = "Cashout request has been declined"
                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                //messageBroker.Publish(new OnCashOutResult
                //{
                //    ClientCode = message.ClientCode.ToUpper(),
                //    UserId = message.UserId,
                //    BetId = message.BetId.ToString().ToUpper(),
                //    BetStatus = (BetStatus)bet.BetStatusId,
                //    CashOutStatus = CashOutStatus.Unavailable,
                //    CashOutStake = 0,
                //    Message = "Cashout request has been declined",
                //    ClientId = message.ClientId,
                //    Endpoint = message.Endpoint
                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
            }
        }

        private void ProcessPartialCashOutSettlement(OnCashoutBetSettlement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {
            var bet = new Bet();

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        var settledDate = DateTime.UtcNow;
                        bet = context.Bet.Where(c => c.BetId == message.BetId).FirstOrDefault();

                        if (bet != null)
                        {
                            if (bet.BetStatusId != (int)BetStatus.Running || bet.IsFreeBet)
                            {
                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = message.ClientId.ToString().ToUpper(),
                                    UserId = message.UserId,
                                    BetId = message.BetId.ToString().ToUpper(),
                                    BetStatus = (BetStatus)bet.BetStatusId,
                                    Message = "Cashout request has been declined"
                                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                //messageBroker.Publish(new OnCashOutResult
                                //{
                                //    ClientCode = bet.ClientCode.ToUpper(),
                                //    UserId = bet.UserId,
                                //    BetId = bet.BetId.ToString().ToUpper(),
                                //    BetStatus = (BetStatus)bet.BetStatusId,
                                //    CashOutStatus = CashOutStatus.Unavailable,
                                //    CashOutStake = 0,
                                //    Message = "Cashout request has been declined",
                                //    ClientId = bet.ClientId,
                                //    Endpoint = message.Endpoint
                                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
                                return;
                            }

                            var cashoutHistory = context.CashOutHistory.AsNoTracking().Where(c => c.BetId == bet.BetId).OrderByDescending(o => o.CashoutDate).ToList();

                            var isPublish = false;
                            var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ?
                                SendPartialCashoutToMTS(message, mts, bet.Stake, cashoutHistory) : new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Accepted, "1024 - Partial Cashout Accepted (MTS disabled)");

                            if (result.Item1 == CashoutAcceptance.Accepted)
                            {
                                var selections = context.BetSelection.Where(c => c.BetId == message.BetId).ToList();
                                if (selections.Count == 0)
                                {
                                    scope.Complete();
                                    return;
                                }

                                var filteredSelections = selections.Where(c => c.BetId == message.BetId).ToList();

                                if (filteredSelections.Count == 0)
                                {
                                    scope.Complete();
                                    return;
                                }

                                isPublish = SettlePartialBet(message, bet, isPublish, settledDate, context);

                                context.SaveChanges();

                                scope.Complete();

                                if (isPublish)
                                {
                                    bet.Payout = message.CashOutStake * message.CashOutStakePercent;

                                    var payload = new OnBetSettlement();
                                    payload.NonTaxedPayout = message.NonTaxedPayout;
                                    payload.Bet = bet;
                                    payload.BetSelections = selections;

                                    messageBroker.Publish(payload, "OnBetPartialCashOut", "r.bet.operator", correlationId, 10);

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = message.ClientId.ToString().ToUpper(),
                                        UserId = message.UserId,
                                        BetId = message.BetId.ToString().ToUpper(),
                                        BetStatus = BetStatus.Running,
                                        Message = "Partial Cashout request has been accepted"
                                    }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                    //messageBroker.Publish(new OnCashOutResult
                                    //{
                                    //    ClientCode = message.ClientCode.ToString().ToUpper(),
                                    //    UserId = message.UserId,
                                    //    BetId = message.BetId.ToString().ToUpper(),
                                    //    BetStatus = BetStatus.Running,
                                    //    CashOutStatus = CashOutStatus.Unavailable,
                                    //    CashOutStake = 0,
                                    //    Message = "Cashout request has been accepted",
                                    //    ClientId = message.ClientId.ToString().ToUpper(),
                                    //    Endpoint = message.Endpoint
                                    //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
                                }
                            }
                            else
                            {
                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = message.ClientId.ToString().ToUpper(),
                                    UserId = message.UserId,
                                    BetId = message.BetId.ToString().ToUpper(),
                                    BetStatus = BetStatus.Running,
                                    Message = JsonSerializer.Serialize(result),
                                }, "OnBetStatus", $"r.bet.cashout.error", correlationId);

                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = message.ClientId.ToString().ToUpper(),
                                    UserId = message.UserId,
                                    BetId = message.BetId.ToString().ToUpper(),
                                    BetStatus = BetStatus.Running,
                                    Message = "Partial Cashout request has been declined"
                                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                //messageBroker.Publish(new OnCashOutResult
                                //{
                                //    ClientCode = bet.ClientCode.ToUpper(),
                                //    UserId = bet.UserId,
                                //    BetId = bet.BetId.ToString().ToUpper(),
                                //    BetStatus = BetStatus.Running,
                                //    CashOutStatus = CashOutStatus.Unavailable,
                                //    CashOutStake = 0,
                                //    Message = "Partial Cashout request has been declined",
                                //    ClientId = message.ClientId.ToString().ToUpper(),
                                //    Endpoint = message.Endpoint
                                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Payload = message
                };

                messageBroker.Publish(new OnBetStatus
                {
                    ClientId = message.ClientId.ToString().ToUpper(),
                    UserId = message.UserId,
                    BetId = message.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Running,
                    Message = JsonSerializer.Serialize(errorMessage),
                }, "OnBetStatus", $"r.bet.cashout.error", correlationId);

                messageBroker.Publish(new OnBetStatus
                {
                    ClientId = message.ClientId.ToString().ToUpper(),
                    UserId = message.UserId,
                    BetId = message.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Running,
                    Message = "Partial Cashout request has been declined"
                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                //messageBroker.Publish(new OnCashOutResult
                //{
                //    ClientCode = message.ClientCode.ToUpper(),
                //    UserId = message.UserId,
                //    BetId = message.BetId.ToString().ToUpper(),
                //    BetStatus = (BetStatus)bet.BetStatusId,
                //    CashOutStatus = CashOutStatus.Unavailable,
                //    CashOutStake = 0,
                //    Message = "Partial Cashout request has been declined",
                //    ClientId = message.ClientId,
                //    Endpoint = message.Endpoint
                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);
            }
        }

        private bool SettleBet(OnCashoutBetSettlement message, Bet bet, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, List<BetSelection> filteredSelections, List<CashOutHistory> partialCashout)
        {

            foreach (var selection in filteredSelections)
            {
                var tempSelection = message.Probabilities.Where(c => c.BetSelectionId == selection.BetSelectionId).FirstOrDefault();

                if (selection.BetSelectionStatusId == (int)BetStatus.Running)
                {
                    selection.IsSettled = true;
                    selection.BetSelectionStatusId = (int)BetStatus.CashedOut;
                    selection.BetSelectionStatusName = BetStatus.CashedOut.ToString();
                }

                if (tempSelection != null)
                {
                    selection.Probability = tempSelection.Probability;
                    selection.WonProbability = tempSelection.WonProbability;
                    selection.LostProbability = tempSelection.LostProbability;
                    selection.VoidProbability = tempSelection.VoidProbability;
                    selection.HalfWonProbability = tempSelection.HalfWonProbability;
                    selection.HalfLostProbability = tempSelection.HalfLostProbability;
                }

                var betSelectionEntity = context.BetSelection.Update(selection).Entity;

                if (selection.BetSelectionStatusId == (int)BetStatus.CashedOut)
                {
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
            }
            var prevCashoutTaxedStake = 0m;
            var prevCashoutTaxedWinnings = 0m;
            var prevCashoutAmount = 0m;
            if (partialCashout.Any())
            {
                prevCashoutTaxedStake = partialCashout.Sum(c => c.TaxedStake);
                prevCashoutTaxedWinnings = partialCashout.Sum(c => c.TaxedWinnings);
                prevCashoutAmount = partialCashout.Sum(c => c.CashoutAmount);
            }

            var stake = bet.TrueStake - bet.TaxedStake;
            var payout = prevCashoutAmount + message.CashOutStake;
            var winloss = payout - bet.TrueStake;

            var betStatus = GetStatus(selections.Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), payout, stake);

            bet.WinLoss = winloss;
            bet.BetStatusId = (int)betStatus;
            bet.BetStatusName = betStatus.ToString();

            bet.Payout = payout;
            bet.SettlementDate = settledDate;
            bet.SettlementDateOffset = settledDate.AddHours(-4);

            bet.TaxedStake = prevCashoutTaxedStake + message.TaxedStake;
            bet.TaxedWinnings = prevCashoutTaxedWinnings + message.TaxedWinnings;

            bet.ParlayBonus = 0;
            bet.ParlayBonusWinnings = 0;

            // get percentage of real and bonus payout.
            var stakeRealPayoutPercentage = bet.StakeReal / bet.TrueStake;
            var stakeBonusPayoutPercentage = bet.StakeBonus / bet.TrueStake;

            bet.StakeRealPayout = payout * stakeRealPayoutPercentage;
            bet.StakeBonusPayout = payout * stakeBonusPayoutPercentage;

            var betEntity = context.Bet.Update(bet).Entity;

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

            if(partialCashout.Any())
            {
                var cashoutHistory = partialCashout.FirstOrDefault(c => c.BetId == message.BetId);

                if (cashoutHistory != null)
                {
                    var stakeUsed = cashoutHistory.PostStake;
                    var trueStakeUsed = cashoutHistory.PostTrueStake;

                    context.CashOutHistory.Add(new CashOutHistory
                    {
                        Id = Guid.NewGuid(),
                        BetId = message.BetId,
                        ClientId = message.ClientId,
                        ClientCode = message.ClientCode,
                        OriginalStake = bet.Stake,
                        StakeUsed = stakeUsed,
                        PreStake = cashoutHistory.PostStake,
                        PostStake = cashoutHistory.PostStake - stakeUsed,
                        TrueStake = bet.TrueStake,
                        TrueStakeUsed = trueStakeUsed,
                        PreTrueStake = cashoutHistory.PostTrueStake,
                        PostTrueStake = cashoutHistory.PostTrueStake - trueStakeUsed,
                        CashoutAmount = message.CashOutStake * message.CashOutStakePercent,
                        CashoutPercent = 1,
                        CashoutDate = settledDate,
                        CashoutDateOffset = settledDate.AddHours(-4),
                        NonTaxedPayout = message.NonTaxedPayout,
                        TaxedStake = message.TaxedStake,
                        TaxedWinnings = message.TaxedWinnings
                    });
                }

            }

            isPublish = true;
            payload.Bet = betEntity;
            payload.BetSelections = selections;

            context.SaveChanges();
            return isPublish;
        }

        private bool SettlePartialBet(OnCashoutBetSettlement message, Bet bet, bool isPublish, DateTime settledDate, BettingContext context)
        {
            var cashoutHistory = context.CashOutHistory.AsNoTracking().Where(c => c.BetId == message.BetId).OrderByDescending(o => o.CashoutDate).FirstOrDefault();

            if (cashoutHistory != null)
            {
                var stakeUsed = cashoutHistory.PostStake * message.CashOutStakePercent;
                var trueStakeUsed = cashoutHistory.PostTrueStake * message.CashOutStakePercent;

                context.CashOutHistory.Add(new CashOutHistory
                {
                    Id = Guid.NewGuid(),
                    BetId = message.BetId,
                    ClientId = message.ClientId,
                    ClientCode = message.ClientCode,
                    OriginalStake = bet.Stake,
                    StakeUsed = stakeUsed,
                    PreStake = cashoutHistory.PostStake,
                    PostStake = cashoutHistory.PostStake - stakeUsed,
                    TrueStake = bet.TrueStake,
                    TrueStakeUsed = trueStakeUsed,
                    PreTrueStake = cashoutHistory.PostTrueStake,
                    PostTrueStake = cashoutHistory.PostTrueStake - trueStakeUsed,
                    CashoutAmount = message.CashOutStake * message.CashOutStakePercent,
                    CashoutPercent = message.CashOutStakePercent,
                    CashoutDate = settledDate,
                    CashoutDateOffset = settledDate.AddHours(-4),
                    NonTaxedPayout = message.NonTaxedPayout,
                    TaxedStake = message.TaxedStake,
                    TaxedWinnings = message.TaxedWinnings   
                });
            }
            else
            {
                var stakeUsed = bet.Stake * message.CashOutStakePercent;
                var trueStakeUsed = bet.TrueStake * message.CashOutStakePercent;

                context.CashOutHistory.Add(new CashOutHistory
                {
                    Id = Guid.NewGuid(),
                    BetId = message.BetId,
                    ClientId = message.ClientId,
                    ClientCode = message.ClientCode,
                    OriginalStake = bet.Stake,
                    StakeUsed = stakeUsed,
                    PreStake = bet.Stake,
                    PostStake = bet.Stake - stakeUsed,
                    TrueStake = bet.TrueStake,
                    TrueStakeUsed = trueStakeUsed,
                    PreTrueStake = bet.TrueStake,
                    PostTrueStake = bet.TrueStake - trueStakeUsed,
                    CashoutAmount = message.CashOutStake * message.CashOutStakePercent,
                    CashoutPercent = message.CashOutStakePercent,
                    CashoutDate = settledDate,
                    CashoutDateOffset = settledDate.AddHours(-4),
                    NonTaxedPayout = message.NonTaxedPayout,
                    TaxedStake = message.TaxedStake,
                    TaxedWinnings = message.TaxedWinnings
                });

            }

            isPublish = true;

            context.SaveChanges();
            return isPublish;
        }

        private BetStatus GetStatus(List<BetStatus> statuses, decimal payout, decimal stake)
        {
            var result = BetStatus.Lost;

            var betSelections = statuses;
            
            var lost = betSelections.Where(c => c == BetStatus.Lost).Count();
            var won = betSelections.Where(c => c == BetStatus.Won).Count();
            var halfWon = betSelections.Where(c => c == BetStatus.HalfWon).Count();
            var halfLost = betSelections.Where(c => c == BetStatus.HalfLost).Count();
            var voidd = betSelections.Where(c => c == BetStatus.Void).Count();
            var cancelled = betSelections.Where(c => c == BetStatus.Cancelled).Count();
            var cashout = betSelections.Where(c => c == BetStatus.CashedOut).Count();

            decimal winLoss = payout - stake;

            if (cashout > 0)
                return BetStatus.CashedOut;

            if (betSelections.Contains(BetStatus.Lost))
                return BetStatus.Lost;

            if (cashout == 1 && betSelections.Count == 1)
                return BetStatus.CashedOut;

            if (winLoss == 0 && voidd >= 1)
                return BetStatus.Void;

            if (winLoss == 0 && cancelled >= 1)
                return BetStatus.Cancelled;

            if (halfWon == 1 && betSelections.Count == 1)
                return BetStatus.HalfWon;

            if (halfLost == 1 && betSelections.Count == 1)
                return BetStatus.HalfLost;

            if (halfWon == 1 && betSelections.Where(c => c == BetStatus.Void).Count() == betSelections.Count - 1)
                return BetStatus.HalfWon;

            if (halfLost == 1 && betSelections.Where(c => c == BetStatus.Void).Count() == betSelections.Count - 1)
                return BetStatus.HalfLost;

            if (payout <= 0) //use payout instead of winloss for V3
                return BetStatus.Lost;

            if (payout > 0) //use payout instead of winloss for V3
                return BetStatus.Won;

            return result;
        }


        private Tuple<CashoutAcceptance, string> SendCashoutToMTS(OnCashoutBetSettlement message, IMtsSdk mts)
        {
            try
            {
                _factory = mts.BuilderFactory;

                CashoutAcceptance betStatus;
                string reason;

                var cashoutStake = (long)0;

                if (message.ClientCode.ToUpper() == "RKP")
                    cashoutStake = (long)(message.CashOutStake * .10m) * 10000;
                else
                    cashoutStake = (long)(message.CashOutStake * 10000);

                //var cashoutStake = (long)(message.CashOutStake * 10000);

                var betCashout = _factory.CreateTicketCashoutBuilder().SetTicketId(message.BetId.ToString().ToUpper()).SetCashoutStake(cashoutStake).BuildTicket();

                var response = mts.SendTicketCashoutBlocking(betCashout);

                betStatus = response.Status;
                reason = $"{response.Reason.Code} - {response.Reason.Message}";

                return new Tuple<CashoutAcceptance, string>(betStatus, reason);
            }
            catch (Exception ex)
            {
                return new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }

        private Tuple<CashoutAcceptance, string> SendPartialCashoutToMTS(OnCashoutBetSettlement message, IMtsSdk mts, decimal stake, List<CashOutHistory> cashoutHistory)
        {
            try
            {
                _factory = mts.BuilderFactory;

                CashoutAcceptance betStatus;
                string reason;

                var cashoutStake = (long)0;
                var cashoutStakePercent = (int)0;
                
                var prevCashoutAmount = cashoutHistory.Any() ? cashoutHistory.Sum(c => c.CashoutAmount) : 0m;
                var newCashoutAmount = message.CashOutStake * message.CashOutStakePercent;

                var prevRemainingStake = cashoutHistory.Any() ? cashoutHistory.FirstOrDefault().PostStake : stake;
                var newRemainingStake = prevRemainingStake - (prevRemainingStake * message.CashOutStakePercent); 
                var tempCashoutPercent = (stake - newRemainingStake) / stake;

                if (message.ClientCode.ToUpper() == "RKP")
                {
                    cashoutStake = (long)((prevCashoutAmount + newCashoutAmount) * .10m) * 10000;
                    cashoutStakePercent = (int)((tempCashoutPercent * 100) * .10m) * 10000;
                }
                else
                {
                    cashoutStake = (long)((prevCashoutAmount + newCashoutAmount) * 10000);
                    cashoutStakePercent = (int)((tempCashoutPercent * 100) * 10000);
                }

                var betCashout = _factory.CreateTicketCashoutBuilder().SetTicketId(message.BetId.ToString().ToUpper()).SetCashoutStake(cashoutStake).SetCashoutPercent(cashoutStakePercent).BuildTicket();

                var response = mts.SendTicketCashoutBlocking(betCashout);

                betStatus = response.Status;
                reason = $"{response.Reason.Code} - {response.Reason.Message}";

                return new Tuple<CashoutAcceptance, string>(betStatus, reason);
            }
            catch (Exception ex)
            {
                return new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }

        public void ProcessCashOut(OnCashoutBetSettlement message, CashoutType cashoutType, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {
            try
            {
                if (message.HasLive)
                {
                    Thread.Sleep(Convert.ToInt32(_configuration["Custom:Delay"]));

                    var httpClient = new HttpClient
                    {
                        BaseAddress = new Uri(_configuration["API:PlatformBettingAPI"])
                    };

                    var initial = httpClient.PostAsync("v1/bet/cashout/state", new StringContent(JsonSerializer.Serialize(new CashoutStateRequestDTO { BetId = message.BetId }), System.Text.Encoding.UTF8, "application/json")).Result;

                    if (initial.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<BaseResponseDTO<CashoutStateResponseDTO>>(initial.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        var payload = new OnCashoutBetSettlement
                        {
                            BetId = result.Result.BetId,
                            ClientId = message.ClientId,
                            ClientCode = message.ClientCode,
                            UserId = message.UserId,
                            CashoutType = result.Result.CashoutType,
                            CashOutStake = result.Result.CashOutStake,
                            TaxedStake = result.Result.TaxedStake,
                            TaxedWinnings = result.Result.TaxedWinnings,
                            NonTaxedPayout = result.Result.NonTaxedPayout,
                            HasLive = message.HasLive,
                            Probabilities = result.Result.Probabilities,
                            Endpoint = message.Endpoint,
                            CashOutStakePercent = message.CashOutStakePercent
                        };

                        var isSuccess = true;

                        foreach (var item in message.Probabilities)
                        {
                            var currentProbability = payload.Probabilities.Where(c => c.BetSelectionId == item.BetSelectionId).FirstOrDefault();

                            if (currentProbability == null)
                            {
                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = message.ClientId,
                                    UserId = message.UserId,
                                    BetId = message.BetId.ToString().ToUpper(),
                                    BetStatus = BetStatus.Running,
                                    Message = "Cashout request has been declined"
                                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                //messageBroker.Publish(new OnCashOutResult
                                //{
                                //    ClientCode = message.ClientCode.ToUpper(),
                                //    UserId = payload.UserId,
                                //    BetId = payload.BetId.ToString().ToUpper(),
                                //    BetStatus = BetStatus.Running,
                                //    CashOutStatus = CashOutStatus.Unavailable,
                                //    CashOutStake = 0,
                                //    Message = "Cashout request has been declined",
                                //    ClientId = payload.ClientId,
                                //    Endpoint = message.Endpoint
                                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);

                                Console.WriteLine($"{DateTime.UtcNow} : Live Cashout declined - {message.BetId}");
                                isSuccess = false;
                                break;
                            }
                            else
                            {
                                if (currentProbability.Probability < item.Probability)
                                {

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = message.ClientId,
                                        UserId = message.UserId,
                                        BetId = message.BetId.ToString().ToUpper(),
                                        BetStatus = BetStatus.Running,
                                        Message = "Cashout request has been declined"
                                    }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                    //messageBroker.Publish(new OnCashOutResult
                                    //{
                                    //    ClientCode = message.ClientCode.ToUpper(),
                                    //    UserId = payload.UserId,
                                    //    BetId = payload.BetId.ToString().ToUpper(),
                                    //    BetStatus = BetStatus.Running,
                                    //    CashOutStatus = payload.CashOutStake == 0 ? CashOutStatus.Unavailable : CashOutStatus.Available,
                                    //    CashOutStake = payload.CashOutStake,
                                    //    Message = "Cashout request has been declined",
                                    //    ClientId = message.ClientId,
                                    //    Endpoint = message.Endpoint
                                    //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);

                                    Console.WriteLine($"{DateTime.UtcNow} : Live Cashout declined - {message.BetId}");//
                                    isSuccess = false;
                                    break;
                                }
                            }
                        }

                        if (isSuccess == true)
                        {
                            if(cashoutType == CashoutType.Full)
                                ProcessCashOutSettlement(payload, mts, messageBroker, correlationId);
                            else if (cashoutType == CashoutType.Partial)
                                ProcessPartialCashOutSettlement(payload, mts, messageBroker, correlationId);

                            Console.WriteLine($"{DateTime.UtcNow} : Live Cashout successful - {result.Result.BetId}");
                        }
                    }
                    else
                    {

                        messageBroker.Publish(new OnBetStatus
                        {
                            ClientId = message.ClientId,
                            UserId = message.UserId,
                            BetId = message.BetId.ToString().ToUpper(),
                            BetStatus = BetStatus.Running,
                            Message = "Cashout request has been declined"
                        }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                        //messageBroker.Publish(new OnCashOutResult
                        //{
                        //    ClientCode = message.ClientCode.ToUpper(),
                        //    UserId = message.UserId,
                        //    BetId = message.BetId.ToString().ToUpper(),
                        //    BetStatus = BetStatus.Running,
                        //    CashOutStatus = CashOutStatus.Unavailable,
                        //    CashOutStake = 0,
                        //    Message = "Cashout request has been declined",
                        //    ClientId = message.ClientId,
                        //    Endpoint = message.Endpoint
                        //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);

                        Console.WriteLine($"{DateTime.UtcNow} : Live Cashout declined - {message.BetId}");
                    }
                }
                else
                {
                    if (cashoutType == CashoutType.Full)
                        ProcessCashOutSettlement(message, mts, messageBroker, correlationId);
                    else if (cashoutType == CashoutType.Partial)
                        ProcessPartialCashOutSettlement(message, mts, messageBroker, correlationId);

                    Console.WriteLine($"{DateTime.UtcNow} : Prematch Cashout successful - {message.BetId}");
                }

            }
            catch (Exception ex)
            {
                //messageBroker.Publish(new OnCashOutResult
                //{
                //    ClientCode = message.ClientCode.ToUpper(),
                //    UserId = message.UserId,
                //    BetId = message.BetId.ToString().ToUpper(),
                //    BetStatus = BetStatus.Running,
                //    CashOutStatus = CashOutStatus.Unavailable,
                //    CashOutStake = 0,
                //    Message = "Cashout request has been declined",
                //    ClientId = message.ClientId,
                //    Endpoint = message.Endpoint
                //}, "OnBetCashOutResult", $"r.bet.operator.cashout.{message.ClientCode.ToUpper()}", correlationId);

                Console.WriteLine($"{DateTime.UtcNow} : Error: {ex.Message} -> Payload: {JsonSerializer.Serialize(message)}");
            }
        }
    }
}
