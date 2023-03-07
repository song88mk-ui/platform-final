using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Data.Reports;
using Platform.Shared.Enums;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using static Platform.AgentReports.API.Models.Response.OutstandingBetFixtureResponse;
using Platform.Shared.Models;
using Platform.AgentReports.API.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace Platform.AgentReports.API.Repository
{
    public class ReportsRepository : IReportsRepository
    {
        #region Fields

        private readonly AgentReportsContext _context;

        #endregion

        #region Ctor

        public ReportsRepository(AgentReportsContext context)
        {
            _context = context;
        }

        #endregion

        #region Methods

        public async Task<BaseResponseDTO<OutstandingBetFixtureResponse>> GetOutstandingBetFixtures(long agentId, int page, int pageSize, CurrencyCode currency)
        {
            var skip = (page - 1) * pageSize;
            double rowCount = 0;
            var result = new List<BetFixtureModel>();

            var fullTimeMarketTemplateIds = new List<string> { "1", "2", "3", "22", "23" };
            var halfTimeMarketTemplateIds = new List<string> { "8", "9", "10" };

            var marketTemplateIds = new List<string> { "1", "2", "3", "22", "23", "8", "9", "10" };

            var currencies = Enum.GetValues(typeof(CurrencyCode)).Cast<CurrencyCode>().Select(s => s.ToString()).ToList();

            rowCount = _context.Bet.Where(c => c.ParentUserId == agentId && c.ProductId == 1 && c.Count == 1 && c.StatusId == (int)BetStatus.Running && marketTemplateIds.Contains(c.MarketTemplateId) && currencies.Contains(c.CurrencyCode)).GroupBy(g => g.FixtureId).Count();
            var bets = _context.Bet.Where(c => c.ParentUserId == agentId && c.ProductId == 1 && c.Count == 1 && c.StatusId == (int)BetStatus.Running && marketTemplateIds.Contains(c.MarketTemplateId) && currencies.Contains(c.CurrencyCode))
                 .Select(s => new
                 {
                     SportId = s.SportId,
                     TransactionDate = s.TransactionDate,
                     FixtureId = s.FixtureId,
                     SportName = s.SportName,
                     SegmentId = s.SegmentId,
                     SegmentName = s.SegmentName,
                     LeagueId = s.LeagueId,
                     LeagueName = s.LeagueName,
                     FixtureName = s.FixtureName,
                     FixtureStartDate = s.FixtureStartDate,
                     MarketTemplateId = s.MarketTemplateId,
                     MarketName = s.MarketName,
                     PositionTaking = s.PositionTaking,
                     TrueStake = s.TrueStake
                 })
                .ToList().OrderByDescending(o => o.TransactionDate).GroupBy(g => g.FixtureId).Skip(skip).Take(pageSize).Select(s => new BetFixtureModel
                {
                    SportId = s.Select(ss => ss.SportId).FirstOrDefault(),
                    SportName = s.Select(ss => ss.SportName).FirstOrDefault(),
                    SegmentId = s.Select(ss => ss.SegmentId).FirstOrDefault(),
                    SegmentName = s.Select(ss => ss.SegmentName).FirstOrDefault(),
                    LeagueId = s.Select(ss => ss.LeagueId).FirstOrDefault(),
                    LeagueName = s.Select(ss => ss.LeagueName).FirstOrDefault(),
                    FixtureId = s.Key,
                    FixtureName = s.Select(ss => ss.FixtureName).FirstOrDefault(),
                    FixtureStartDate = s.Select(ss => ss.FixtureStartDate).FirstOrDefault(),
                    Markets = s.GroupBy(g => g.MarketTemplateId).Select(m => new BetFixtureMarketModel
                    {
                        MarketTemplateGroupId = fullTimeMarketTemplateIds.Contains(m.Key) ? "1" : "2",
                        MarketTemplateGroupName = fullTimeMarketTemplateIds.Contains(m.Key) ? "Full Time" : "Half Time",
                        MarketTemplateId = m.Key,
                        MarketName = m.Select(ss => ss.MarketName).FirstOrDefault(),
                        Receive = m.Sum(ss => (ss.PositionTaking * ss.TrueStake))
                    }).OrderBy(o => o.MarketTemplateId).ToList()
                }).ToList();

            if (bets.Count == 0)
            {
                return new BaseResponseDTO<OutstandingBetFixtureResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new OutstandingBetFixtureResponse
                    {
                        CurrentPage = page,
                        PageCount = (int)Math.Ceiling(rowCount / pageSize),
                        PageSize = pageSize,
                        RowCount = (int)rowCount,
                        Bets = result
                    }
                };
            }

            return new BaseResponseDTO<OutstandingBetFixtureResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new OutstandingBetFixtureResponse
                {
                    CurrentPage = page,
                    PageCount = (int)Math.Ceiling(rowCount / pageSize),
                    PageSize = pageSize,
                    RowCount = (int)rowCount,
                    Bets = bets
                }
            };
        }

        public async Task<BaseResponseDTO<OutstandingBetResponse>> GetAllBets(SearchBetRequest request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            var rowCount = (double)0;
            var result = new List<OutstandingBetResponse.Bet>();
            var totalReceive = 0m;
            var totalStake = 0m;
            var supportedProduct = new[] { 1 };
            var mergedResults = new List<OutstandingBetResponse.Bet>();

            var records = _context.Bet.Where(c => c.ParentUserId == request.AgentId && c.StatusId == (int)BetStatus.Running && supportedProduct.Contains(c.ProductId)).AsNoTracking();

            if (request.FromDate.HasValue)
                records = records.Where(c => c.TransactionDateOffset.Date >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
                records = records.Where(c => c.TransactionDateOffset.Date <= request.ToDate.Value.Date);

            if (!string.IsNullOrEmpty(request.UserName))
                records = records.Where(c => c.Username.Trim().ToLower().Contains(request.UserName.Trim().ToLower()));

            if (!string.IsNullOrEmpty(request.AgentPartnerUserName))
                records = records.Where(c => c.AgentUsername.ToString().Trim().ToLower().Contains(request.AgentPartnerUserName.Trim().ToLower()));

            if (!string.IsNullOrEmpty(request.BetId))
                records = records.Where(c => c.BetId.ToString().Trim().ToLower().Contains(request.BetId.Trim().ToLower()));

            rowCount = records.Count();
            totalReceive = records.Sum(s => s.PositionTaking * s.TrueStake);
            totalStake = records.Sum(s => s.Stake);
            var bets = records.OrderByDescending(o => o.TransactionDate).AsNoTracking().ToList();

            if (!bets.Any())
            {
                return new BaseResponseDTO<OutstandingBetResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new OutstandingBetResponse
                    {
                        CurrentPage = request.Page,
                        PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                        PageSize = request.PageSize,
                        RowCount = (int)rowCount,
                        Bets = new List<OutstandingBetResponse.Bet>()
                    }
                };
            }

            bets = bets.OrderByDescending(s => s.TransactionDate).Skip(skip).Take(request.PageSize).ToList();

            var betIds = bets.Select(s => s.BetId).Distinct().ToList();
            var betSelections = _context.BetSelection.Where(c => c.ParentUserId == request.AgentId && betIds.Contains(c.BetId)).ToList();

            foreach(var bet in bets)
            {
                var selections = betSelections.Where(c => c.BetId == bet.BetId).Select(s => new OutstandingBetResponse.BetSelection
                {
                    ParentUserId = s.ParentUserId,
                    BetId = s.BetId,
                    BetSelectionId = s.BetSelectionId,
                    UserId = s.UserId,
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = string.IsNullOrEmpty(s.LeagueName) ? s.SegmentName : s.LeagueName,
                    FixtureId = s.FixtureId,
                    FixtureName = s.FixtureName,
                    MarketId = s.MarketId,
                    MarketName = s.MarketName,
                    SelectionId = s.SelectionId,
                    SelectionName = s.SelectionName,
                    FixtureStatusId = s.FixtureStatusId,
                    FixtureStatusName = s.FixtureStatusName,
                    Line = s.Line,
                    Odds = s.Odds,
                    StatusId = s.StatusId,
                    StatusName = s.StatusName,
                    IsSettled = s.IsSettled,
                    IsResettled = s.IsResettled,
                    FixtureStartDate = s.FixtureStartDate,
                    MarketTemplateGroupId = s.MarketTemplateGroupId,
                    MarketTemplateGroupName = GetMarketTemplateGroupName(s.MarketTemplateGroupId),
                    MarketTemplateId = s.MarketTemplateId,
                    SelectionTemplateId = s.SelectionTemplateId,
                    Score = s.Score,
                }).ToList();

                var fixtureName = selections.Select(s => s.FixtureName).FirstOrDefault();

                result.Add(new OutstandingBetResponse.Bet
                {
                    ParentUserId = bet.ParentUserId,
                    BetId = bet.BetId,
                    UserId = bet.UserId,
                    Username = bet.Username,
                    PositionTaking = bet.PositionTaking,
                    Commission = bet.Commission,
                    Stake = bet.Stake,
                    TrueStake = bet.TrueStake,
                    Odds = bet.Odds,
                    OddsTypeId = bet.OddsTypeId,
                    OddsTypeName = bet.OddsTypeName,
                    Payout = bet.Payout,
                    WinLoss = bet.WinLoss,
                    StatusId = bet.StatusId,
                    StatusName = bet.StatusName,
                    Reason = bet.Reason,
                    CurrencyCode = bet.CurrencyCode,
                    IPAddress = bet.IPAddress,
                    TransactionDate = bet.TransactionDate,
                    FixtureName = fixtureName,
                    Receive = bet.Stake * bet.PositionTaking,
                    BetSelections = selections,
                    SelectionTemplateId = bet.SelectionTemplateId,
                    SelectionName = bet.SelectionName,
                    MarketTypeId = bet.MarketTypeId
                });
            }

            return new BaseResponseDTO<OutstandingBetResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new OutstandingBetResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    Bets = new List<OutstandingBetResponse.Bet>()
                }
            };
        }

        #endregion

        #region Utilities

        private string GetMarketTemplateGroupName(string marketTemplateGroupId)
        {
            switch (marketTemplateGroupId)
            {
                case "1":
                    return "Full Time";
                case "2":
                    return "Half Time";
                default:
                    return "Others";
            }
        }

        #endregion
    }
}
