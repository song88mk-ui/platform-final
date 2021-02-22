using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Linq;
using System.Transactions;

namespace Platform.Placement.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    try
                    {
                        var bet = context.Bet.Find(message.BetId);                        
                        if (bet != null)
                        {
                            var betStatus = BetStatus.Running;

                            bet.BetStatusId = (int)betStatus;
                            bet.BetStatusName = betStatus.ToString();
                            bet.Reason = "Accepted (MTS disabled)";

                            context.Bet.Update(bet);

                            context.BetAudit.Add(new BetAudit
                            {
                                BetId = bet.BetId,
                                UserId = bet.UserId,
                                Stake = bet.Stake,
                                TrueStake = bet.TrueStake,
                                Odds = bet.Odds,
                                OddsTypeId = bet.OddsTypeId,
                                OddsTypeName = bet.OddsTypeName,
                                Payout = bet.Payout,
                                WinLoss = bet.WinLoss,
                                BetStatusId = bet.BetStatusId,
                                BetStatusName = bet.BetStatusName,
                                Reason = bet.Reason,
                                CurrencyCode = bet.CurrencyCode,
                                LanguageCode = bet.LanguageCode,
                                ExchangeRate = bet.ExchangeRate,
                                IPAddress = bet.IPAddress,
                                Channel = bet.Channel,
                                ClientId = bet.ClientId,
                                ClientCode = bet.ClientCode,
                                LimitId = bet.LimitId,
                                Endpoint = bet.Endpoint,
                                OperatorToken = bet.OperatorToken,
                                CompanyShare = bet.CompanyShare,
                                IsResettled = bet.IsResettled,
                                IsCashout = bet.IsCashout,
                                BetDate = bet.BetDate,
                                BetDateOffset = bet.BetDateOffset,
                                SettlementDate = bet.SettlementDate,
                                SettlementDateOffset = bet.SettlementDateOffset
                            });

                            var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                            foreach (var betSelection in betSelections)
                            {
                                betSelection.BetSelectionStatusId = bet.BetStatusId;
                                betSelection.BetSelectionStatusName = bet.BetStatusName;

                                context.BetSelection.Update(betSelection);

                                context.BetSelectionAudit.Add(new BetSelectionAudit
                                {
                                    BetId = betSelection.BetId,
                                    BetSelectionId = betSelection.BetSelectionId,
                                    UserId = betSelection.UserId,
                                    SportId = betSelection.SportId,
                                    SportName = betSelection.SportName,
                                    SegmentId = betSelection.SegmentId,
                                    SegmentName = betSelection.SegmentName,
                                    LeagueId = betSelection.LeagueId,
                                    LeagueName = betSelection.LeagueName,
                                    FixtureId = betSelection.FixtureId,
                                    FixtureName = betSelection.FixtureName,
                                    FixtureTypeId = betSelection.FixtureTypeId,
                                    FixtureTypeName = betSelection.FixtureTypeName,
                                    MarketTemplateId = betSelection.MarketTemplateId,
                                    MarketId = betSelection.MarketId,
                                    MarketName = betSelection.MarketName,
                                    SelectionTemplateId = betSelection.SelectionTemplateId,
                                    SelectionId = betSelection.SelectionId,
                                    SelectionName = betSelection.SelectionName,
                                    FixtureStatusId = betSelection.FixtureStatusId,
                                    FixtureStatusName = betSelection.FixtureStatusName,
                                    Line = betSelection.Line,
                                    Odds = betSelection.Odds,
                                    BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                    BetSelectionStatusName = betSelection.BetSelectionStatusName,
                                    IsSettled = false,
                                    IsResettled = false,
                                    FixtureSpecifier = betSelection.FixtureSpecifier,
                                    SelectionSpecifier = betSelection.SelectionSpecifier,
                                    FixtureStartDate = betSelection.FixtureStartDate,
                                    BetHomeScore = betSelection.BetHomeScore,
                                    BetAwayScore = betSelection.BetAwayScore,
                                    SettlementHomeScore = string.Empty,
                                    SettlementAwayScore = string.Empty,
                                    CorrelationId = betSelection.CorrelationId
                                });
                            }

                            context.SaveChanges();
                        }

                        scope.Complete();
                        messageBroker.Publish(message, "OnBetPlacement", "r.bet.operator", correlationId);

                        var selections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();
                        ReportUtility.ReportBet(messageBroker, ReportAdapter.ConvertToBetModel(bet, selections, _configuration["ConnectionStrings:Platform"]));
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}
