using Platform.Betting.API.Data;
using Platform.Betting.API.Data.Entities;
using Platform.Betting.API.Utility;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using System;
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
            return new BaseResponseDTO<BetHistoryResponseDTO>();
        }
    }
}
