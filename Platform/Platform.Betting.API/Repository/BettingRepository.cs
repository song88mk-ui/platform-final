using Microsoft.EntityFrameworkCore;
using Platform.Betting.API.Data;
using Platform.Betting.API.Data.Entities;
using Platform.Betting.API.Utility;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.Betting.API.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly BettingContext _bettingContext;
        private readonly IMessageBroker _messageBroker;
        public BettingRepository(BettingContext bettingContext, IMessageBroker messageBroker)
        {
            _bettingContext = bettingContext;
            _messageBroker = messageBroker;
            _messageBroker.Connect();
        }
        public async Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request)
        {
            //TODO: bet validation
            //TODO: Max Fixture Bet  

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    Guid betId;

                    var totalOdds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
                    var stake = request.Stake;

                    var betEntity = _bettingContext.Bet.Update(new Bet
                    {
                        UserId = request.UserId,
                        Stake = request.Stake,
                        TrueStake = stake,
                        Odds = totalOdds,
                        OddsTypeId = (int)request.OddsType,
                        OddsTypeName = request.OddsType.ToString(),
                        Payout = stake == request.Stake ? stake * totalOdds : stake + request.Stake,
                        WinLoss = 0m,
                        BetStatusId = (int)BetStatus.Waiting,
                        BetStatusName = BetStatus.Waiting.ToString(),
                        Reason = string.Empty,
                        CurrencyCode = request.CurrencyCode.ToString(),
                        LanguageCode = request.LanguageCode.ToString(),
                        ExchangeRate = request.ExchangeRate,
                        IPAddress = request.IPAddress,
                        Channel = request.Channel.ToString(),
                        ClientId = request.ClientId,
                        ClientCode = request.ClientCode,
                        LimitId = request.LimitId,
                        Endpoint = request.Endpoint,
                        OperatorToken = request.OperatorToken,
                        CompanyShare = request.CompanyShare,
                        IsResettled = false,
                        IsCashout = false,
                        BetDate = request.BetDate,
                        BetDateOffset = request.BetDate.AddHours(-4),
                        SettlementDate = request.BetDate,
                        SettlementDateOffset = request.BetDate.AddHours(-4)
                    }).Entity;

                    betId = betEntity.BetId;
                    request.BetId = betId;

                    _bettingContext.BetAudit.Add(new BetAudit
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

                    foreach (var selection in request.Selections)
                    {
                        var betSelectionEntity = _bettingContext.BetSelection.Update(new BetSelection
                        {
                            BetId = betId,
                            UserId = request.UserId,
                            SportId = selection.SportId,
                            SportName = selection.SportName,
                            SegmentId = selection.SegmentId,
                            SegmentName = selection.SegmentName,
                            LeagueId = selection.LeagueId,
                            LeagueName = selection.LeagueName,
                            FixtureId = selection.FixtureId,
                            FixtureName = selection.FixtureName,
                            FixtureTypeId = (int)selection.FixtureType,
                            FixtureTypeName = selection.FixtureType.ToString(),
                            MarketId = selection.MarketId,
                            MarketName = selection.MarketName,
                            SelectionId = selection.SelectionId,
                            SelectionName = selection.SelectionName,
                            FixtureStatusId = (int)selection.FixtureStatus,
                            FixtureStatusName = selection.FixtureStatus.ToString(),
                            Line = selection.Line,
                            Odds = selection.Odds,
                            BetSelectionStatusId = betEntity.BetStatusId,
                            BetSelectionStatusName = betEntity.BetStatusName,
                            IsSettled = false,
                            IsResettled = false,
                            FixtureSpecifier = selection.FixtureSpecifier,
                            SelectionSpecifier = selection.SelectionSpecifier,
                            FixtureStartDate = selection.FixtureStartDate,
                            BetHomeScore = selection.BetHomeScore,
                            BetAwayScore = selection.BetAwayScore,
                            SettlementHomeScore = string.Empty,
                            SettlementAwayScore = string.Empty,
                            CorrelationId = selection.CorrelationId
                        }).Entity;

                        var betSelectionId = betSelectionEntity.BetSelectionId;
                        selection.BetSelectionId = betSelectionId;

                        _bettingContext.BetSelectionAudit.Add(new BetSelectionAudit
                        {
                            BetId = betId,
                            BetSelectionId = betSelectionId,
                            UserId = request.UserId,
                            SportId = selection.SportId,
                            SportName = selection.SportName,
                            SegmentId = selection.SegmentId,
                            SegmentName = selection.SegmentName,
                            LeagueId = selection.LeagueId,
                            LeagueName = selection.LeagueName,
                            FixtureId = selection.FixtureId,
                            FixtureName = selection.FixtureName,
                            FixtureTypeId = (int)selection.FixtureType,
                            FixtureTypeName = selection.FixtureType.ToString(),
                            MarketId = selection.MarketId,
                            MarketName = selection.MarketName,
                            SelectionId = selection.SelectionId,
                            SelectionName = selection.SelectionName,
                            FixtureStatusId = (int)selection.FixtureStatus,
                            FixtureStatusName = selection.FixtureStatus.ToString(),
                            Line = selection.Line,
                            Odds = selection.Odds,
                            BetSelectionStatusId = betEntity.BetStatusId,
                            BetSelectionStatusName = betEntity.BetStatusName,
                            IsSettled = false,
                            IsResettled = false,
                            FixtureSpecifier = selection.FixtureSpecifier,
                            SelectionSpecifier = selection.SelectionSpecifier,
                            FixtureStartDate = selection.FixtureStartDate,
                            BetHomeScore = selection.BetHomeScore,
                            BetAwayScore = selection.BetAwayScore,
                            SettlementHomeScore = string.Empty,
                            SettlementAwayScore = string.Empty,
                            CorrelationId = selection.CorrelationId
                        });
                    }

                    _bettingContext.SaveChanges();
                    scope.Complete();

                    _messageBroker.Publish(new OnBetPlacement { BetId = betId }, "OnBetPlacement", "r.bet.placement", Guid.NewGuid().ToString().ToUpper());
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                    };
                }
            }

            return new BaseResponseDTO<BetResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetResponseDTO
                {
                    UserId = request.UserId,
                    BetId = request.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Waiting
                }
            };
        }

        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            double rowCount = 0;
            var result = new List<BetHistoryDTO>();
            var transactions = new List<Bet>();
            DateTime dateNow = DateTime.Now;
            if (request.IsCurrent)
            {
                // Get current bets waiting, running
                // Get cancelled and rejected with age of 5min
                //Accepted Status
                var betStatusWaitingRunning = new int[] { (int)BetStatus.Waiting, (int)BetStatus.Running };
                var betStatusCancelledRejected = new int[] { (int)BetStatus.Cancelled, (int)BetStatus.Rejected };

                var rawCurrent = _bettingContext.Bet.Where(c => c.BetDateOffset.Date >= request.FromDate && c.BetDateOffset.Date <= request.ToDate.Date && c.UserId == request.UserId && c.ClientId == request.ClientId && (betStatusWaitingRunning.Contains(c.BetStatusId) || (betStatusCancelledRejected.Contains(c.BetStatusId) && c.BetDateOffset.AddMinutes(5) <= DateTime.Now))).AsNoTracking();
                rowCount = rawCurrent.Count();
                transactions = rawCurrent.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
            }
            else
            {
                if (request.BetStatusFilter == BetStatusFilter.All)
                {
                    var rawAll = _bettingContext.Bet.Where(c => c.BetDateOffset.Date >= request.FromDate && c.BetDateOffset.Date <= request.ToDate.Date && c.UserId == request.UserId && c.ClientId == request.ClientId).AsNoTracking();

                    rowCount = rawAll.Count();
                    transactions = rawAll.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }

                if (request.BetStatusFilter == BetStatusFilter.Open)
                {
                    var rawOpen = _bettingContext.Bet.Where(c => c.BetDateOffset.Date >= request.FromDate.Date && c.BetDateOffset.Date <= request.ToDate.Date && c.UserId == request.UserId && c.BetStatusId < (int)BetStatus.Won && c.ClientId == request.ClientId).AsNoTracking();

                    rowCount = rawOpen.Count();
                    transactions = rawOpen.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }

                if (request.BetStatusFilter == BetStatusFilter.Settled)
                {
                    var rawSettled = _bettingContext.Bet.Where(c => c.BetDateOffset.Date >= request.FromDate.Date && c.BetDateOffset.Date <= request.ToDate.Date && c.UserId == request.UserId && c.BetStatusId > (int)BetStatus.Running && c.ClientId == request.ClientId).AsNoTracking();

                    rowCount = rawSettled.Count();
                    transactions = rawSettled.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }
            }
            

            if (transactions.Count == 0)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetHistoryResponseDTO
                    {
                        CurrentPage = request.Page,
                        PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                        PageSize = request.PageSize,
                        RowCount = request.RowCount,
                        Bets = result
                    }
                };
            }

            var winLoss = _bettingContext.Bet.Where(c => c.BetDateOffset.Date >= request.FromDate.Date && c.BetDateOffset.Date <= request.ToDate.Date).AsNoTracking();

            foreach (var bet in transactions)
            {
                var selections = _bettingContext.BetSelection.Where(c => c.BetId == bet.BetId).AsNoTracking();

                result.Add(new BetHistoryDTO
                {
                    BetId = bet.BetId.ToString(),
                    Stake = bet.Stake,
                    Odds = bet.Odds,
                    Payout = bet.Payout,
                    WinLoss = bet.WinLoss,
                    BetStatus = (BetStatus)bet.BetStatusId,
                    TransactionDate = bet.BetDate,
                    Commission = bet.CompanyShare,
                    CurrencyCode = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), bet.CurrencyCode),
                    OddsType = (OddsType)bet.OddsTypeId,
                    Selections = selections.Select(s => new BetSelectionHistoryDTO
                    {
                        SportId = s.SportId,
                        SportName = s.SportName,
                        LeagueId = s.LeagueId,
                        LeagueName = s.LeagueName,
                        FixtureId = s.FixtureId,
                        FixtureName = s.FixtureName,
                        MarketId = s.MarketId,
                        MarketName = s.MarketName,
                        Line = s.Line,
                        SelectionId = s.SelectionId,
                        SelectionName = s.SelectionName,
                        Odds = s.Odds,
                        BetAwayScore = s.BetAwayScore,
                        BetHomeScore = s.BetHomeScore,
                        BetSelectionId = s.BetSelectionId.ToString(),
                        FixtureStartDate = s.FixtureStartDate,
                        FixtureStatus = (FixtureStatus)s.FixtureStatusId,
                        MarketTemplateId = 0, // No available mapping
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        SelectionTemplateId= 0, // No available mapping
                        SettlementAwayScore = s.SettlementAwayScore,
                        SettlementHomeScore = s.SettlementHomeScore

                    }).ToList()
                });
            }

            return new BaseResponseDTO<BetHistoryResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetHistoryResponseDTO
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = request.RowCount,
                    Bets = result
                }
            };
        }
    }
}
