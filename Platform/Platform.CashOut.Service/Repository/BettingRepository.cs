using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
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
using System.Text.Json;
using System.Transactions;

namespace Platform.CashOut.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private IBuilderFactory _factory;
        private readonly ILogger<BettingRepository> _logger;
        private readonly IConfiguration _configuration;
        public BettingRepository(IConfiguration configuration, ILogger<BettingRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void ProcesssCashoutBetSettlement(OnCashoutBetSettlement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {

            #region Old Code

            //if (message.Bet == null)
            //    return;

            //var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ?
            //    message.BetSelections.Any(c => c.SportId > 1000) ? new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Accepted, "1024 - Cashout Accepted (MTS disabled for VS)") :
            //    SendCashoutToMTS(message, mts) : new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Accepted, "1024 - Cashout Accepted (MTS disabled)");

            //if (result.Item1 == CashoutAcceptance.Accepted)
            //{

            //    foreach (var betSelection in message.BetSelections)
            //    {
            //        var isPublish = false;
            //        var payload = new OnBetSettlement();
            //        var settledDate = DateTime.UtcNow;

            //        using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            //        {
            //            var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

            //            using (var context = new BettingContext(optionsBuilder.Options))
            //            {
            //                try
            //                {
            //                    var selections = context.BetSelection.Where(c => c.BetId == message.Bet.BetId).ToList();

            //                    if (selections.Count == 0)
            //                    {
            //                        scope.Complete();
            //                        continue;
            //                    }

            //                    var filteredSelection = selections.Where(c => c.SelectionId == betSelection.SelectionId).FirstOrDefault();

            //                    if (filteredSelection == null)
            //                    {
            //                        scope.Complete();
            //                        continue;
            //                    }

            //                    isPublish = SettleBet(message, betSelection, isPublish, payload, settledDate, context, selections, filteredSelection);

            //                    if(isPublish)
            //                    {
            //                        var bet = context.Bet.Find(filteredSelection.BetId);

            //                        var preStake = bet.Stake - context.CashoutHistory.Where(c => c.BetId == message.Bet.BetId).Sum(s => s.StakeUsed);
            //                        var postStake = preStake - message.StakeUsed;
            //                        context.CashoutHistory.Add(new CashoutHistory
            //                        {
            //                            BetId = bet.BetId,
            //                            CashedOutAmount = message.Cashout,
            //                            CashoutDate = settledDate,
            //                            CashoutDateOffset = settledDate.AddHours(-4),
            //                            CashoutTypeId = (int)CashoutType.Full,
            //                            CashoutTypeName = CashoutType.Full.ToString(),
            //                            ClientId = bet.ClientId,
            //                            ClientCode = bet.ClientCode,
            //                            OriginalStake = bet.Stake,
            //                            StakeUsed = message.StakeUsed,
            //                            PreStake = preStake,
            //                            PostStake = postStake,

            //                        });
            //                        context.SaveChanges();
            //                    }

            //                    scope.Complete();

            //                    if (isPublish)
            //                    {
            //                        messageBroker.Publish(payload, "OnBetSettlement", "r.bet.operator.test", correlationId);

            //                        //TODO: Uncomment this ticket
            //                        //messageBroker.Publish(new OnBetStatus
            //                        //{
            //                        //    ClientId = payload.Bet.ClientId.ToString().ToUpper(),
            //                        //    UserId = payload.Bet.UserId,
            //                        //    BetId = payload.Bet.BetId.ToString().ToUpper(),
            //                        //    BetStatus = (BetStatus)payload.Bet.BetStatusId
            //                        //}, "OnBetStatus", "r.bet.betstatus", correlationId);
            //                    }
            //                }
            //                catch (Exception ex)
            //                {
            //                    _logger.LogError($"ProcesssCashoutBetSettlement {JsonSerializer.Serialize(message)} {ex.Message}");
            //                }
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //    var payload = new OnBetPlacement();

            //    try
            //    {

            //        using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            //        {
            //            var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

            //            using (var context = new BettingContext(optionsBuilder.Options))
            //            {
            //                var bet = context.Bet.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

            //                if (bet != null)
            //                {
            //                    bet.Reason = result.Item2;

            //                    var betEntity = context.Bet.Update(bet).Entity;
            //                    payload.Bet = betEntity;

            //                    context.BetAudit.Add(new BetAudit
            //                    {
            //                        BetId = betEntity.BetId,
            //                        UserId = betEntity.UserId,
            //                        Stake = betEntity.Stake,
            //                        TrueStake = betEntity.TrueStake,
            //                        Odds = betEntity.Odds,
            //                        OddsTypeId = betEntity.OddsTypeId,
            //                        OddsTypeName = betEntity.OddsTypeName,
            //                        Payout = betEntity.Payout,
            //                        WinLoss = betEntity.WinLoss,
            //                        BetStatusId = betEntity.BetStatusId,
            //                        BetStatusName = betEntity.BetStatusName,
            //                        Reason = betEntity.Reason,
            //                        CurrencyCode = betEntity.CurrencyCode,
            //                        LanguageCode = betEntity.LanguageCode,
            //                        ExchangeRate = betEntity.ExchangeRate,
            //                        IPAddress = betEntity.IPAddress,
            //                        Channel = betEntity.Channel,
            //                        ClientId = betEntity.ClientId,
            //                        ClientIdRef = betEntity.ClientIdRef,
            //                        ClientCode = betEntity.ClientCode,
            //                        ClientName = betEntity.ClientName,
            //                        LimitId = betEntity.LimitId,
            //                        Endpoint = betEntity.Endpoint,
            //                        OperatorToken = betEntity.OperatorToken,
            //                        CompanyShare = betEntity.CompanyShare,
            //                        IsResettled = betEntity.IsResettled,
            //                        IsCashout = betEntity.IsCashout,
            //                        BetDate = betEntity.BetDate,
            //                        BetDateOffset = betEntity.BetDateOffset,
            //                        SettlementDate = betEntity.SettlementDate,
            //                        SettlementDateOffset = betEntity.SettlementDateOffset
            //                    });

            //                    var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

            //                    foreach (var betSelection in betSelections)
            //                    {
            //                        betSelection.BetSelectionStatusId = bet.BetStatusId;
            //                        betSelection.BetSelectionStatusName = bet.BetStatusName;
            //                        betSelection.IsSettled = true;

            //                        var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
            //                        payload.BetSelections.Add(betSelectionEntity);

            //                        context.BetSelectionAudit.Add(new BetSelectionAudit
            //                        {
            //                            BetId = betSelectionEntity.BetId,
            //                            BetSelectionId = betSelectionEntity.BetSelectionId,
            //                            UserId = betSelectionEntity.UserId,
            //                            SportId = betSelectionEntity.SportId,
            //                            SportName = betSelectionEntity.SportName,
            //                            SegmentId = betSelectionEntity.SegmentId,
            //                            SegmentName = betSelectionEntity.SegmentName,
            //                            LeagueId = betSelectionEntity.LeagueId,
            //                            LeagueName = betSelectionEntity.LeagueName,
            //                            FixtureId = betSelectionEntity.FixtureId,
            //                            FixtureName = betSelectionEntity.FixtureName,
            //                            FixtureTypeId = betSelectionEntity.FixtureTypeId,
            //                            FixtureTypeName = betSelectionEntity.FixtureTypeName,
            //                            MarketTemplateId = betSelectionEntity.MarketTemplateId,
            //                            MarketId = betSelectionEntity.MarketId,
            //                            MarketName = betSelectionEntity.MarketName,
            //                            SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
            //                            SelectionId = betSelectionEntity.SelectionId,
            //                            SelectionName = betSelectionEntity.SelectionName,
            //                            FixtureStatusId = betSelectionEntity.FixtureStatusId,
            //                            FixtureStatusName = betSelectionEntity.FixtureStatusName,
            //                            Line = betSelectionEntity.Line,
            //                            Odds = betSelectionEntity.Odds,
            //                            BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
            //                            BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
            //                            IsSettled = betSelectionEntity.IsSettled,
            //                            IsResettled = betSelectionEntity.IsResettled,
            //                            FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
            //                            SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
            //                            FixtureStartDate = betSelectionEntity.FixtureStartDate,
            //                            BetScore = betSelectionEntity.BetScore,
            //                            SettlementScore = betSelectionEntity.SettlementScore,
            //                            CorrelationId = betSelectionEntity.CorrelationId
            //                        });
            //                    }

            //                    ProcessStakeFixtureLimit(payload, context);

            //                    context.SaveChanges();
            //                }
            //                scope.Complete();
            //                messageBroker.Publish(payload, "OnBetTimeout", "r.bet.cancellation.test", correlationId);

            //                //publish to reports
            //                messageBroker.Publish(payload, "OnBetRejection", "r.bet.reports.test", correlationId);

            //                //TODO: Uncomment this ticket
            //                //messageBroker.Publish(new OnBetStatus
            //                //{
            //                //    ClientId = payload.Bet.ClientId.ToString().ToUpper(),
            //                //    UserId = payload.Bet.UserId,
            //                //    BetId = payload.Bet.BetId.ToString().ToUpper(),
            //                //    BetStatus = (BetStatus)payload.Bet.BetStatusId
            //                //}, "OnBetStatus", "r.bet.betstatus", correlationId);
            //            }
            //        }

            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError($"{DateTime.UtcNow} - END - Error Rejected - {message.Bet.BetId.ToString().ToUpper()} - {ex.Message}");

            //        ////TODO: Uncomment this ticket
            //        //messageBroker.Publish(new
            //        //{
            //        //    BetId = $"{message.Bet.BetId.ToString().ToUpper()} - Rejected Exception",
            //        //    Error = ex.Message,
            //        //    StackTrace = ex.StackTrace,
            //        //    InnerException = ex.InnerException.ToString()
            //        //}, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

            //        RejectBetOnError(message, messageBroker, correlationId, payload, new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"10002 - {ex.Message}"), 2);
            //    }
            //}

            #endregion

            #region New Code

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        var settledDate = DateTime.UtcNow;
                        var payload = new OnBetSettlement();
                        var bet = context.Bet.Where(c => c.BetId == message.BetId).FirstOrDefault();

                        if (bet != null)
                        {
                            if (bet.BetStatusId != (int)BetStatus.Running)
                                return;

                            var isPublish = false;
                            var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ?
                                SendCashoutToMTS(message, mts) : new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Accepted, "1024 - Cashout Accepted (MTS disabled)");

                            if (result.Item1 == CashoutAcceptance.Accepted)
                            {
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

                                var totalStakeUsed = context.CashOutHistory.Where(c => c.BetId == message.BetId).Sum(s => s.StakeUsed);
                                var preStake = bet.Stake - totalStakeUsed;
                                var postStake = preStake - message.StakeUsed;

                                if ((totalStakeUsed + message.StakeUsed) >= bet.Stake)
                                {
                                    bet.Reason = $"{result.Item2} (MTS)";

                                    isPublish = SettleBet(message, bet, isPublish, payload, settledDate, context, selections, filteredSelections);
                                }

                                context.CashOutHistory.Add(new CashOutHistory
                                {
                                    BetId = bet.BetId,
                                    CashedOutAmount = message.Cashout,
                                    CashoutDate = settledDate,
                                    CashoutDateOffset = settledDate.AddHours(-4),
                                    CashoutTypeId = (int)CashoutType.Full,
                                    CashoutTypeName = CashoutType.Full.ToString(),
                                    ClientId = bet.ClientId,
                                    ClientCode = bet.ClientCode,
                                    OriginalStake = bet.Stake,
                                    StakeUsed = message.StakeUsed,
                                    PreStake = preStake,
                                    PostStake = postStake,
                                });
                                context.SaveChanges();


                                scope.Complete();

                                if (isPublish)
                                {
                                    messageBroker.Publish(payload, "OnBetFullCashOut", "r.bet.operator.test", correlationId);

                                    messageBroker.Publish(new OnBetStatus
                                    {
                                        ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                                        UserId = payload.Bet.UserId,
                                        BetId = payload.Bet.BetId.ToString().ToUpper(),
                                        BetStatus = (BetStatus)payload.Bet.BetStatusId
                                    }, "OnBetStatus", "r.bet.betstatus", correlationId);
                                }
                            }
                            else
                            {

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
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"{DateTime.UtcNow} - END - Error Rejected - {message.BetId.ToString().ToUpper()} - {ex.Message}");

                ////TODO: Uncomment this ticket
                //messageBroker.Publish(new
                //{
                //    BetId = $"{message.Bet.BetId.ToString().ToUpper()} - Rejected Exception",
                //    Error = ex.Message,
                //    StackTrace = ex.StackTrace,
                //    InnerException = ex.InnerException.ToString()
                //}, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

            }

            #endregion 
        }


        private bool SettleBet(OnCashoutBetSettlement message, Bet bet, bool isPublish, OnBetSettlement payload, DateTime settledDate, BettingContext context, List<BetSelection> selections, List<BetSelection> filteredSelections)
        {

            foreach (var selection in filteredSelections)
            {

                selection.IsSettled = true;
                selection.BetSelectionStatusId = (int)BetStatus.CashedOut;
                selection.BetSelectionStatusName = BetStatus.CashedOut.ToString();

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
                    CorrelationId = betSelectionEntity.CorrelationId,
                    Probability = betSelectionEntity.Probability,
                    CashoutStatusId = betSelectionEntity.CashoutStatusId
                });
            }

            var payout = message.Cashout;

            var betStatus = GetStatus(selections.Select(s => (BetStatus)s.BetSelectionStatusId).ToList(), payout, bet.TrueStake);

            bet.WinLoss = payout == 0 ? -bet.Stake : payout - bet.Stake;
            bet.BetStatusId = (int)betStatus;
            bet.BetStatusName = betStatus.ToString();
            bet.Payout = payout;
            bet.SettlementDate = settledDate;
            bet.SettlementDateOffset = settledDate.AddHours(-4);
            bet.IsCashout = true;

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

            if (cashout == 1 && betSelections.Where(c => c == BetStatus.Void).Count() == betSelections.Count - 1)
                return BetStatus.CashedOut;

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

                var cashoutStake = (long)(message.Cashout * 10000);

                var betCashout = _factory.CreateTicketCashoutBuilder().SetTicketId(message.BetId.ToString().ToUpper()).SetCashoutStake(cashoutStake).BuildTicket();

                var response = mts.SendTicketCashoutBlocking(betCashout);

                _logger.LogInformation($"SendCashoutToMTS - Response - {JsonSerializer.Serialize(response)}");

                betStatus = response.Status;
                reason = $"{response.Reason.Code} - {response.Reason.Message}";

                return new Tuple<CashoutAcceptance, string>(betStatus, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendCashoutToMTS - {message.BetId.ToString().ToUpper()} - {ex.Message}");

                return new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }

    }
}
