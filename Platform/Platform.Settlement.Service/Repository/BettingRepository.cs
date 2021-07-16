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
        private List<BetSelection> GetBetSelectionsForCancellation(long selectionId)
        {
            var result = new List<BetSelection>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    try
                    {
                        var betSelections = context.BetSelection.AsNoTracking().Where(c => c.SelectionId == selectionId && c.IsSettled == false).ToList();
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

            var betSelections = statuses;
            betSelections.Add(status);

            var lost = betSelections.Where(c => c == BetStatus.Lost).Count();
            var won = betSelections.Where(c => c == BetStatus.Won).Count();
            var halfWon = betSelections.Where(c => c == BetStatus.HalfWon).Count();
            var halfLost = betSelections.Where(c => c == BetStatus.HalfLost).Count();
            var voidd = betSelections.Where(c => c == BetStatus.Void).Count();
            var cancelled = betSelections.Where(c => c == BetStatus.Cancelled).Count();

            decimal winLoss = payout - stake;

            if (betSelections.Contains(BetStatus.Lost))
                return BetStatus.Lost;

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
                                isPublish = SettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection);
                            else
                                isPublish = ResettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection);

                            scope.Complete();

                            if (isPublish)
                            {
                                if (payload.Bet.BetStatusId == (int)BetStatus.Cancelled)
                                {
                                    messageBroker.Publish(payload, "OnBetCancellation", "r.bet.operator", correlationId);

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                        UserId = payload.Bet.UserId,
                                        BetId = payload.Bet.BetId.ToString().ToUpper(),
                                        BetStatus = (BetStatus)payload.Bet.BetStatusId
                                    }, "OnBetStatus", "r.bet.betstatus", correlationId);
                                }
                                else
                                {
                                    messageBroker.Publish(payload, "OnBetSettlement", "r.bet.operator", correlationId);

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
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }
        private DateTime ConvertToDateTime(double timestamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dtDateTime.AddMilliseconds(timestamp).ToUniversalTime();
        }
        public void ProcesssBetSettlementForCancellation(OnUOFBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            var betSelections = GetBetSelectionsForCancellation(message.SelectionId);

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
                            var bet = context.Bet.AsNoTracking().Where(c => c.BetId == betSelection.BetId).FirstOrDefault();

                            var betDate = bet.BetDate;

                            var isStartTimeIncluded = false;
                            var isEndTimeIncluded = false;

                            var startTime = DateTime.UtcNow;
                            var endTime = DateTime.UtcNow;

                            if (message.StartTime > 0)
                            {
                                isStartTimeIncluded = true;
                                startTime = ConvertToDateTime(message.StartTime);
                            }

                            if (message.EndTime > 0)
                            {
                                isEndTimeIncluded = true;
                                endTime = ConvertToDateTime(message.EndTime);
                            }

                            if (isStartTimeIncluded == true && isEndTimeIncluded == true)
                            {
                                if (betDate < startTime)
                                {
                                    scope.Complete();
                                    continue;
                                }

                                if (betDate > endTime)
                                {
                                    scope.Complete();
                                    continue;
                                }
                            }

                            if (isStartTimeIncluded == true && isEndTimeIncluded == false)
                            {
                                if (betDate < startTime)
                                {
                                    scope.Complete();
                                    continue;
                                }
                            }

                            if (isStartTimeIncluded == false && isEndTimeIncluded == true)
                            {
                                if (betDate > endTime)
                                {
                                    scope.Complete();
                                    continue;
                                }
                            }

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

                            isPublish = SettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection);

                            scope.Complete();

                            if (isPublish)
                            {
                                if (payload.Bet.BetStatusId == (int)BetStatus.Cancelled)
                                {
                                    messageBroker.Publish(payload, "OnBetCancellation", "r.bet.operator", correlationId);

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                        UserId = payload.Bet.UserId,
                                        BetId = payload.Bet.BetId.ToString().ToUpper(),
                                        BetStatus = (BetStatus)payload.Bet.BetStatusId
                                    }, "OnBetStatus", "r.bet.betstatus", correlationId);
                                }
                                else
                                {
                                    messageBroker.Publish(payload, "OnBetSettlement", "r.bet.operator", correlationId);

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
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        private bool SettleBet(OnUOFBetSettlement message, BetSelection betSelection, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, BetSelection filteredSelection)
        {
            if (filteredSelection.BetSelectionStatusId != (int)BetStatus.Running)
                return false;

            filteredSelection.BetSelectionStatusId = (int)message.SelectionStatus;
            filteredSelection.BetSelectionStatusName = message.SelectionStatus.ToString();
            filteredSelection.SettlementScore = message.SettlementScore;
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
                BetScore = betSelectionEntity.BetScore,
                SettlementScore = betSelectionEntity.SettlementScore,
                Probability = betSelectionEntity.Probability,
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

                    bet.TaxedStake = 0;
                    bet.TaxedWinnings = 0;

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
                        var taxedStake = bet.TrueStake - (bet.TrueStake / (1 + bet.StakeTax));
                        var stake = message.SelectionStatus == BetStatus.Cancelled ? bet.TrueStake : bet.TrueStake - taxedStake;
                        var payout = GetPayout(selections.Select(s => new Tuple<BetStatus, decimal>((BetStatus)s.BetSelectionStatusId, s.Odds)).ToList(), stake);

                        var taxedWinnings = (payout - bet.TrueStake) * bet.WinningsTax;

                        if (payout > bet.TrueStake)
                        {
                            payout = payout - taxedWinnings;
                        }
                        else
                            taxedWinnings = 0;

                        var betStatus = GetStatus(selections.Where(c => c.SelectionId != betSelection.SelectionId).Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), message.SelectionStatus, payout, stake);

                        bet.WinLoss = payout == 0 ? -bet.TrueStake : payout - bet.TrueStake;
                        bet.BetStatusId = (int)betStatus;
                        bet.BetStatusName = betStatus.ToString();
                        bet.Payout = payout;
                        bet.SettlementDate = settledDate;
                        bet.SettlementDateOffset = settledDate.AddHours(-4);
                        bet.TaxedStake = message.SelectionStatus == BetStatus.Cancelled ? 0 : taxedStake;
                        bet.TaxedWinnings = taxedWinnings;

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

                        ProcessStakeFixtureLimit(message, betSelection, context, selections, betEntity);

                        isPublish = true;
                        payload.Bet = betEntity;
                        payload.BetSelections = selections;
                    }
                }
            }

            context.SaveChanges();
            return isPublish;
        }

        private static void ProcessStakeFixtureLimit(OnUOFBetSettlement message, BetSelection betSelection, BettingContext context, List<BetSelection> selections, Bet betEntity)
        {
            if (selections.Count == 1 && message.SelectionStatus == BetStatus.Cancelled)
            {
                var stakeLimitFixture = context.StakeLimitFixture.Find($"{betEntity.ClientCode}-{betEntity.UserId}", betSelection.FixtureId);

                if (stakeLimitFixture != null)
                {
                    stakeLimitFixture.Remaining += betEntity.TrueStake;

                    context.StakeLimitFixture.Update(stakeLimitFixture);
                }
            }
        }

        private bool ResettleBet(OnUOFBetSettlement message, BetSelection betSelection, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, BetSelection filteredSelection)
        {
            if (filteredSelection.BetSelectionStatusId == (int)message.SelectionStatus || filteredSelection.BetSelectionStatusId == (int)BetStatus.Waiting || filteredSelection.BetSelectionStatusId == (int)BetStatus.Cancelled || filteredSelection.BetSelectionStatusId == (int)BetStatus.Rejected || filteredSelection.BetSelectionStatusId == (int)BetStatus.CashedOut)
                return false;

            filteredSelection.BetSelectionStatusId = (int)message.SelectionStatus;
            filteredSelection.BetSelectionStatusName = message.SelectionStatus.ToString();
            filteredSelection.SettlementScore = message.SettlementScore;
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
                BetScore = betSelectionEntity.BetScore,
                SettlementScore = betSelectionEntity.SettlementScore,
                Probability = betSelectionEntity.Probability,
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

                    bet.TaxedStake = 0;
                    bet.TaxedWinnings = 0;

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
                        var taxedStake = bet.TrueStake - (bet.TrueStake / (1 + bet.StakeTax));
                        var stake = message.SelectionStatus == BetStatus.Cancelled ? bet.TrueStake : bet.TrueStake - taxedStake;
                        var payout = GetPayout(selections.Select(s => new Tuple<BetStatus, decimal>((BetStatus)s.BetSelectionStatusId, s.Odds)).ToList(), stake);

                        var taxedWinnings = (payout - bet.TrueStake) * bet.WinningsTax;

                        if (payout > bet.TrueStake)
                            payout = payout - taxedWinnings;
                        else
                            taxedWinnings = 0;

                        var betStatus = GetStatus(selections.Where(c => c.SelectionId != betSelection.SelectionId).Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), message.SelectionStatus, payout, stake);

                        bet.WinLoss = payout == 0 ? -bet.TrueStake : payout - bet.TrueStake;
                        bet.BetStatusId = (int)betStatus;
                        bet.BetStatusName = betStatus.ToString();
                        bet.Payout = payout;
                        bet.SettlementDate = settledDate;
                        bet.SettlementDateOffset = settledDate.AddHours(-4);
                        bet.TaxedStake = message.SelectionStatus == BetStatus.Cancelled ? 0 : taxedStake;
                        bet.TaxedWinnings = taxedWinnings;

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
                    SettlementScore = message.SettlementScore,
                    StartTime = message.StartTime,
                    EndTime = message.EndTime
                };

                ProcesssBetSettlementForCancellation(cancellation, messageBroker, correlationId);
            }
        }

        public void ProcessBackOfficeBetCancellation(OnBOBetCancellaton message, IMessageBroker messageBroker, string correlationId)
        {
            foreach (var betItem in message.Bets)
            {
                var betData = new Bet();
                var betSelectionData = new List<BetSelection>();
                var settledDate = DateTime.UtcNow;

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        try
                        {
                            var bet = context.Bet.Find(betItem.BetId);

                            if (bet == null || bet.BetStatusId == (int)BetStatus.Waiting || bet.BetStatusId == (int)BetStatus.Cancelled || bet.BetStatusId == (int)BetStatus.Rejected)
                            {
                                scope.Complete();
                                continue;
                            }

                            //if (bet == null || bet.BetStatusId == (int)BetStatus.Waiting || bet.BetStatusId == (int)BetStatus.CashedOut || bet.BetStatusId == (int)BetStatus.Cancelled || bet.BetStatusId == (int)BetStatus.Rejected)
                            //{
                            //    scope.Complete();
                            //    continue;
                            //}

                            var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                            if (betSelections.Count == 0)
                            {
                                scope.Complete();
                                continue;
                            }

                            var reason = string.IsNullOrEmpty(betItem.Reason) ? "Cancelled by Backoffice" : betItem.Reason;

                            var payout = GetPayout(betSelections.Select(s => new Tuple<BetStatus, decimal>(BetStatus.Cancelled, s.Odds)).ToList(), bet.TrueStake);

                            bet.Reason = $"103 - {reason}";
                            bet.WinLoss = payout == 0 ? -bet.Stake : payout - bet.Stake;
                            bet.BetStatusId = (int)BetStatus.Cancelled;
                            bet.BetStatusName = BetStatus.Cancelled.ToString();
                            bet.Payout = payout;
                            bet.SettlementDate = settledDate;
                            bet.SettlementDateOffset = settledDate.AddHours(-4);
                            bet.TaxedStake = 0;
                            bet.TaxedWinnings = 0;

                            var betEntity = context.Bet.Update(bet).Entity;
                            betData = betEntity;

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

                            foreach (var betSelection in betSelections)
                            {
                                betSelection.BetSelectionStatusId = bet.BetStatusId;
                                betSelection.BetSelectionStatusName = bet.BetStatusName;
                                betSelection.IsSettled = true;

                                var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                betSelectionData.Add(betSelectionEntity);

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
                                    CorrelationId = betSelectionEntity.CorrelationId
                                });
                            }

                            ProcessStakeFixtureLimit(betSelectionData, context, betEntity);

                            context.SaveChanges();
                            scope.Complete();

                            messageBroker.Publish(new OnBetSettlement
                            {
                                Bet = betData,
                                BetSelections = betSelectionData
                            }, "OnBetCancellation", "r.bet.operator", correlationId);

                            messageBroker.Publish(new OnBetStatus
                            {
                                ClientId = betData.ClientId.ToString().ToUpper(),
                                UserId = betData.UserId,
                                BetId = betData.BetId.ToString().ToUpper(),
                                BetStatus = (BetStatus)betData.BetStatusId
                            }, "OnBetStatus", "r.bet.betstatus", correlationId);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }
        private static void ProcessStakeFixtureLimit(List<BetSelection> betSelectionData, BettingContext context, Bet betEntity)
        {
            if (betSelectionData.Count == 1)
            {
                var stakeLimitFixture = context.StakeLimitFixture.Find($"{betEntity.ClientCode}-{betEntity.UserId}", betSelectionData.First().FixtureId);

                if (stakeLimitFixture != null)
                {
                    stakeLimitFixture.Remaining += betEntity.TrueStake;

                    context.StakeLimitFixture.Update(stakeLimitFixture);
                }
            }
        }
    }
}
