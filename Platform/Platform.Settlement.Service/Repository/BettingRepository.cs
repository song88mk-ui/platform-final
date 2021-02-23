using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
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
        private List<BetSelection> GetBetSelectionsByMarketId(long marketId)
        {
            var result = new List<BetSelection>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    try
                    {
                        var betSelections = context.BetSelection.AsNoTracking().Where(c => c.MarketId == marketId).ToList();
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
                    case BetStatus.Cancelled:
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
            var cancelled = bets.Where(c => c == BetStatus.Cancelled).Count();

            if (cancelled > 0)
                result = BetStatus.Cancelled;

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

                            if (selections.Count == 0)
                            {
                                scope.Complete();
                                continue;
                            }

                            var filteredSelection = selections.Where(c => c.SelectionId == message.SelectionId).FirstOrDefault();

                            if (filteredSelection == null)
                            {
                                scope.Complete();
                                continue;
                            }

                            if (filteredSelection.IsSettled == false)
                            {
                                if (filteredSelection.BetSelectionStatusId == (int)BetStatus.Waiting)
                                {
                                    scope.Complete();
                                    continue;
                                }

                                isPublish = SettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection);
                            }
                            else
                            {
                                if (message.SelectionStatus == BetStatus.Cancelled)
                                {
                                    scope.Complete();
                                    continue;
                                }

                                //TODO: resettlement here
                                //void then apply correct status

                                isPublish = ResettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection);
                            }

                            scope.Complete();

                            if (isPublish)
                            {
                                if (payload.Bet.BetStatusId == (int)BetStatus.Cancelled)
                                {
                                    messageBroker.Publish(payload, "OnBetCancellation", "r.bet.operator", correlationId);
                                    //ReportUtility.ReportCancellation(messageBroker, payload.Bet, payload.BetSelections);
                                }
                                else
                                {
                                    messageBroker.Publish(payload, "OnBetSettlement", "r.bet.operator", correlationId);
                                    //ReportUtility.ReportSettlement(messageBroker, payload.Bet, payload.BetSelections);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        private bool SettleBet(OnUOFBetSettlement message, BetSelection betSelection, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, BetSelection filteredSelection)
        {
            if (filteredSelection.BetSelectionStatusId == (int)BetStatus.Waiting)
                return false;

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

            if (message.SelectionStatus == BetStatus.Lost)
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
            else
            {
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
            }

            context.SaveChanges();
            return isPublish;
        }
        private bool ResettleBet(OnUOFBetSettlement message, BetSelection betSelection, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, BetSelection filteredSelection)
        {
            if (filteredSelection.BetSelectionStatusId == (int)message.SelectionStatus)
                return false;

            filteredSelection.BetSelectionStatusId = (int)message.SelectionStatus;
            filteredSelection.BetSelectionStatusName = message.SelectionStatus.ToString();
            filteredSelection.SettlementHomeScore = message.SettlementHomeScore;
            filteredSelection.SettlementAwayScore = message.SettlementAwayScore;
            filteredSelection.IsSettled = true;
            filteredSelection.IsResettled = true;

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

            if (message.SelectionStatus == BetStatus.Lost)
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
                        SettlementDateOffset = betEntity.SettlementDateOffset
                    });

                    isPublish = true;
                    payload.Bet = betEntity;
                    payload.BetSelections = selections;
                }
            }
            else
            {
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
                            SettlementDateOffset = betEntity.SettlementDateOffset
                        });

                        isPublish = true;
                        payload.Bet = betEntity;
                        payload.BetSelections = selections;
                    }
                }
            }

            context.SaveChanges();
            return isPublish;
        }

        public void ProcessBetCancellation(OnUOFBetCancellation message, IMessageBroker messageBroker, string correlationId)
        {
            var betSelections = GetBetSelectionsByMarketId(message.MarketId);

            var selectionIds = betSelections.Select(s => s.SelectionId).Distinct().ToList();

            foreach (var selectionId in selectionIds)
            {
                var cancellation = new OnUOFBetSettlement
                {
                    SelectionId = selectionId,
                    SelectionStatus = message.MarketStatus,
                    SettlementHomeScore = message.SettlementHomeScore,
                    SettlementAwayScore = message.SettlementAwayScore
                };

                ProcesssBetSettlement(cancellation, messageBroker, correlationId);
            }
        }
    }
}
