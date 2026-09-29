using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Helpers;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Providers;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.Entities.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Transactions;

namespace Platform.CashOut.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly ILogger<BettingRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        // Cashouts whose GTS assessment answered "AssessmentDelayed": parked here until the
        // verdict arrives via Ably -> Provider.Service -> OnBetCashOutDelayedVerdict, or until
        // the TTL expires (then declined so the client UI is released). In-memory on purpose:
        // the window is ~betDelay (seconds); a restart loses the entry and the player retries.
        private sealed class PendingCashout
        {
            public OnCashoutBetSettlement Message { get; init; }
            public IMessageBroker MessageBroker { get; init; }
            public string CorrelationId { get; init; }
            public DateTime ParkedAtUtc { get; init; }
        }

        private static readonly ConcurrentDictionary<Guid, PendingCashout> _pendingCashouts = new ConcurrentDictionary<Guid, PendingCashout>();
        private static readonly TimeSpan _pendingCashoutTtl = TimeSpan.FromSeconds(30);
        private readonly Timer _pendingCashoutSweeper;

        public BettingRepository(IConfiguration configuration, ILogger<BettingRepository> logger, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;

            // Singleton service, so one sweeper for the process lifetime.
            _pendingCashoutSweeper = new Timer(_ => SweepExpiredPendingCashouts(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        private void ProcessCashOutSettlement(OnCashoutBetSettlement message, IMessageBroker messageBroker, string correlationId)
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

                            var isPublish = false;
                            Tuple<CashoutAcceptance, string> result;
                            var partialCashout = context.PartialCashoutAudit.AsNoTracking().Where(c => c.BetId == bet.BetId).ToList();

                            var selections = context.BetSelection.Where(c => c.BetId == message.BetId).ToList();
                            if (selections.Count == 0)
                            {
                                scope.Complete();
                                return;
                            }

                            // Pre-acceptance provider gate: the provider is resolved from the
                            // message's ChannelTypeId (BG -> GTS assessment, SR -> MTS ticket
                            // cashout), same resolver pattern as Platform.Placement.Servicev2.
                            // Providers self-gate on Custom:IsProviderEnabled.
                            var isFullCashout = !partialCashout.Any() && message.CashoutType == CashoutType.Full;

                            decimal cashoutPercent = 0m;
                            decimal prevCashoutAmount = 0m;

                            if (!isFullCashout)
                            {
                                prevCashoutAmount = partialCashout.Any() ? partialCashout.Sum(s => s.CashoutAmount) : 0;
                                var originalStake = partialCashout.Any() ? partialCashout.FirstOrDefault(c => c.BetId == bet.BetId).OriginalStake : bet.Stake;
                                var remainingStake = bet.Stake - (bet.Stake * message.CashOutStakePercent);
                                cashoutPercent = (originalStake - remainingStake) / originalStake;
                            }

                            var providerResult = ValidateCashoutWithProvider(message, bet, selections, isFullCashout, cashoutPercent, prevCashoutAmount);

                            if (providerResult.Item1 == BetAcceptance.Pending)
                            {
                                // GTS AssessmentDelayed: the real verdict arrives via Ably in
                                // ~betDelay seconds. Park the cashout (no writes were made; the
                                // transaction rolls back) and resume in ProcessCashOutDelayedVerdict.
                                ParkPendingCashout(message, messageBroker, correlationId);
                                return;
                            }

                            result = new Tuple<CashoutAcceptance, string>(
                                providerResult.Item1 == BetAcceptance.Accepted ? CashoutAcceptance.Accepted : CashoutAcceptance.Rejected,
                                providerResult.Item2);

                            if (result.Item1 == CashoutAcceptance.Accepted)
                            {
                                // Double-cashout guard: two concurrent messages for the same bet can
                                // both pass the initial Running check (a plain ReadCommitted read
                                // releases its shared lock immediately) and the GTS gate is read-only,
                                // so it accepts both. Re-read the row under UPDLOCK — held to the end
                                // of the transaction, it serializes concurrent settlers; the loser
                                // blocks here until the winner commits.
                                //
                                // GM-865: a full cashout (or settlement) flips BetStatusId away from
                                // Running, so checking status alone catches that race. A partial
                                // cashout does not - the bet stays Running - so a second concurrent
                                // partial that read `bet` above before this lock would otherwise pass
                                // this check and then compute its share from stale, pre-first-cashout
                                // stake values in SettleBet below, corrupting Bet.Stake/TrueStake on
                                // SaveChanges. Copying the locked row's current stake fields onto the
                                // already-tracked `bet` (rather than a second FromSqlRaw().AsTracking()
                                // for the same key, which EF rejects as a duplicate tracked instance)
                                // makes SettleBet operate on whatever the lock's winner already
                                // committed.
                                var lockedBet = context.Bet
                                    .FromSqlRaw("SELECT * FROM [Bet] WITH (UPDLOCK, ROWLOCK) WHERE [BetId] = {0}", message.BetId)
                                    .AsNoTracking()
                                    .FirstOrDefault();

                                if (lockedBet == null || lockedBet.BetStatusId != (int)BetStatus.Running)
                                {
                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = message.ClientId.ToString().ToUpper(),
                                        UserId = message.UserId,
                                        BetId = message.BetId.ToString().ToUpper(),
                                        BetStatus = lockedBet != null ? (BetStatus)lockedBet.BetStatusId : (BetStatus)bet.BetStatusId,
                                        Message = "Cashout request has been declined"
                                    }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                    _logger.LogWarning("{@betId} - Cashout declined: bet is no longer Running (concurrent cashout/settlement)", message.BetId);
                                    return;
                                }

                                bet.Stake = lockedBet.Stake;
                                bet.TrueStake = lockedBet.TrueStake;
                                bet.StakeReal = lockedBet.StakeReal;
                                bet.StakeBonus = lockedBet.StakeBonus;
                                bet.TaxedStake = lockedBet.TaxedStake;
                                bet.TaxedWinnings = lockedBet.TaxedWinnings;
                                bet.IsPartialCashout = lockedBet.IsPartialCashout;

                                var payload = new OnBetSettlement();
                                payload.ChannelTypeId = message.ChannelTypeId;

                                // Cumulative cashout figures for the provider update: `partialCashout`
                                // holds the prior audit rows and `bet` still carries pre-event values
                                // (SettleBet reduces them below). A full cashout consumes the whole
                                // remaining TrueStake; a partial consumes only its percentage of it.
                                payload.OriginalTrueStake = partialCashout.Any()
                                    ? partialCashout.First().TrueStake
                                    : bet.TrueStake;
                                payload.CashedOutTotalAmount = partialCashout.Sum(p => p.CashoutAmount) + message.CashOutStake;
                                payload.CashedOutTotalTrueStake = partialCashout.Sum(p => p.TrueStakeUsed)
                                    + (message.CashoutType == CashoutType.Partial
                                        ? bet.TrueStake * message.CashOutStakePercent
                                        : bet.TrueStake);

                                OnBetSettlementPartial partialPayload = null;
                                if (message.CashoutType == CashoutType.Partial)
                                {
                                    partialPayload = new OnBetSettlementPartial()
                                    {
                                        CashoutStake = message.CashOutStake,
                                        CashoutStakePercent = message.CashOutStakePercent,
                                        Stake = bet.Stake * message.CashOutStakePercent,
                                        TrueStake = bet.TrueStake * message.CashOutStakePercent,
                                        TaxedStake = message.TaxedStake,
                                        NonTaxedPayout = message.NonTaxedPayout,
                                        OriginalTrueStake = payload.OriginalTrueStake,
                                        CashedOutTotalTrueStake = payload.CashedOutTotalTrueStake,
                                        CashedOutTotalAmount = payload.CashedOutTotalAmount,
                                    };
                                }

                                payload.NonTaxedPayout = message.NonTaxedPayout;

                                var filteredSelections = selections.Where(c => c.BetId == message.BetId).ToList();

                                if (filteredSelections.Count == 0)
                                {
                                    scope.Complete();
                                    return;
                                }

                                if (message.CashoutType == CashoutType.Full)
                                    bet.Reason = result.Item2;

                                isPublish = SettleBet(message, bet, isPublish, payload, settledDate, context, selections, filteredSelections, correlationId);

                                context.SaveChanges();

                                scope.Complete();

                                if (isPublish)
                                {
                                    if (message.CashoutType == CashoutType.Full)
                                        messageBroker.Publish(payload, "OnBetFullCashOut", "r.bet.operator", correlationId, 10);
                                    else
                                    {
                                        partialPayload.Bet = payload.Bet;
                                        partialPayload.BetSelections = payload.BetSelections;
                                        partialPayload.IsAudit = payload.IsAudit;
                                        partialPayload.Message = payload.Message;
                                        partialPayload.IsRetry = payload.IsRetry;
                                        partialPayload.ChannelTypeId = payload.ChannelTypeId;
                                        messageBroker.Publish(partialPayload, "OnBetPartialCashOut", "r.bet.operator", correlationId, 10);
                                    }

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = message.ClientId.ToString().ToUpper(),
                                        UserId = message.UserId,
                                        BetId = message.BetId.ToString().ToUpper(),
                                        BetStatus = (BetStatus)payload.Bet.BetStatusId,
                                        Message = "Cashout request has been accepted"
                                    }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);
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
                                    Message = "Cashout request has been declined"
                                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                                _logger.LogWarning("{@betId} - Cashout request has been declined - MTS Result : {@payload} - {@message}", message.BetId, result, JsonSerializer.Serialize(message));
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
                    Message = "Cashout request has been declined"
                }, "OnBetStatus", $"r.bet.betstatus.{message.ClientCode.ToLower()}", correlationId, 10);

                _logger.LogError(ex, "Payload : {@payload}", message);
            }
        }

        private bool SettleBet(OnCashoutBetSettlement message, Bet bet, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, List<BetSelection> filteredSelections, string correlationId)
        {

            foreach (var selection in filteredSelections)
            {
                var tempSelection = message.Probabilities.Where(c => c.BetSelectionId == selection.BetSelectionId).FirstOrDefault();

                if (message.CashoutType == CashoutType.Full && selection.BetSelectionStatusId == (int)BetStatus.Running)
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

            var payout = message.CashOutStake;

            if (message.CashoutType == CashoutType.Partial)
            {
                var prevCashout = context.PartialCashoutAudit.AsNoTracking().FirstOrDefault(p => p.BetId == message.BetId);
                var originalStake = prevCashout != null ? prevCashout.OriginalStake : bet.Stake;
                var originalTrueStake = prevCashout != null ? prevCashout.TrueStake : bet.TrueStake;
                var stakeUsed = bet.Stake * message.CashOutStakePercent;
                var trueStakeUsed = bet.TrueStake * message.CashOutStakePercent;
                var stakeRealUsed = bet.StakeReal * message.CashOutStakePercent;
                var stakeBonusUsed = bet.StakeBonus * message.CashOutStakePercent;

                context.PartialCashoutAudit.Add(new PartialCashoutAudit
                {
                    Id = Guid.Parse(correlationId),
                    BetId = message.BetId,
                    ClientId = message.ClientId,
                    ClientCode = message.ClientCode,
                    OriginalStake = originalStake,
                    StakeUsed = stakeUsed,
                    PreStake = bet.Stake,
                    PostStake = bet.Stake - stakeUsed,
                    TrueStake = originalTrueStake,
                    TrueStakeUsed = trueStakeUsed,
                    PreTrueStake = bet.TrueStake,
                    PostTrueStake = bet.TrueStake - trueStakeUsed,
                    CashoutAmount = message.CashOutStake,
                    CashoutPercent = message.CashOutStakePercent,
                    CashoutDate = settledDate,
                    CashoutDateOffset = settledDate.AddHours(-4),
                    NonTaxedPayout = message.NonTaxedPayout,
                    TaxedStake = message.TaxedStake,
                    TaxedWinnings = message.TaxedWinnings
                });

                bet.Stake = bet.Stake - stakeUsed;
                bet.TrueStake = bet.TrueStake - trueStakeUsed;
                bet.StakeReal = bet.StakeReal - stakeRealUsed;
                bet.StakeBonus = bet.StakeBonus - stakeBonusUsed;
                bet.TaxedStake = bet.TaxedStake - message.TaxedStake;
                bet.TaxedWinnings = bet.TaxedWinnings - message.TaxedWinnings;
                bet.IsPartialCashout = true;

                var finalStake = bet.Stake;
                payout = finalStake * bet.Odds;

                // Malay and Indo
                if (filteredSelections.Count == 1)
                {
                    var selection = filteredSelections.FirstOrDefault();

                    // malay code here
                    if ((int)bet.OddsTypeId == (int)OddsType.Malaysian && OddsTypeHelper.GetOddsByOddsType((int)bet.OddsTypeId, selection.Odds, selection.IsBinary) < 0 && selection.IsBinary == true)
                    {
                        var currentStake = finalStake;
                        finalStake = Math.Abs(finalStake * Math.Round(OddsTypeHelper.GetOddsByOddsType((int)bet.OddsTypeId, selection.Odds, selection.IsBinary), 2));
                        payout = currentStake + finalStake;
                    }

                    // indo code here
                    if ((int)bet.OddsTypeId == (int)OddsType.Indonesian && OddsTypeHelper.GetOddsByOddsType((int)bet.OddsTypeId, selection.Odds, selection.IsBinary) < 0 && selection.IsBinary == true)
                    {
                        var currentStake = finalStake;
                        finalStake = Math.Abs(finalStake * Math.Round(OddsTypeHelper.GetOddsByOddsType((int)bet.OddsTypeId, selection.Odds, selection.IsBinary), 2));
                        payout = currentStake + finalStake;
                    }
                }
            }

            var stake = bet.TrueStake - bet.TaxedStake;
            var winloss = message.CashoutType == CashoutType.Partial ? 0m : payout - bet.TrueStake;

            var betStatus = message.CashoutType == CashoutType.Full ? GetStatus(selections.Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), payout, stake) : BetStatus.Running;

            bet.WinLoss = winloss;
            bet.BetStatusId = (int)betStatus;
            bet.BetStatusName = betStatus.ToString();

            bet.Payout = payout;
            bet.SettlementDate = settledDate;
            bet.SettlementDateOffset = settledDate.AddHours(-4);

            if (message.CashoutType == CashoutType.Full)
            {
                bet.TaxedStake = message.TaxedStake;
                bet.TaxedWinnings = message.TaxedWinnings;

            }

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

            if (payout <= 0) //use payout instead of winloss for V3
                return BetStatus.Lost;

            if (payout > 0) //use payout instead of winloss for V3
                return BetStatus.Won;

            return result;
        }


        // Resolves the bet's provider (BG -> GTSBetProvider, SR -> MtsBetProvider) and runs
        // its pre-acceptance cash-out gate. A decline stops the cashout before any money
        // moves, feeding the existing declined path (OnBetStatus -> r.bet.betstatus.{clientcode});
        // Pending means a GTS delayed assessment whose verdict arrives via Ably.
        // Fail-fast: resolver/provider errors reject the cashout; the player can retry.
        private Tuple<BetAcceptance, string> ValidateCashoutWithProvider(OnCashoutBetSettlement message, Bet bet, List<BetSelection> selections, bool isFullCashout, decimal cashoutPercent, decimal prevCashoutAmount)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var betProviderFactory = scope.ServiceProvider.GetRequiredService<IBetProviderFactory>();

                var provider = betProviderFactory.Resolve(message.ChannelTypeId);

                var providerResult = provider.ValidateCashoutAsync(message, bet, selections, isFullCashout, cashoutPercent, prevCashoutAmount).GetAwaiter().GetResult();

                _logger.LogInformation("ValidateCashoutWithProvider: {betId} channel {channelTypeId} -> {acceptance} ({reason})", message.BetId, message.ChannelTypeId, providerResult.Acceptance, providerResult.Reason);

                return new Tuple<BetAcceptance, string>(providerResult.Acceptance, providerResult.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateCashoutWithProvider Error for bet {betId}", message.BetId);
                return new Tuple<BetAcceptance, string>(BetAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }

        private void ParkPendingCashout(OnCashoutBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            _pendingCashouts[message.BetId] = new PendingCashout
            {
                Message = message,
                MessageBroker = messageBroker,
                CorrelationId = correlationId,
                ParkedAtUtc = DateTime.UtcNow
            };

            _logger.LogInformation("{@betId} - Cashout parked awaiting GTS delayed verdict", message.BetId);
        }

        public void ProcessCashOutDelayedVerdict(OnBetCashOutDelayedVerdict verdict, IMessageBroker messageBroker, string correlationId)
        {
            if (!_pendingCashouts.TryRemove(verdict.BetId, out var pending))
            {
                // Expired, lost to a restart, or a duplicate verdict — nothing to resume.
                _logger.LogWarning("{@betId} - Delayed cashout verdict had no parked cashout (expired/restarted/duplicate) - ignored", verdict.BetId);
                return;
            }

            pending.Message.DelayedBetAcceptance = verdict.Accepted ? BetAcceptance.Accepted : BetAcceptance.Rejected;
            pending.Message.DelayedMessage = verdict.Reason;

            // Re-enter settlement directly: the live re-quote already ran on the original pass,
            // and the provider gate will honor DelayedBetAcceptance instead of re-assessing.
            ProcessCashOutSettlement(pending.Message, messageBroker, correlationId);
        }

        private void SweepExpiredPendingCashouts()
        {
            foreach (var entry in _pendingCashouts)
            {
                if (DateTime.UtcNow - entry.Value.ParkedAtUtc < _pendingCashoutTtl)
                    continue;

                if (!_pendingCashouts.TryRemove(entry.Key, out var expired))
                    continue;

                try
                {
                    expired.MessageBroker.Publish(new OnBetStatus
                    {
                        ClientId = expired.Message.ClientId.ToString().ToUpper(),
                        UserId = expired.Message.UserId,
                        BetId = expired.Message.BetId.ToString().ToUpper(),
                        BetStatus = BetStatus.Running,
                        Message = "Cashout request has been declined"
                    }, "OnBetStatus", $"r.bet.betstatus.{expired.Message.ClientCode.ToLower()}", expired.CorrelationId, 10);

                    _logger.LogWarning("{@betId} - Cashout declined: GTS delayed verdict not received within {ttlSeconds}s", expired.Message.BetId, _pendingCashoutTtl.TotalSeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{@betId} - Failed publishing expiry decline for parked cashout", expired.Message.BetId);
                }
            }
        }

        public void ProcessCashOut(OnCashoutBetSettlement message, IMessageBroker messageBroker, string correlationId)
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

                    var initial = httpClient.PostAsync("v1/bet/cashout/state", new StringContent(JsonSerializer.Serialize(new CashoutStateRequestDTO { BetId = message.BetId, UserId = message.UserId, ClientId = message.ClientId }), System.Text.Encoding.UTF8, "application/json")).Result;

                    if (initial.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<BaseResponseDTO<CashoutStateResponseDTO>>(initial.Content.ReadAsStringAsync().Result, _jsonOptions);

                        // For partial cashouts the fresh quote values are for the WHOLE bet and must
                        // be scaled by the requested percent, mirroring Betting.API's original
                        // publish (quote value x CashOutStakePercent) — otherwise a partial live
                        // cashout is settled with the full-bet amounts.
                        var isPartial = message.CashoutType == CashoutType.Partial;
                        var percent = isPartial ? message.CashOutStakePercent : 1m;

                        var payload = new OnCashoutBetSettlement
                        {
                            BetId = result.Result.BetId,
                            ClientId = message.ClientId,
                            ClientCode = message.ClientCode,
                            UserId = message.UserId,
                            ChannelTypeId = message.ChannelTypeId,
                            CashoutType = message.CashoutType,
                            CashOutStake = result.Result.CashOutStake * percent,
                            CashOutStakePercent = message.CashOutStakePercent,
                            TaxedStake = result.Result.TaxedStake * percent,
                            TaxedWinnings = result.Result.TaxedWinnings * percent,
                            NonTaxedPayout = result.Result.NonTaxedPayout * percent,
                            HasLive = message.HasLive,
                            Probabilities = result.Result.Probabilities,
                            Endpoint = message.Endpoint,
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

                                _logger.LogWarning("{@betId} - Live Cashout declined", message.BetId);
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

                                    _logger.LogWarning("{@betId} - Live Cashout declined", message.BetId);
                                    isSuccess = false;
                                    break;
                                }
                            }
                        }

                        if (isSuccess == true)
                        {
                            ProcessCashOutSettlement(payload, messageBroker, correlationId);
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

                        _logger.LogWarning("{@betId} - Live Cashout declined", message.BetId);
                    }
                }
                else
                {
                    ProcessCashOutSettlement(message, messageBroker, correlationId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payload : {@payload}", message);
            }
        }
    }
}
