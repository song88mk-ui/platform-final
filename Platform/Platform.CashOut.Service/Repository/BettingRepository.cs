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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

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

        public void ProcesssCashoutBetSettlement(OnCashoutBetSettlement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
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
                            if (bet.BetStatusId != (int)BetStatus.Running)
                            {
                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = bet.ClientId.ToString().ToUpper(),
                                    UserId = bet.UserId,
                                    BetId = bet.BetId.ToString().ToUpper(),
                                    BetStatus = (BetStatus)bet.BetStatusId,
                                    Message = "Cashout request has been declined"
                                }, "OnBetStatus", "r.bet.betstatus", correlationId);
                                return;
                            }
                                
                            var isPublish = false;
                            var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ?
                                SendCashoutToMTS(message, mts) : new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Accepted, "1024 - Cashout Accepted (MTS disabled)");

                            if (result.Item1 == CashoutAcceptance.Accepted)
                            {
                                var payload = new OnBetSettlement();
                                var selections = context.BetSelection.Where(c => c.BetId == message.BetId).ToList();
                                if (selections.Count == 0)
                                {
                                    scope.Complete();
                                    return;
                                }

                                var filteredSelections = selections.Where(c => c.BetId == message.BetId && c.IsSettled == false).ToList();

                                if (filteredSelections.Count == 0)
                                {
                                    scope.Complete();
                                    return;
                                }

                                bet.Reason = $"{result.Item2} (MTS)";

                                isPublish = SettleBet(message, bet, isPublish, payload, settledDate, context, selections, filteredSelections);

                                context.SaveChanges();

                                scope.Complete();

                                if (isPublish)
                                {
                                    messageBroker.Publish(payload, "OnBetFullCashOut", "r.bet.operator", correlationId);

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                        UserId = payload.Bet.UserId,
                                        BetId = payload.Bet.BetId.ToString().ToUpper(),
                                        BetStatus = (BetStatus)payload.Bet.BetStatusId,
                                        Message = "Cashout request has been accepted"
                                    }, "OnBetStatus", "r.bet.betstatus", correlationId);
                                }
                            }
                            else
                            {
                                messageBroker.Publish(new OnBetStatus
                                {
                                    ClientId = bet.ClientId.ToString().ToUpper(),
                                    UserId = bet.UserId,
                                    BetId = bet.BetId.ToString().ToUpper(),
                                    BetStatus = BetStatus.Running,
                                    Message = "Cashout request has been declined"
                                }, "OnBetStatus", "r.bet.betstatus", correlationId);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                messageBroker.Publish(new OnBetStatus
                {
                    ClientId = bet.ClientId.ToString().ToUpper(),
                    UserId = bet.UserId,
                    BetId = message.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Running,
                    Message = "Cashout request has been declined"
                }, "OnBetStatus", "r.bet.betstatus", correlationId);
            }
        }


        private bool SettleBet(OnCashoutBetSettlement message, Bet bet, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, List<BetSelection> filteredSelections)
        {

            foreach (var selection in filteredSelections)
            {
                var tempSelection = message.Probabilities.Where(c => c.BetSelectionId == selection.BetSelectionId).FirstOrDefault();

                selection.IsSettled = true;
                selection.BetSelectionStatusId = (int)BetStatus.CashedOut;
                selection.BetSelectionStatusName = BetStatus.CashedOut.ToString();

                if (tempSelection != null)
                {
                    selection.WonProbability = tempSelection.WonProbability;
                    selection.LostProbability = tempSelection.LostProbability;
                    selection.VoidProbability = tempSelection.VoidProbability;
                    selection.HalfWonProbability = tempSelection.HalfWonProbability;
                    selection.HalfLostProbability = tempSelection.HalfLostProbability;
                }

                var betSelectionEntity = context.BetSelection.Update(selection).Entity;

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

            var stake = bet.TrueStake - bet.TaxedStake;
            var payout = message.CashOutStake;
            var winloss = payout - bet.TrueStake;

            var betStatus = GetStatus(selections.Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), payout, stake);

            bet.WinLoss = winloss;
            bet.BetStatusId = (int)betStatus;
            bet.BetStatusName = betStatus.ToString();

            bet.Payout = payout;
            bet.SettlementDate = settledDate;
            bet.SettlementDateOffset = settledDate.AddHours(-4);

            bet.TaxedStake = message.TaxedStake;
            bet.TaxedWinnings = message.TaxedWinnings;
            
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
                TaxedWinnings = betEntity.TaxedWinnings
            });

            isPublish = true;
            payload.Bet = betEntity;
            payload.BetSelections = selections;

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

            if (winLoss <= 0)
                return BetStatus.Lost;

            if (winLoss > 0)
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

                var cashoutStake = (long)(message.CashOutStake * 10000);

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

    }
}
