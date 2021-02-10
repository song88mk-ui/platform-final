using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Platform.Settlement.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        private List<BetSelection> GetBetSelections(long selectionId)
        {
            var result = new List<BetSelection>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    try
                    {
                        var betSelections = context.BetSelection.AsNoTracking().Where(c => c.SelectionId == selectionId).ToList();
                        if (betSelections.Count > 0)
                            result.AddRange(betSelections);

                        scope.Complete();
                    }
                    catch (Exception)
                    {
                        return result;
                    }
                }
            }

            return result;
        }
        private decimal GetPayout(List<Tuple<BetStatus, decimal>> selections, decimal stake)
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
                        values.Add(selection.Item2);
                        break;
                    case BetStatus.Void:
                        values.Add(1m);
                        break;
                }
            }

            return values.Aggregate((a, b) => a * b) * stake;
        }
        private BetStatus GetStatus(List<BetStatus> statuses, BetStatus status, decimal payout, decimal stake)
        {
            var result = BetStatus.Lost;

            var bets = statuses;
            bets.Add(status);

            var lost = bets.Where(c => c == BetStatus.Lost).Count();
            var won = bets.Where(c => c == BetStatus.Won).Count();
            var halfWon = bets.Where(c => c == BetStatus.HalfWon).Count();
            var halfLost = bets.Where(c => c == BetStatus.HalfLost).Count();
            var voidd = bets.Where(c => c == BetStatus.Void).Count();

            if (voidd > 0)
                result = BetStatus.Void;

            if (halfLost == 1)
                result = payout == (stake / 2) ? BetStatus.HalfLost : BetStatus.Lost;

            if (halfLost > 1)
                result = BetStatus.Lost;

            if (halfWon == 1)
                result = (payout - stake) > stake ? BetStatus.HalfWon : BetStatus.Lost;

            if (halfWon > 1)
                result = (payout - stake) > stake ? BetStatus.Won : BetStatus.Lost;

            if (won > 0)
                result = BetStatus.Won;

            if (lost > 0)
                result = BetStatus.Lost;

            return result;
        }
        public void ProcesssBetSettlement(OnUOFBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            var betSelections = GetBetSelections(message.SelectionId);

            foreach (var betSelection in betSelections)
            {
                var isPublish = false;
                var payload = new OnBetSettlement();
                var settledDate = DateTime.UtcNow;

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        try
                        {
                            var selections = context.BetSelection.Where(c => c.BetId == betSelection.BetId).ToList();

                            if (selections.Count == 0) continue;

                            var filteredSelection = selections.Where(c => c.SelectionId == message.SelectionId).FirstOrDefault();

                            if (filteredSelection == null) continue;

                            if (filteredSelection.IsSettled == false)
                            {
                                filteredSelection.BetSelectionStatusId = (int)message.SelectionStatus;
                                filteredSelection.BetSelectionStatusName = message.SelectionStatus.ToString();
                                filteredSelection.SettlementHomeScore = message.SettlementHomeScore;
                                filteredSelection.SettlementAwayScore = message.SettlementAwayScore;
                                filteredSelection.IsSettled = true;

                                var betSelectionEntity = context.BetSelection.Update(filteredSelection).Entity;

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
                                    BetHomeScore = betSelectionEntity.BetHomeScore,
                                    BetAwayScore = betSelectionEntity.BetAwayScore,
                                    SettlementHomeScore = message.SettlementHomeScore,
                                    SettlementAwayScore = message.SettlementAwayScore,
                                    CorrelationId = betSelection.CorrelationId
                                });

                                var totalSettled = selections.Where(c => c.SelectionId != message.SelectionId && c.IsSettled == true).Count();
                                var totalSelections = selections.Where(c => c.SelectionId != message.SelectionId).Count();

                                if (totalSettled == totalSelections)
                                {
                                    var bet = context.Bet.Find(betSelection.BetId);

                                    if (bet != null)
                                    {
                                        var payout = GetPayout(selections.Select(s => new Tuple<BetStatus, decimal>((BetStatus)s.BetSelectionStatusId, s.Odds)).ToList(), bet.TrueStake);

                                        var betStatus = GetStatus(selections.Where(c => c.SelectionId != betSelection.SelectionId).Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), message.SelectionStatus, payout, bet.TrueStake);

                                        bet.WinLoss = payout == 0 ? -bet.Stake : payout - bet.Stake;
                                        bet.BetStatusId = (int)betStatus;
                                        bet.BetStatusName = betStatus.ToString();
                                        bet.Payout = payout;
                                        bet.SettlementDate = settledDate;
                                        bet.SettlementDateOffset = settledDate.AddHours(-4);

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
                                            ClientCode = betEntity.ClientCode,
                                            LimitId = betEntity.LimitId,
                                            Endpoint = betEntity.Endpoint,
                                            OperatorToken = betEntity.OperatorToken,
                                            CompanyShare = betEntity.CompanyShare,
                                            IsResettled = betEntity.IsResettled,
                                            IsCashout = betEntity.IsCashout,
                                            BetDate = betEntity.BetDate,
                                            BetDateOffset = betEntity.BetDateOffset,
                                            SettlementDate = betEntity.SettlementDate,
                                            SettlementDateOffset = betEntity.SettlementDateOffset
                                        });

                                        isPublish = true;
                                        payload.Bet = betEntity;
                                        payload.BetSelections = selections;
                                    }
                                }

                                context.SaveChanges();
                            }
                            scope.Complete();

                            if (isPublish)
                            {
                                messageBroker.Publish(payload, "OnBetSettlement", "r.bet.operator", string.IsNullOrEmpty(correlationId)? Guid.NewGuid().ToString().ToUpper() : correlationId);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }
    }
}
