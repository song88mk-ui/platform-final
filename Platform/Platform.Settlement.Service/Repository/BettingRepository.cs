using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using Platform.Settlement.Service.Telemetry;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Transactions;
using System.Xml.Schema;

namespace Platform.Settlement.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BettingRepository> _logger;

        // counters
        private readonly Counter<long> _betSettlementCounter;
        private readonly Counter<long> _betCancellationCounter;
        private readonly Counter<long> _betBoCancellationCounter;
        private readonly Counter<long> _betBoManualSettlementCounter;

        private readonly Histogram<double> _betSettlementDurationHistogram;
        private readonly Histogram<double> _betCancellationDurationHistogram;
        private readonly Histogram<double> _betBoCancellationDurationHistogram;
        private readonly Histogram<double> _betBoManualSettlementDurationHistogram;

        public BettingRepository(IConfiguration configuration, ILogger<BettingRepository> logger, Instrumentation instrumentation)
        {
            _configuration = configuration;
            _logger = logger;

            _betSettlementCounter = instrumentation.OnBetSettlementCounter;
            _betCancellationCounter = instrumentation.OnBetCancellationCounter;
            _betBoCancellationCounter = instrumentation.OnBetBackOfficeCancellationCounter;
            _betBoManualSettlementCounter = instrumentation.OnBetBackOfficeManualSettlementCounter;

            _betSettlementDurationHistogram = instrumentation.OnBetSettlementDurationHistogram;
            _betCancellationDurationHistogram = instrumentation.OnBetCancellationDurationHistogram;
            _betBoCancellationDurationHistogram = instrumentation.OnBetBackOfficeCancellationDurationHistogram;
            _betBoManualSettlementDurationHistogram = instrumentation.OnBetBackOfficeManualSettlementDurationHistogram;
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
        private Tuple<bool, ParlayBonusAudit> GetParlayBetConfig(BettingContext context, Guid clientId, string currencyCode, DateTime betDate, List<BetSelection> selections, decimal payout, ref decimal parlayBonusWinnings)
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
                {
                    parlayBonusWinnings = payout * result.ParlayBonus;

                    if (parlayBonusWinnings > result.ParlayBonusMaxWinnings && result.ParlayBonusMaxWinnings > 0)
                    {
                        parlayBonusWinnings = 0;
                        return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());
                    }
                    else
                        return new Tuple<bool, ParlayBonusAudit>(true, result);
                }
            }
            else
                return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());

        }

        private decimal GetPayout(List<Tuple<BetStatus, decimal, decimal, bool>> selections, decimal trueStake, decimal stake, bool isBetBuilder, decimal odds, OddsType oddsType)
        {
            var values = new List<decimal>();

            if (selections.Count == 1 && OddsTypeHelper.GetOddsByOddsType((int)oddsType, selections.First().Item2, selections.First().Item4) < 0 && (oddsType == OddsType.Indonesian || oddsType == OddsType.Malaysian))
            {
                switch (selections.First().Item1)
                {
                    case BetStatus.Lost:
                        return 0m * trueStake;

                    case BetStatus.HalfWon:
                        return (stake / 2) + trueStake;

                    case BetStatus.HalfLost:
                        return 0.5m * trueStake;

                    case BetStatus.Won:
                        return trueStake + stake;

                    case BetStatus.Void:
                        return 1m * trueStake;

                    case BetStatus.Cancelled:
                        return 1m * trueStake;
                }
            }

            if (isBetBuilder == true)
            {
                if (selections.Count() == selections.Where(s => s.Item1 == BetStatus.Won).Count())
                    return (odds * trueStake);
                else if (selections.Any(s => s.Item1 == BetStatus.Lost))
                    return 0m;
                else if (selections.Any(s => s.Item1 == BetStatus.Void))
                    return trueStake;
                else if (selections.Any(s => s.Item1 == BetStatus.Cancelled))
                    return trueStake;
                else
                    return trueStake;
            }

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
                        values.Add(selection.Item2 * selection.Item3);
                        break;
                    case BetStatus.Void:
                        values.Add(1m);
                        break;
                    case BetStatus.Cancelled:
                        values.Add(1m);
                        break;
                }
            }

            return values.Aggregate((a, b) => a * b) * trueStake;
        }
        private BetStatus GetStatus(List<BetStatus> statuses, BetStatus status, decimal payout, decimal stake, bool isBetBuilder)
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
            var running = betSelections.Where(c => c == BetStatus.Running).Count();

            if (isBetBuilder == true)
            {
                if (lost >= 1)
                    return BetStatus.Lost;

                if (cancelled >= 1)
                    return BetStatus.Cancelled;

                if (voidd >= 1)
                    return BetStatus.Void;

                if (running >= 1)
                    return BetStatus.Running;

                if (won == betSelections.Count)
                    return BetStatus.Won;

                return BetStatus.Lost;
            }

            decimal winLoss = payout - stake;

            if (betSelections.Contains(BetStatus.Lost))
                return BetStatus.Lost;

            if (winLoss == 0 && cancelled >= 1)
                return BetStatus.Cancelled;

            if (running > 0 && lost == 0 && cancelled == 0)
                return BetStatus.Running;

            if (winLoss == 0 && voidd >= 1)
                return BetStatus.Void;

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

        private BetStatus GetInitialBetStatus(List<BetStatus> statuses, BetStatus status, bool isBetBuilder)
        {
            var betSelections = statuses;
            betSelections.Add(status);

            var lost = betSelections.Where(c => c == BetStatus.Lost).Count();
            var cancelled = betSelections.Where(c => c == BetStatus.Cancelled).Count();
            var voidd = betSelections.Where(c => c == BetStatus.Void).Count();
            var running = betSelections.Where(c => c == BetStatus.Running).Count();

            if (lost >= 1)
                return BetStatus.Lost;

            if (cancelled >= 1)
                return BetStatus.Cancelled;

            if (isBetBuilder && voidd >= 1)
                return BetStatus.Void;

            if (running > 0 && lost == 0 && cancelled == 0)
                return BetStatus.Running;

            return BetStatus.Lost;
        }

        public void ProcessBetSettlement(OnUOFBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            Stopwatch? duration = _betSettlementDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

            var betSelections = GetBetSelections(message.SelectionId);

            foreach (var betSelection in betSelections)
            {
                var finalResult = new Tuple<bool, bool>(false, false);
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
                                finalResult = SettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection, false, string.Empty);
                            else
                                finalResult = ResettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection, false, string.Empty);

                            scope.Complete();

                            if (finalResult.Item1)
                            {
                                payload.IsAudit = finalResult.Item2;

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
                            _betSettlementCounter.Add(1);
                            if (duration !=null)
                              _betSettlementDurationHistogram.Record(duration.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception ex)
                        {
                          _logger.LogError(ex.ToString());
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
        public void ProcessBetSettlementForCancellation(OnUOFBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            Stopwatch? duration = _betCancellationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

            var betSelections = GetBetSelectionsForCancellation(message.SelectionId);

            foreach (var betSelection in betSelections)
            {
                var finalResult = new Tuple<bool, bool>(false, false);
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

                            finalResult = SettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection, false, string.Empty);

                            scope.Complete();

                            if (finalResult.Item1)
                            {
                                payload.IsAudit = finalResult.Item2;

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
                            _betCancellationCounter.Add(1);
                            if (duration != null)
                              _betCancellationDurationHistogram.Record(duration.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        private decimal GetStake(List<BetSelection> selections, decimal taxedStake, decimal trueStake)
        {
            var totalSelections = selections.Count();
            var cancelledSelections = selections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Cancelled).ToList();

            if (cancelledSelections.Count == totalSelections || selections.Any(c => c.BetSelectionStatusId == (int)BetStatus.Lost))
                return trueStake;

            //apply for Won, HalfWon, HalfLost, Void
            return trueStake - taxedStake;
        }

        private Tuple<bool, bool> SettleBet(OnUOFBetSettlement message, BetSelection betSelection, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, BetSelection filteredSelection, bool isManualSettle, string reason)
        {
            var finalResult = new Tuple<bool, bool>(false, false);

            if (filteredSelection.BetSelectionStatusId != (int)BetStatus.Running)
                return new Tuple<bool, bool>(false, false);

            if (!string.IsNullOrEmpty(message.SettlementScore))
                filteredSelection.SettlementScore = message.SettlementScore;

            filteredSelection.BetSelectionStatusId = (int)message.SelectionStatus;
            filteredSelection.BetSelectionStatusName = message.SelectionStatus.ToString();
            filteredSelection.IsSettled = true;
            filteredSelection.DeadHeatFactor = message.DeadHeatFactor;

            var validStatus = new List<int> { (int)BetStatus.Won, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost, (int)BetStatus.Void };

            if (validStatus.Contains(filteredSelection.BetSelectionStatusId))
                filteredSelection.Probability = 1;
            else
                filteredSelection.Probability = 0;

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
                CorrelationId = betSelection.CorrelationId,
                DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                IsBinary = betSelectionEntity.IsBinary
            });

            var betPriceboost = context.BetPriceBoost.Where(c => c.BetId == betSelectionEntity.BetId).FirstOrDefault();
            if (betPriceboost != null)
            {
                betPriceboost.BetStatusId = betSelectionEntity.BetSelectionStatusId;
                betPriceboost.BetStatusName = betSelectionEntity.BetSelectionStatusName;
                context.BetPriceBoost.Update(betPriceboost);
            }

            var bet = context.Bet.Find(betSelection.BetId);
            if (bet != null)
            {
                var isBetRunning = bet.BetStatusId == (int)BetStatus.Running;

                if (message.SelectionStatus == BetStatus.Lost)
                {
                    finalResult = ApplyLost(context, message, payload, bet, selections, betSelection, settledDate, isPublish, isManualSettle, reason);
                }
                else if (bet.BetTypeId == (int)BetType.BetBuilder && message.SelectionStatus == BetStatus.Void)
                {
                    finalResult = ApplyAllSettled(context, message, payload, bet, selections, betSelection, settledDate, isPublish, isManualSettle, reason, false);
                }
                else
                {
                    var totalSettled = selections.Where(c => c.SelectionId != message.SelectionId && c.IsSettled == true).Count();
                    var totalSelections = selections.Where(c => c.SelectionId != message.SelectionId).Count();

                    if (totalSettled == totalSelections)
                    {
                        finalResult = ApplyAllSettled(context, message, payload, bet, selections, betSelection, settledDate, isPublish, isManualSettle, reason, false);
                    }
                }

                if (!isBetRunning && finalResult.Item1 == true)
                    payload.IsResettle = true;
            }

            context.SaveChanges();
            return finalResult;
        }

        private static void ProcessStakeFixtureLimit(OnUOFBetSettlement message, BetSelection betSelection, BettingContext context, List<BetSelection> selections, Bet betEntity)
        {
            if (selections.Count == 1 && (message.SelectionStatus == BetStatus.Cancelled || message.SelectionStatus == BetStatus.Void))
            {
                var stakeLimitFixture = context.StakeLimitFixture.Find($"{betEntity.ClientCode}-{betEntity.UserId}", betSelection.FixtureId);

                if (stakeLimitFixture != null)
                {
                    stakeLimitFixture.Remaining += betEntity.TrueStake;

                    context.StakeLimitFixture.Update(stakeLimitFixture);
                }
            }
        }

        private Tuple<bool, bool> ResettleBet(OnUOFBetSettlement message, BetSelection betSelection, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, BetSelection filteredSelection, bool isManualSettle, string reason)
        {
            var finalResult = new Tuple<bool, bool>(false, false);

            if (filteredSelection.BetSelectionStatusId == (int)message.SelectionStatus || filteredSelection.BetSelectionStatusId == (int)BetStatus.Waiting || filteredSelection.BetSelectionStatusId == (int)BetStatus.Cancelled || filteredSelection.BetSelectionStatusId == (int)BetStatus.Rejected || filteredSelection.BetSelectionStatusId == (int)BetStatus.CashedOut)
                return new Tuple<bool, bool>(false, false);

            var bet = context.Bet.Find(betSelection.BetId);
            if (bet != null)
                if (bet.IsResettled && isManualSettle == false)
                    return new Tuple<bool, bool>(false, false);

            if (bet.IsAgent)
                return new Tuple<bool, bool>(false, false);

            if (bet.BetStatusId == (int)BetStatus.CashedOut)
                return new Tuple<bool, bool>(false, false);

            filteredSelection.BetSelectionStatusId = (int)message.SelectionStatus;
            filteredSelection.BetSelectionStatusName = message.SelectionStatus.ToString();
            if (!string.IsNullOrEmpty(message.SettlementScore))
                filteredSelection.SettlementScore = message.SettlementScore;
            filteredSelection.IsSettled = true;
            filteredSelection.IsResettled = true;
            filteredSelection.DeadHeatFactor = message.DeadHeatFactor;

            var validStatus = new List<int> { (int)BetStatus.Won, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost, (int)BetStatus.Void };

            if (validStatus.Contains(filteredSelection.BetSelectionStatusId))
                filteredSelection.Probability = 1;
            else
                filteredSelection.Probability = 0;

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
                CorrelationId = betSelection.CorrelationId,
                DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                IsBinary = betSelectionEntity.IsBinary
            });

            var betPriceboost = context.BetPriceBoost.Where(c => c.BetId == betSelectionEntity.BetId).FirstOrDefault();
            if (betPriceboost != null)
            {
                betPriceboost.BetStatusId = betSelectionEntity.BetSelectionStatusId;
                betPriceboost.BetStatusName = betSelectionEntity.BetSelectionStatusName;
                context.BetPriceBoost.Update(betPriceboost);
            }

            if (bet != null)
            {
                var isBetRunning = bet.BetStatusId == (int)BetStatus.Running;
                if (message.SelectionStatus == BetStatus.Lost)
                {
                    finalResult = ApplyLost(context, message, payload, bet, selections, betSelection, settledDate, isPublish, isManualSettle, reason);
                }
                else if (bet.BetTypeId == (int)BetType.BetBuilder && message.SelectionStatus == BetStatus.Void)
                {
                    finalResult = ApplyAllSettled(context, message, payload, bet, selections, betSelection, settledDate, isPublish, isManualSettle, reason, true);
                }
                else
                {
                    var totalSettled = selections.Where(c => c.SelectionId != message.SelectionId && c.IsSettled == true).Count();
                    var totalSelections = selections.Where(c => c.SelectionId != message.SelectionId).Count();

                    var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder ? true : false;

                    var initialBetStatus = GetInitialBetStatus(selections.Where(c => c.SelectionId != betSelection.SelectionId).Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), message.SelectionStatus, isBetBuilder);

                    if (totalSettled == totalSelections)
                    {
                        finalResult = ApplyAllSettled(context, message, payload, bet, selections, betSelection, settledDate, isPublish, isManualSettle, reason, true);
                    }
                    else if ((int)initialBetStatus != bet.BetStatusId && initialBetStatus == BetStatus.Running)
                    {
                        ApplyRunning(context, bet, selections, betSelection, settledDate);
                    }
                }

                if (!isBetRunning && finalResult.Item1 == true)
                    payload.IsResettle = true;
            }

            context.SaveChanges();
            return finalResult;
        }

        private Tuple<bool, bool> ApplyLost(BettingContext context, OnUOFBetSettlement message, OnBetSettlement payload, Bet bet, List<BetSelection> selections, BetSelection betSelection, DateTime settledDate, bool isPublish, bool isManualSettle, string reason)
        {
            var isInsertToBetAudit = true;
            isPublish = string.IsNullOrEmpty(bet.BetIdRef) ? true : false; // don't publish if BetIdRef available (means bet is placed via OddsFeed integration)

            var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder ? true : false;

            var payout = GetPayout(selections.Select(s => new Tuple<BetStatus, decimal, decimal, bool>((BetStatus)s.BetSelectionStatusId, s.Odds, s.DeadHeatFactor, s.IsBinary)).ToList(), bet.TrueStake, bet.Stake, isBetBuilder, bet.Odds, (OddsType)bet.OddsTypeId);
            var betStatus = GetStatus(selections.Where(c => c.SelectionId != betSelection.SelectionId).Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), message.SelectionStatus, payout, bet.TrueStake, isBetBuilder);

            if (betStatus == (BetStatus)bet.BetStatusId)
                isInsertToBetAudit = false;
            else
            {
                bet.SettlementDate = settledDate;
                bet.SettlementDateOffset = settledDate.AddHours(-4);
            }

            payload.NonTaxedPayout = payout;

            bet.WinLoss = payout == 0 ? -bet.TrueStake : payout - bet.TrueStake;
            bet.BetStatusId = (int)betStatus;
            bet.BetStatusName = betStatus.ToString();

            bet.Payout = payout;

            var finalStake = bet.TrueStake - bet.StakeBonus;

            bet.TaxedStake = bet.IsFreeBet == false ? finalStake - (finalStake / (1 + bet.StakeTax)) : 0;
            bet.TaxedWinnings = 0;

            bet.ParlayBonus = 0;
            bet.ParlayBonusWinnings = 0;
            bet.StakeRealPayout = 0;
            bet.StakeBonusPayout = 0;

            if (isManualSettle)
            {
                bet.IsResettled = true;
                bet.Reason = reason;
            }

            var betEntity = context.Bet.Update(bet).Entity;

            if (isInsertToBetAudit)
            {
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
                    IsAgent = betEntity.IsAgent,
                    BetIdRef = betEntity.BetIdRef
                });
            }

            payload.Bet = betEntity;
            payload.BetSelections = selections;

            return new Tuple<bool, bool>(isPublish, isInsertToBetAudit);
        }
        private Tuple<bool, bool> ApplyAllSettled(BettingContext context, OnUOFBetSettlement message, OnBetSettlement payload, Bet bet, List<BetSelection> selections, BetSelection betSelection, DateTime settledDate, bool isPublish, bool isManualSettle, string reason, bool isResettle)
        {
            var isInsertToBetAudit = true;
            isPublish = string.IsNullOrEmpty(bet.BetIdRef) ? true : false; // don't publish if BetIdRef available (means bet is placed via OddsFeed integration)

            var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder ? true : false;

            var finalStake = bet.TrueStake - bet.StakeBonus;

            var taxedStake = bet.IsFreeBet == false ? finalStake - (finalStake / (1 + bet.StakeTax)) : 0;

            var stake = GetStake(selections, taxedStake, bet.TrueStake);
            var payout = GetPayout(selections.Select(s => new Tuple<BetStatus, decimal, decimal, bool>((BetStatus)s.BetSelectionStatusId, s.Odds, s.DeadHeatFactor, s.IsBinary)).ToList(), stake, bet.Stake, isBetBuilder, bet.Odds, (OddsType)bet.OddsTypeId);
            var parlayBonusWinnings = 0m;
            var parlayBonus = 0m;

            var nonTaxedStake = bet.TrueStake;
            var nonTaxedPayout = GetPayout(selections.Select(s => new Tuple<BetStatus, decimal, decimal, bool>((BetStatus)s.BetSelectionStatusId, s.Odds, s.DeadHeatFactor, s.IsBinary)).ToList(), nonTaxedStake, bet.Stake, isBetBuilder, bet.Odds, (OddsType)bet.OddsTypeId);
            var nonTaxedParlayBonusWinnings = 0m;

            var wonSelectionCount = selections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Won).Count();
            var lostSelectionCount = selections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Lost).Count();

            var hasVirtualSport = selections.Any(s => s.SportId > 1000) ? true : false;

            if (hasVirtualSport == false && wonSelectionCount >= 3 && bet.IsFreeBet == false && lostSelectionCount == 0)
            {
                var parlayConfig = GetParlayBetConfig(context, Guid.Parse(bet.ClientId), bet.CurrencyCode, bet.BetDate, selections, payout, ref parlayBonusWinnings);

                if (parlayConfig.Item1 == true)
                {
                    parlayBonus = parlayConfig.Item2.ParlayBonus;

                    nonTaxedParlayBonusWinnings = nonTaxedPayout * parlayConfig.Item2.ParlayBonus;
                }
            }

            var taxedWinnings = payout + parlayBonusWinnings;

            if (bet.IsFreeBet)
                finalStake = bet.TrueStake;

            taxedWinnings = bet.IsFreeBet == false && bet.StakeBonus == bet.TrueStake ? 0 : (taxedWinnings - finalStake) * bet.WinningsTax;

            if (payout > bet.TrueStake)
                payout = payout - taxedWinnings;
            else
                taxedWinnings = 0;

            var betStatus = GetStatus(selections.Where(c => c.SelectionId != betSelection.SelectionId).Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), message.SelectionStatus, payout, stake, isBetBuilder);

            stake = betStatus == BetStatus.Won || betStatus == BetStatus.HalfWon || betStatus == BetStatus.Void ? bet.TrueStake : stake;

            var totalPayout = payout + parlayBonusWinnings;

            nonTaxedPayout = nonTaxedPayout + nonTaxedParlayBonusWinnings;

            if (betStatus == (BetStatus)bet.BetStatusId && totalPayout == bet.Payout)
                isInsertToBetAudit = false;
            else
            {
                bet.SettlementDate = settledDate;
                bet.SettlementDateOffset = settledDate.AddHours(-4);
            }
            
            bet.WinLoss = totalPayout == 0 ? -bet.TrueStake : totalPayout - stake;
            bet.BetStatusId = (int)betStatus;
            bet.BetStatusName = betStatus.ToString();
            bet.Payout = totalPayout;
            bet.TaxedStake = betStatus == BetStatus.Cancelled ? 0 : taxedStake;
            bet.TaxedWinnings = taxedWinnings;
            bet.ParlayBonus = parlayBonus;
            bet.ParlayBonusWinnings = parlayBonusWinnings;            

            // get percentage of real and bonus payout.
            var stakeRealPayoutPercentage = bet.StakeReal / bet.TrueStake;
            var stakeBonusPayoutPercentage = bet.StakeBonus / bet.TrueStake;

            bet.StakeRealPayout = payout * stakeRealPayoutPercentage;
            bet.StakeBonusPayout = payout * stakeBonusPayoutPercentage;

            if (isManualSettle)
            {
                bet.IsResettled = true;
                bet.Reason = reason;
            }

            var betEntity = context.Bet.Update(bet).Entity;

            if (isInsertToBetAudit)
            {
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
                    IsAgent = betEntity.IsAgent,
                    BetIdRef = betEntity.BetIdRef
                });

                if (isResettle == false)
                {
                    ProcessStakeFixtureLimit(message, betSelection, context, selections, betEntity);
                }
            }

            payload.Bet = betEntity;
            payload.BetSelections = selections;
            payload.NonTaxedPayout = nonTaxedPayout;

            return new Tuple<bool, bool>(isPublish, isInsertToBetAudit);
        }

        private void ApplyRunning(BettingContext context, Bet bet, List<BetSelection> selections, BetSelection betSelection, DateTime settledDate)
        {
            var taxedStake = 0m;
            var stake = bet.TrueStake - taxedStake;

            var payout = stake * bet.Odds;

            var parlayBonusWinnings = 0m;
            var parlayBonus = 0m;

            var hasVirtualSport = selections.Any(s => s.SportId > 1000) ? true : false;
            var wonSelectionCount = selections.Where(c => c.SelectionId != betSelection.SelectionId && (c.BetSelectionStatusId == (int)BetStatus.Won || c.BetSelectionStatusId == (int)BetStatus.Running)).Count() + 1; // +1 for Running status
            var lostSelectionCount = selections.Where(c => c.SelectionId != betSelection.SelectionId && c.BetSelectionStatusId == (int)BetStatus.Lost).Count();

            if (hasVirtualSport == false && wonSelectionCount >= 3 && bet.IsFreeBet == false && lostSelectionCount == 0)
            {
                var parlayConfig = GetParlayBetConfig(context, Guid.Parse(bet.ClientId), bet.CurrencyCode, bet.BetDate, selections, payout, ref parlayBonusWinnings);

                if (parlayConfig.Item1 == true)
                    parlayBonus = parlayConfig.Item2.ParlayBonus;
            }

            bet.WinLoss = 0m;
            bet.BetStatusId = (int)BetStatus.Running;
            bet.BetStatusName = BetStatus.Running.ToString();
            bet.Payout = payout;
            bet.SettlementDate = settledDate;
            bet.SettlementDateOffset = settledDate.AddHours(-4);
            bet.TaxedStake = 0m;
            bet.TaxedWinnings = 0m;
            bet.ParlayBonus = parlayBonus;
            bet.ParlayBonusWinnings = 0m;
            bet.StakeRealPayout = 0m;
            bet.StakeBonusPayout = 0m;            

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
                IsAgent = betEntity.IsAgent,
                BetIdRef = betEntity.BetIdRef
            });
        }

        public void ProcessBetCancellation(OnUOFBetCancellation message, IMessageBroker messageBroker, string correlationId)
        {
            Stopwatch? duration = _betCancellationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

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

                ProcessBetSettlementForCancellation(cancellation, messageBroker, correlationId);
            }
            _betCancellationCounter.Add(1);
            if (duration != null)
              _betCancellationDurationHistogram.Record(duration.Elapsed.TotalMilliseconds);
        }

        public void ProcessBackOfficeBetCancellation(OnBOBetCancellaton message, IMessageBroker messageBroker, string correlationId)
        {

            Stopwatch? duration = _betBoCancellationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;

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

                            if (bet == null || bet.BetStatusId == (int)BetStatus.Waiting || bet.BetStatusId == (int)BetStatus.Cancelled || bet.BetStatusId == (int)BetStatus.Rejected || bet.BetStatusId == (int)BetStatus.CashedOut)
                            {
                                scope.Complete();
                                continue;
                            }

                            if (!string.IsNullOrEmpty(bet.BetIdRef))
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

                            var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder ? true : false;

                            var reason = string.IsNullOrEmpty(betItem.Reason) ? "Cancelled by Backoffice" : betItem.Reason;

                            var payout = GetPayout(betSelections.Select(s => new Tuple<BetStatus, decimal, decimal, bool>(BetStatus.Cancelled, s.Odds, s.DeadHeatFactor, s.IsBinary)).ToList(), bet.TrueStake, bet.Stake, isBetBuilder, bet.Odds, (OddsType)bet.OddsTypeId);

                            bet.Reason = $"103 - {reason}";
                            bet.WinLoss = payout == 0 ? -bet.Stake : payout - bet.Stake;
                            bet.BetStatusId = (int)BetStatus.Cancelled;
                            bet.BetStatusName = BetStatus.Cancelled.ToString();
                            bet.Payout = payout;
                            bet.SettlementDate = settledDate;
                            bet.SettlementDateOffset = settledDate.AddHours(-4);
                            bet.TaxedStake = 0;
                            bet.TaxedWinnings = 0;
                            bet.ParlayBonus = 0;
                            bet.ParlayBonusWinnings = 0;
                            bet.StakeRealPayout = 0;
                            bet.StakeBonusPayout = 0;

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
                                IsAgent = betEntity.IsAgent,
                                BetIdRef = betEntity.BetIdRef
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
                                    IsBinary = betSelectionEntity.IsBinary
                                    
                                });
                            }

                            var betPriceboost = context.BetPriceBoost.Where(c => c.BetId == bet.BetId).FirstOrDefault();
                            if (betPriceboost != null)
                            {
                                betPriceboost.BetStatusId = betEntity.BetStatusId;
                                betPriceboost.BetStatusName = betEntity.BetStatusName;

                                context.BetPriceBoost.Update(betPriceboost);
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

                            _betBoCancellationCounter.Add(1);
                            if (duration != null)
                              _betBoCancellationDurationHistogram.Record(duration.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }
        public void ProcessBackOfficeManualSettlement(OnBOBetManualSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            Stopwatch? duration = _betBoManualSettlementDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
            foreach (var betItem in message.Bets)
            {
                var finalResult = new Tuple<bool, bool>(false, false);
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
                            var selections = context.BetSelection.AsNoTracking().Where(c => c.BetId == betItem.BetId).ToList();

                            if (selections.Count != 1) //do not process if parlay or no selection at all
                            {
                                scope.Complete();
                                continue;
                            }

                            var filteredSelection = selections.FirstOrDefault();

                            if (filteredSelection == null)
                            {
                                scope.Complete();
                                continue;
                            }

                            OnUOFBetSettlement request = new OnUOFBetSettlement()
                            {
                                SelectionId = filteredSelection.SelectionId,
                                DeadHeatFactor = 1,
                                SelectionStatus = betItem.NewStatus,
                            };

                            betItem.Reason = $"1000 - {betItem.Reason}";

                            if (filteredSelection.IsSettled == false)
                                finalResult = SettleBet(request, filteredSelection, isPublish, payload, settledDate, context, selections, filteredSelection, true, betItem.Reason);
                            else
                                finalResult = ResettleBet(request, filteredSelection, isPublish, payload, settledDate, context, selections, filteredSelection, true, betItem.Reason);

                            scope.Complete();

                            if (finalResult.Item1)
                            {
                                payload.IsAudit = finalResult.Item2;

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

                            _betBoManualSettlementCounter.Add(1);
                            if (duration != null)
                              _betBoManualSettlementDurationHistogram.Record(duration.Elapsed.TotalMilliseconds);
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
