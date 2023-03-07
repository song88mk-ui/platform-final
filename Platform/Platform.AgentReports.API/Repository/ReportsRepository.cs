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
using Platform.Shared.Data.Reports.Entities;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace Platform.AgentReports.API.Repository
{
    public class ReportsRepository : IReportsRepository
    {

        #region Fields

        private readonly AgentReportsContext _context;
        private readonly IHttpClientFactory _httpClient;
        private readonly IConfiguration _config;

        #endregion

        #region CTOR

        public ReportsRepository(AgentReportsContext context, IHttpClientFactory httpClient, IConfiguration config)
        {
            _context = context;
            _httpClient = httpClient;
            _config = config;
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

        public BaseResponseDTO<WinLossResponse> GetProfitLossV2(long parentAgentId, ProfitLossRequest request)
        {
            #region W/O BACK AND LAY

            var skip = (request.Page - 1) * request.PageSize;
            double rowCount = 0;

            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };

            var iQueryableData = GetProfitLossAggregateQueryable2(parentAgentId, request).AsNoTracking();
            var membersCount = GetDistinctMembers2(request);

            var bets = iQueryableData.Select(s => new
            {
                IndirectUserId = s.IndirectUserId,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                BetId = s.BetId,
                ReferenceId = s.ReferenceId,
                Stake = s.Stake,
                TrueStake = s.TrueStake,
                StatusId = s.StatusId,
                Commission = s.Commission,
                MemberWinLoss = s.MemberWinLoss,
                MemberCommission = s.MemberCommission,
                MemberWLC = s.MemberWLC,
                AgentWinLoss = s.AgentWinLoss,
                AgentCommission = s.AgentCommission,
                AgentWLC = s.AgentWLC,
                DownlineWinLoss = s.DownlineWinLoss,
                DownlineCommission = s.DownlineCommission,
                DownlineWLC = s.DownlineWLC,
                UplineWinLoss = s.UplineWinLoss,
                UplineCommission = s.UplineCommission,
                UplineWLC = s.UplineWLC,
                ExchangeRate = s.ExchangeRate
            }).ToList().GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).Select(d => new WinLossModel()
            {
                UserId = d.Key.IndirectUserId,
                Username = d.Key.Username,
                UserTypeId = d.Key.UserTypeId,
                CurrencyCode = d.Key.CurrencyCode,
                StakeCount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).ToList().Distinct().Count(),
                StakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.Stake, ss.ExchangeRate }).ToList().Distinct().Sum(x => x.Stake / x.ExchangeRate),
                //ValidStakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.TrueStake, ss.ExchangeRate, ss.StatusId }).Where(ss => states.ToArray().Contains(ss.StatusId)).ToList().Distinct().Sum(x => x.TrueStake / x.ExchangeRate),
                ValidStakeAmount = d.Sum(x => x.TrueStake / x.ExchangeRate),
                Commission = d.Sum(s => s.Commission / s.ExchangeRate),
                MemberWinLoss = d.Sum(s => s.MemberWinLoss / s.ExchangeRate),
                MemberCommission = d.Sum(s => s.MemberCommission / s.ExchangeRate),
                MemberWLC = d.Sum(s => s.MemberWLC / s.ExchangeRate),
                AgentWinLoss = d.Sum(s => s.AgentWinLoss / s.ExchangeRate),
                AgentCommission = d.Sum(s => s.AgentCommission / s.ExchangeRate),
                AgentWLC = d.Sum(s => s.AgentWLC / s.ExchangeRate),
                DownlineWinLoss = d.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
                DownlineCommission = d.Sum(s => s.DownlineCommission / s.ExchangeRate),
                DownlineWLC = d.Sum(s => s.DownlineWLC / s.ExchangeRate),
                UplineWinLoss = d.Sum(s => s.UplineWinLoss / s.ExchangeRate),
                UplineCommission = d.Sum(s => s.UplineCommission / s.ExchangeRate),
                UplineWLC = d.Sum(s => s.UplineWLC / s.ExchangeRate),
                MemberCount = d.First().UserTypeId == (int)UserType.Player ? 1 : membersCount.First(m => m.Item1 == d.Key.IndirectUserId).Item2
            }).ToList();

            rowCount = bets.Count();
            var totalMemberCount = bets.Select(b => b.MemberCount).Sum();

            return new BaseResponseDTO<WinLossResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = bets.Sum(s => s.StakeAmount),
                    TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
                    TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
                    TotalCommission = bets.Sum(s => s.Commission),
                    TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
                    TotalMemberCommission = bets.Sum(s => s.MemberCommission),
                    TotalMemberWLC = bets.Sum(s => s.MemberWLC),
                    TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
                    TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
                    TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
                    TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
                    TotalAgentCommission = bets.Sum(s => s.AgentCommission),
                    TotalAgentWLC = bets.Sum(s => s.AgentWLC),
                    TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
                    TotalUplineCommission = bets.Sum(s => s.UplineCommission),
                    TotalUplineWLC = bets.Sum(s => s.UplineWLC),
                    TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
                    Bets = bets.Skip(skip).Take(request.PageSize).ToList(),
                    TotalMemberCount = totalMemberCount
                }
            };

            #endregion
        }
        public BaseResponseDTO<WinLossResponse> GetProfitLossV2(long parentAgentId, long directUserId, ProfitLossRequest request)
        {
            #region W/O BACK AND LAY

            var skip = (request.Page - 1) * request.PageSize;
            double rowCount = 0;

            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };

            var iQueryableData = GetProfitLossAggregateQueryableParentUserIdAndDirectUserId2(parentAgentId, directUserId, request).AsNoTracking();
            var membersCount = GetDistinctMembers2(request);

            var bets = iQueryableData.Select(s => new
            {
                IndirectUserId = s.IndirectUserId,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                BetId = s.BetId,
                ReferenceId = s.ReferenceId,
                Stake = s.Stake,
                TrueStake = s.TrueStake,
                StatusId = s.StatusId,
                Commission = s.Commission,
                MemberWinLoss = s.MemberWinLoss,
                MemberCommission = s.MemberCommission,
                MemberWLC = s.MemberWLC,
                AgentWinLoss = s.AgentWinLoss,
                AgentCommission = s.AgentCommission,
                AgentWLC = s.AgentWLC,
                DownlineWinLoss = s.DownlineWinLoss,
                DownlineCommission = s.DownlineCommission,
                DownlineWLC = s.DownlineWLC,
                UplineWinLoss = s.UplineWinLoss,
                UplineCommission = s.UplineCommission,
                UplineWLC = s.UplineWLC,
                ExchangeRate = s.ExchangeRate
            }).ToList().GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).Select(d => new WinLossModel()
            {
                UserId = d.Key.IndirectUserId,
                Username = d.Key.Username,
                UserTypeId = d.Key.UserTypeId,
                CurrencyCode = d.Key.CurrencyCode,
                StakeCount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).ToList().Distinct().Count(),
                StakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.Stake, ss.ExchangeRate }).ToList().Distinct().Sum(x => x.Stake / x.ExchangeRate),
                //ValidStakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.TrueStake, ss.ExchangeRate, ss.StatusId }).Where(ss => states.ToArray().Contains(ss.StatusId)).ToList().Distinct().Sum(x => x.TrueStake / x.ExchangeRate),
                ValidStakeAmount = d.Sum(x => x.TrueStake / x.ExchangeRate),
                Commission = d.Sum(s => s.Commission / s.ExchangeRate),
                MemberWinLoss = d.Sum(s => s.MemberWinLoss / s.ExchangeRate),
                MemberCommission = d.Sum(s => s.MemberCommission / s.ExchangeRate),
                MemberWLC = d.Sum(s => s.MemberWLC / s.ExchangeRate),
                AgentWinLoss = d.Sum(s => s.AgentWinLoss / s.ExchangeRate),
                AgentCommission = d.Sum(s => s.AgentCommission / s.ExchangeRate),
                AgentWLC = d.Sum(s => s.AgentWLC / s.ExchangeRate),
                DownlineWinLoss = d.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
                DownlineCommission = d.Sum(s => s.DownlineCommission / s.ExchangeRate),
                DownlineWLC = d.Sum(s => s.DownlineWLC / s.ExchangeRate),
                UplineWinLoss = d.Sum(s => s.UplineWinLoss / s.ExchangeRate),
                UplineCommission = d.Sum(s => s.UplineCommission / s.ExchangeRate),
                UplineWLC = d.Sum(s => s.UplineWLC / s.ExchangeRate),
                MemberCount = d.First().UserTypeId == (int)UserType.Player ? 1 : membersCount.First(m => m.Item1 == d.Key.IndirectUserId).Item2
            }).ToList();

            rowCount = bets.Count();
            var totalMemberCount = bets.Select(b => b.MemberCount).Sum();

            return new BaseResponseDTO<WinLossResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = bets.Sum(s => s.StakeAmount),
                    TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
                    TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
                    TotalCommission = bets.Sum(s => s.Commission),
                    TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
                    TotalMemberCommission = bets.Sum(s => s.MemberCommission),
                    TotalMemberWLC = bets.Sum(s => s.MemberWLC),
                    TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
                    TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
                    TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
                    TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
                    TotalAgentCommission = bets.Sum(s => s.AgentCommission),
                    TotalAgentWLC = bets.Sum(s => s.AgentWLC),
                    TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
                    TotalUplineCommission = bets.Sum(s => s.UplineCommission),
                    TotalUplineWLC = bets.Sum(s => s.UplineWLC),
                    TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
                    Bets = bets.Skip(skip).Take(request.PageSize).ToList(),
                    TotalMemberCount = totalMemberCount
                }
            };

            #endregion
        }
        public BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetailsV2(long parentAgentId, long directUserId, ProfitLossRequest request)
        {
            #region W/O BACK AND LAY

            var skip = (request.Page - 1) * request.PageSize;
            var productTypes = request.Product.Split(',').Select(int.Parse).ToList();
            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            IQueryable<WinLoss> profitLossDetails;
            string[] currencies = new string[0];

            if (!string.IsNullOrEmpty(request.Currency))
            {
                currencies = request.Currency.Split(',');
            }

            var accounts = new List<string>();
            if (!string.IsNullOrEmpty(request.Accounts))
            {
                accounts = request.Accounts.Split(',').ToList();
            }

            var betIds = new List<string>();
            if (!string.IsNullOrEmpty(request.BetIds))
            {
                betIds = request.BetIds.Split(',').ToList();
            }

            var status = new List<string>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                status = request.Status.Split(',').Select(s => s.ToUpper().Replace("-", "")).ToList();
            }


            if (request.DateFilter == "transactiondate")
            {
                profitLossDetails = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentAgentId &&
                    w.IndirectUserId == directUserId &&
                    w.TransactionDateOffset >= request.FromDate &&
                    w.TransactionDateOffset < request.ToDate.AddDays(1) &&
                    currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }
            else
            {
                profitLossDetails = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentAgentId &&
                    w.IndirectUserId == directUserId &&
                    w.SettledDateOffset >= request.FromDate &&
                    w.SettledDateOffset < request.ToDate.AddDays(1) &&
                    currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId))).AsNoTracking();
            }
            else
            {
                profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId)).AsNoTracking();
            }

            if (accounts.Any())
            {
                profitLossDetails = profitLossDetails.Where(r => accounts.Contains(r.Username));
            }

            if (status.Any())
            {
                profitLossDetails = profitLossDetails.Where(p => status.Any(s => s.Equals((string.IsNullOrEmpty(p.ReferenceId) || p.StatusName.ToUpper() == "CANCELLED") ? p.StatusName.ToUpper() : $"ADJUSTMENT{p.StatusName.ToUpper()}")));
            }

            if (betIds.Any())
            {
                //profitLossDetails = profitLossDetails.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                profitLossDetails = profitLossDetails.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
            }

            var betsRaw = profitLossDetails.Select(ss => new
            {
                ss.SettledDate,
                ss.IndirectUserId,
                ss.ReferenceId,
                ss.BetId,
                ss.TrueStake,
                ss.TransactionDate,
                ss.Username,
                ss.UserTypeId,
                ss.CurrencyCode,
                ss.Stake,
                ss.Commission,
                ss.MemberCommission,
                ss.MemberWinLoss,
                ss.MemberWLC,
                ss.DownlineCommission,
                ss.DownlineWLC,
                ss.DownlineWinLoss,
                ss.AgentWinLoss,
                ss.AgentCommission,
                ss.AgentWLC,
                ss.UplineCommission,
                ss.UplineWinLoss,
                ss.UplineWLC,
                ss.IsParlay,
                ss.Odds,
                ss.OddsTypeId,
                ss.OddsTypeName,
                ss.StatusId,
                ss.StatusName,
                ss.ProductId,
                ss.SegmentName,
                ss.ProductName,
                ss.LeagueId,
                ss.LeagueName,
                ss.FixtureId,
                ss.FixtureName,
                ss.FixtureStartDate,
                ss.MarketTemplateId,
                ss.MarketName,
                ss.SelectionTemplateId,
                ss.SelectionName,
                ss.Score,
                ss.FixtureStatusId,
                ss.FixtureStatusName,
                ss.Line,
                ss.MarketTemplateGroupId,
                ss.MarketTemplateGroupName,
                ss.PositionTakingDetails,
                ss.CommissionDetails,
                ss.CasinoDetails,
                ss.MarketTypeId,
                ss.MarketGroupId,
                ss.ExchangeRate
            }).ToList();

            double rowCount = betsRaw.Count();

            if (rowCount == 0)
            {
                return new BaseResponseDTO<WinLossDetailsResponse>()
                {
                    Result = new WinLossDetailsResponse()
                    {
                        Bets = new List<WinLossDetailsModel>(),
                        CurrentPage = request.PageSize,
                        PageCount = 0,
                        PageSize = request.PageSize
                    },
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString()
                };
            }

            var totalStakeAmount = betsRaw.Sum(s => s.Stake / s.ExchangeRate);
            var totalValidStakeAmount = betsRaw.Sum(s => s.TrueStake / s.ExchangeRate);

            var bets = betsRaw.OrderBy(p => p.SettledDate).Skip(skip).Take(request.PageSize).ToList().Select(s => new WinLossDetailsModel()
            {
                UserId = s.IndirectUserId,
                BetId = string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId),
                TransactionDate = s.TransactionDate,
                SettlementDate = s.SettledDate,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                StakeAmount = s.Stake / s.ExchangeRate,
                ValidStakeAmount = s.TrueStake / s.ExchangeRate,
                Commission = s.Commission / s.ExchangeRate,
                MemberWinLoss = s.MemberWinLoss / s.ExchangeRate,
                MemberCommission = s.MemberCommission / s.ExchangeRate,
                MemberWLC = s.MemberWLC / s.ExchangeRate,
                DownlineWinLoss = s.DownlineWinLoss / s.ExchangeRate,
                DownlineCommission = s.DownlineCommission / s.ExchangeRate,
                DownlineWLC = s.DownlineWLC / s.ExchangeRate,
                AgentWinLoss = s.AgentWinLoss / s.ExchangeRate,
                AgentCommission = s.AgentCommission / s.ExchangeRate,
                AgentWLC = s.AgentWLC / s.ExchangeRate,
                UplineCommission = s.UplineCommission / s.ExchangeRate,
                UplineWinLoss = s.UplineWinLoss / s.ExchangeRate,
                UplineWLC = s.UplineWLC / s.ExchangeRate,
                IsParlay = s.IsParlay,
                Stake = s.Stake / s.ExchangeRate,
                TrueStake = s.TrueStake / s.ExchangeRate,
                Odds = s.Odds,
                OddsTypeId = s.OddsTypeId,
                OddsTypeName = s.OddsTypeName,
                StatusId = s.StatusId,
                StatusName = (string.IsNullOrEmpty(s.ReferenceId) || s.StatusName.ToUpper() == "CANCELLED") ? s.StatusName : $"Adjustment-{s.StatusName}",
                VirtualSportName = s.ProductId == 2 ? s.SegmentName : string.Empty,
                ProductId = s.ProductId,
                ProductName = s.ProductName,
                LeagueId = s.LeagueId,
                LeagueName = string.IsNullOrEmpty(s.LeagueName) ? s.SegmentName : s.LeagueName,
                FixtureId = s.FixtureId,
                FixtureName = s.FixtureName,
                FixtureStartDate = s.FixtureStartDate,
                MarketTemplateId = s.MarketTemplateId,
                MarketName = s.MarketName,
                SelectionTemplateId = s.SelectionTemplateId,
                SelectionName = s.SelectionName,
                Score = s.Score,
                FixtureStatusId = s.FixtureStatusId,
                FixtureStatusName = s.FixtureStatusName,
                Line = s.Line,
                MarketTemplateGroupId = s.MarketTemplateGroupId,
                MarketTemplateGroupName = s.MarketTemplateGroupName,
                PositionTakingDetails = JsonConvert.DeserializeObject<PositionTakingDetails>(s.PositionTakingDetails),
                CommissionDetails = JsonConvert.DeserializeObject<List<CommissionDetails>>(s.CommissionDetails),
                CasinoDetails = GetCasinoDetailForPL(s.CasinoDetails, s.ExchangeRate),
                Reason = string.IsNullOrEmpty(s.ReferenceId) ? string.Empty : $"ADJUSTMENT - {s.StatusName}",
                BetScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(1, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                FinalScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(2, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                HalftimeScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(3, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                MarketTypeId = s.MarketTypeId,
                MarketGroupId = s.MarketGroupId
            }).ToList();

            return new BaseResponseDTO<WinLossDetailsResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossDetailsResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = totalStakeAmount,
                    TotalValidStakeAmount = totalValidStakeAmount,
                    TotalCommission = betsRaw.Sum(s => s.Commission / s.ExchangeRate),
                    TotalMemberWinLoss = betsRaw.Sum(s => s.MemberWinLoss / s.ExchangeRate),
                    TotalMemberCommission = betsRaw.Sum(s => s.MemberCommission / s.ExchangeRate),
                    TotalMemberWLC = betsRaw.Sum(s => s.MemberWLC / s.ExchangeRate),
                    TotalDownlineWinLoss = betsRaw.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
                    TotalDownlineCommission = betsRaw.Sum(s => s.DownlineCommission / s.ExchangeRate),
                    TotalDownlineWLC = betsRaw.Sum(s => s.DownlineWLC / s.ExchangeRate),
                    TotalAgentWinLoss = betsRaw.Sum(s => s.AgentWinLoss / s.ExchangeRate),
                    TotalAgentCommission = betsRaw.Sum(s => s.AgentCommission / s.ExchangeRate),
                    TotalAgentWLC = betsRaw.Sum(s => s.AgentWLC / s.ExchangeRate),
                    TotalUplineWinLoss = betsRaw.Sum(s => s.UplineWinLoss / s.ExchangeRate),
                    TotalUplineCommission = betsRaw.Sum(s => s.UplineCommission / s.ExchangeRate),
                    TotalUplineWLC = betsRaw.Sum(s => s.UplineWLC / s.ExchangeRate),
                    Bets = bets.OrderBy(s => s.TransactionDate).ToList()
                }
            };

            #endregion
        }
        public BaseResponseDTO<WinLossResponse> GetProfitLoss(long parentAgentId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter)
        {
            #region W/ BACK AND LAY

            //var skip = (page - 1) * pageSize;
            //double rowCount = 0;

            //var productTypes = product.Split(',').Select(int.Parse).ToList();
            //string[] currencies = new string[0];

            //if (!string.IsNullOrEmpty(currency))
            //{
            //    currencies = currency.Split(',');
            //}

            //string[] statuses = new string[0];
            //var backAndLayCount = new Dictionary<long, int>();

            //if (!string.IsNullOrEmpty(status))
            //{
            //    statuses = status.Split(',');
            //}

            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };

            //var placeholders = new List<BackAndLayDataPlaceholder>();

            //var iQueryableData = GetProfitLossAggregateQueryable(parentAgentId, fromDate, toDate, currencies, dateFilter, productTypes).AsNoTracking();

            //foreach (var data in iQueryableData.Where(s => s.MarketTypeId > 0).GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).AsNoTracking())
            //{
            //    var betIds = data.Select(s => s.BetId).Distinct().ToList();

            //    var liabilities = new List<BLCurrentLiabilitiesResponse>();

            //    var settledStatus = new List<int?>() { (int)StatusType.Won, (int)StatusType.Lost };

            //    if (betIds.Any())
            //        liabilities = _backAndLaySheetHelper.GetBackAndLayLiabilitiesBasedOnBetIds(betIds);

            //    placeholders.Add(new BackAndLayDataPlaceholder()
            //    {
            //        UserId = data.Key.IndirectUserId,
            //        StakeCount = liabilities.Where(s => s.FinalStatus != null).Count(),
            //        Stake = liabilities.Where(s => settledStatus.Contains(s.FinalStatus)).Sum(s => s.CurrentStake * -1)
            //    });
            //}

            //var groupedData = iQueryableData.GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).AsNoTracking().ToList();

            //var bets = new List<WinLossModel>();

            //foreach (var d in groupedData)
            //{
            //    var betIds = d.Where(w => w.MarketTypeId > 0).Select(s => s.BetId).ToList();

            //    var sheets = new List<BackAndLayWalletResponse>();

            //    if (betIds.Any())
            //        sheets = _backAndLaySheetHelper.GetBackAndLaySheetByBetIds(betIds);

            //    var backAndLayBetIds = sheets.Select(s => s.BetId).ToList();

            //    bets.Add(new WinLossModel()
            //    {
            //        UserId = d.Key.IndirectUserId,
            //        Username = d.Key.Username,
            //        UserTypeId = d.Key.UserTypeId,
            //        CurrencyCode = d.Key.CurrencyCode,
            //        StakeCount = d.Count(s => !backAndLayBetIds.Contains(s.BetId)) +
            //                        (placeholders.FirstOrDefault(s => s.UserId == d.Key.IndirectUserId) == null
            //                            ? 0.00m
            //                            : placeholders.First(s => s.UserId == d.Key.IndirectUserId).StakeCount),
            //        StakeAmount = d.Where(s => !backAndLayBetIds.Contains(s.BetId) && states.Contains(s.StatusId)).Sum(s => s.Stake / s.ExchangeRate) -
            //                         d.Where(s =>
            //                             !string.IsNullOrEmpty(s.ReferenceId) && !backAndLayBetIds.Contains(s.BetId) &&
            //                             states.Contains(s.StatusId)).Sum(s => s.Stake / s.ExchangeRate) +
            //                         (placeholders.FirstOrDefault(s => s.UserId == d.Key.IndirectUserId) == null
            //                             ? 0.00m
            //                             : placeholders.First(s => s.UserId == d.Key.IndirectUserId).Stake /
            //                               d.First().ExchangeRate),
            //        ValidStakeAmount =
            //               d.Where(s => states.Contains(s.StatusId) && !backAndLayBetIds.Contains(s.BetId))
            //                   .Sum(s => s.TrueStake / s.ExchangeRate) +
            //               (placeholders.FirstOrDefault(s => s.UserId == d.Key.IndirectUserId) == null
            //                   ? 0.00m
            //                   : placeholders.First(s => s.UserId == d.Key.IndirectUserId).Stake / d.First().ExchangeRate),
            //        Commission = d.Sum(s => s.Commission / s.ExchangeRate),
            //        MemberWinLoss = d.Sum(s => s.MemberWinLoss / s.ExchangeRate),
            //        MemberCommission = d.Sum(s => s.MemberCommission / s.ExchangeRate),
            //        MemberWLC = d.Sum(s => s.MemberWLC / s.ExchangeRate),
            //        AgentWinLoss = d.Sum(s => s.AgentWinLoss / s.ExchangeRate),
            //        AgentCommission = d.Sum(s => s.AgentCommission / s.ExchangeRate),
            //        AgentWLC = d.Sum(s => s.AgentWLC / s.ExchangeRate),
            //        DownlineWinLoss = d.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
            //        DownlineCommission = d.Sum(s => s.DownlineCommission / s.ExchangeRate),
            //        DownlineWLC = d.Sum(s => s.DownlineWLC / s.ExchangeRate),
            //        UplineWinLoss = d.Sum(s => s.UplineWinLoss / s.ExchangeRate),
            //        UplineCommission = d.Sum(s => s.UplineCommission / s.ExchangeRate),
            //        UplineWLC = d.Sum(s => s.UplineWLC / s.ExchangeRate),
            //    });
            //}

            //rowCount = bets.Count();

            //return new BaseResponseDTO<WinLossResponse>
            //{
            //    StatusCode = (int)ResultType.Success,
            //    StatusName = ResultType.Success.ToString(),
            //    Result = new WinLossResponse
            //    {
            //        CurrentPage = page,
            //        PageCount = (int)Math.Ceiling(rowCount / pageSize),
            //        PageSize = pageSize,
            //        RowCount = (int)rowCount,
            //        TotalStakeAmount = bets.Sum(s => s.StakeAmount),
            //        TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
            //        TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
            //        TotalCommission = bets.Sum(s => s.Commission),
            //        TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
            //        TotalMemberCommission = bets.Sum(s => s.MemberCommission),
            //        TotalMemberWLC = bets.Sum(s => s.MemberWLC),
            //        TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
            //        TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
            //        TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
            //        TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
            //        TotalAgentCommission = bets.Sum(s => s.AgentCommission),
            //        TotalAgentWLC = bets.Sum(s => s.AgentWLC),
            //        TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
            //        TotalUplineCommission = bets.Sum(s => s.UplineCommission),
            //        TotalUplineWLC = bets.Sum(s => s.UplineWLC),
            //        TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
            //        Bets = bets.Where(s => s.StakeCount > 0).Skip(skip).Take(pageSize).ToList()
            //    }
            //};

            #endregion

            #region W/O BACK AND LAY

            var skip = (page - 1) * pageSize;
            double rowCount = 0;

            var productTypes = product.Split(',').Select(int.Parse).ToList();
            string[] currencies = new string[0];

            if (!string.IsNullOrEmpty(currency))
            {
                currencies = currency.Split(',');
            }

            var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };

            var iQueryableData = GetProfitLossAggregateQueryable(parentAgentId, fromDate, toDate, currencies, dateFilter, productTypes).AsNoTracking();
            var membersCount = GetDistinctMembers(fromDate, toDate, currencies, dateFilter, productTypes);

            var bets = iQueryableData.Select(s => new
            {
                IndirectUserId = s.IndirectUserId,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                BetId = s.BetId,
                ReferenceId = s.ReferenceId,
                Stake = s.Stake,
                TrueStake = s.TrueStake,
                StatusId = s.StatusId,
                Commission = s.Commission,
                MemberWinLoss = s.MemberWinLoss,
                MemberCommission = s.MemberCommission,
                MemberWLC = s.MemberWLC,
                AgentWinLoss = s.AgentWinLoss,
                AgentCommission = s.AgentCommission,
                AgentWLC = s.AgentWLC,
                DownlineWinLoss = s.DownlineWinLoss,
                DownlineCommission = s.DownlineCommission,
                DownlineWLC = s.DownlineWLC,
                UplineWinLoss = s.UplineWinLoss,
                UplineCommission = s.UplineCommission,
                UplineWLC = s.UplineWLC,
                ExchangeRate = s.ExchangeRate
            }).ToList().GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).Select(d => new WinLossModel()
            {
                UserId = d.Key.IndirectUserId,
                Username = d.Key.Username,
                UserTypeId = d.Key.UserTypeId,
                CurrencyCode = d.Key.CurrencyCode,
                StakeCount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).ToList().Distinct().Count(),
                StakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.Stake, ss.ExchangeRate }).ToList().Distinct().Sum(x => x.Stake / x.ExchangeRate),
                //ValidStakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.TrueStake, ss.ExchangeRate, ss.StatusId }).Where(ss => states.ToArray().Contains(ss.StatusId)).ToList().Distinct().Sum(x => x.TrueStake / x.ExchangeRate),
                ValidStakeAmount = d.Sum(x => x.TrueStake / x.ExchangeRate),
                Commission = d.Sum(s => s.Commission / s.ExchangeRate),
                MemberWinLoss = d.Sum(s => s.MemberWinLoss / s.ExchangeRate),
                MemberCommission = d.Sum(s => s.MemberCommission / s.ExchangeRate),
                MemberWLC = d.Sum(s => s.MemberWLC / s.ExchangeRate),
                AgentWinLoss = d.Sum(s => s.AgentWinLoss / s.ExchangeRate),
                AgentCommission = d.Sum(s => s.AgentCommission / s.ExchangeRate),
                AgentWLC = d.Sum(s => s.AgentWLC / s.ExchangeRate),
                DownlineWinLoss = d.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
                DownlineCommission = d.Sum(s => s.DownlineCommission / s.ExchangeRate),
                DownlineWLC = d.Sum(s => s.DownlineWLC / s.ExchangeRate),
                UplineWinLoss = d.Sum(s => s.UplineWinLoss / s.ExchangeRate),
                UplineCommission = d.Sum(s => s.UplineCommission / s.ExchangeRate),
                UplineWLC = d.Sum(s => s.UplineWLC / s.ExchangeRate),
                MemberCount = d.First().UserTypeId == (int)UserType.Player ? 1 : membersCount.First(m => m.Item1 == d.Key.IndirectUserId).Item2
            }).ToList();

            rowCount = bets.Count();
            var totalMemberCount = bets.Select(b => b.MemberCount).Sum();

            return new BaseResponseDTO<WinLossResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossResponse
                {
                    CurrentPage = page,
                    PageCount = (int)Math.Ceiling(rowCount / pageSize),
                    PageSize = pageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = bets.Sum(s => s.StakeAmount),
                    TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
                    TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
                    TotalCommission = bets.Sum(s => s.Commission),
                    TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
                    TotalMemberCommission = bets.Sum(s => s.MemberCommission),
                    TotalMemberWLC = bets.Sum(s => s.MemberWLC),
                    TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
                    TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
                    TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
                    TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
                    TotalAgentCommission = bets.Sum(s => s.AgentCommission),
                    TotalAgentWLC = bets.Sum(s => s.AgentWLC),
                    TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
                    TotalUplineCommission = bets.Sum(s => s.UplineCommission),
                    TotalUplineWLC = bets.Sum(s => s.UplineWLC),
                    TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
                    Bets = bets.Skip(skip).Take(pageSize).ToList(),
                    TotalMemberCount = totalMemberCount
                }
            };

            #endregion
        }
        public BaseResponseDTO<WinLossResponse> GetProfitLoss(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter)
        {
            #region W/ BACK AND LAY

            //var skip = (page - 1) * pageSize;
            //double rowCount = 0;

            //var productTypes = product.Split(',').Select(int.Parse).ToList();
            //string[] currencies = new string[0];

            //if (!string.IsNullOrEmpty(currency))
            //{
            //    currencies = currency.Split(',');
            //}

            //string[] statuses = new string[0];

            //if (!string.IsNullOrEmpty(status))
            //{
            //    statuses = status.Split(',');
            //}

            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };

            //var placeholders = new List<BackAndLayDataPlaceholder>();

            //var iQueryableData = GetProfitLossAggregateQueryableParentUserIdAndDirectUserId(parentAgentId, directUserId, fromDate, toDate, currencies, dateFilter, productTypes).AsNoTracking();

            //foreach (var data in iQueryableData.Where(s => s.MarketTypeId > 0).GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).AsNoTracking())
            //{
            //    var betIds = data.Select(s => s.BetId).Distinct().ToList();

            //    var liabilities = new List<BLCurrentLiabilitiesResponse>();

            //    var settledStatus = new List<int?>() { (int)StatusType.Won, (int)StatusType.Lost };

            //    if (betIds.Any())
            //        liabilities = _backAndLaySheetHelper.GetBackAndLayLiabilitiesBasedOnBetIds(betIds);

            //    placeholders.Add(new BackAndLayDataPlaceholder()
            //    {
            //        UserId = data.Key.IndirectUserId,
            //        StakeCount = liabilities.Where(s => s.FinalStatus != null).Count(),
            //        Stake = liabilities.Where(s => settledStatus.Contains(s.FinalStatus)).Sum(s => s.CurrentStake * -1)
            //    });
            //}

            //var groupedData = iQueryableData.GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).AsNoTracking().ToList();

            //var bets = new List<WinLossModel>();

            //foreach (var d in groupedData)
            //{
            //    var betIds = d.Where(w => w.MarketTypeId > 0).Select(s => s.BetId).ToList();

            //    var sheets = new List<BackAndLayWalletResponse>();

            //    if (betIds.Any())
            //        sheets = _backAndLaySheetHelper.GetBackAndLaySheetByBetIds(betIds);

            //    var backAndLayBetIds = sheets.Select(s => s.BetId).ToList();

            //    bets.Add(new WinLossModel()
            //    {
            //        UserId = d.Key.IndirectUserId,
            //        Username = d.Key.Username,
            //        UserTypeId = d.Key.UserTypeId,
            //        CurrencyCode = d.Key.CurrencyCode,
            //        StakeCount = d.Count(s => !backAndLayBetIds.Contains(s.BetId)) +
            //                      (placeholders.FirstOrDefault(s => s.UserId == d.Key.IndirectUserId) == null
            //                          ? 0.00m
            //                          : placeholders.First(s => s.UserId == d.Key.IndirectUserId).StakeCount),
            //        StakeAmount = d.Where(s => !backAndLayBetIds.Contains(s.BetId) && states.Contains(s.StatusId)).Sum(s => s.Stake / s.ExchangeRate) -
            //                       d.Where(s =>
            //                           !string.IsNullOrEmpty(s.ReferenceId) && !backAndLayBetIds.Contains(s.BetId) &&
            //                           states.Contains(s.StatusId)).Sum(s => s.Stake / s.ExchangeRate) +
            //                       (placeholders.FirstOrDefault(s => s.UserId == d.Key.IndirectUserId) == null
            //                           ? 0.00m
            //                           : placeholders.First(s => s.UserId == d.Key.IndirectUserId).Stake /
            //                             d.First().ExchangeRate),
            //        ValidStakeAmount =
            //             d.Where(s => states.Contains(s.StatusId) && !backAndLayBetIds.Contains(s.BetId))
            //                 .Sum(s => s.TrueStake / s.ExchangeRate) +
            //             (placeholders.FirstOrDefault(s => s.UserId == d.Key.IndirectUserId) == null
            //                 ? 0.00m
            //                 : placeholders.First(s => s.UserId == d.Key.IndirectUserId).Stake / d.First().ExchangeRate),
            //        Commission = d.Sum(s => s.Commission / s.ExchangeRate),
            //        MemberWinLoss = d.Sum(s => s.MemberWinLoss / s.ExchangeRate),
            //        MemberCommission = d.Sum(s => s.MemberCommission / s.ExchangeRate),
            //        MemberWLC = d.Sum(s => s.MemberWLC / s.ExchangeRate),
            //        AgentWinLoss = d.Sum(s => s.AgentWinLoss / s.ExchangeRate),
            //        AgentCommission = d.Sum(s => s.AgentCommission / s.ExchangeRate),
            //        AgentWLC = d.Sum(s => s.AgentWLC / s.ExchangeRate),
            //        DownlineWinLoss = d.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
            //        DownlineCommission = d.Sum(s => s.DownlineCommission / s.ExchangeRate),
            //        DownlineWLC = d.Sum(s => s.DownlineWLC / s.ExchangeRate),
            //        UplineWinLoss = d.Sum(s => s.UplineWinLoss / s.ExchangeRate),
            //        UplineCommission = d.Sum(s => s.UplineCommission / s.ExchangeRate),
            //        UplineWLC = d.Sum(s => s.UplineWLC / s.ExchangeRate),
            //    });
            //}

            //rowCount = bets.Count();

            //return new BaseResponseDTO<WinLossResponse>
            //{
            //    StatusCode = (int)ResultType.Success,
            //    StatusName = ResultType.Success.ToString(),
            //    Result = new WinLossResponse
            //    {
            //        CurrentPage = page,
            //        PageCount = (int)Math.Ceiling(rowCount / pageSize),
            //        PageSize = pageSize,
            //        RowCount = (int)rowCount,
            //        TotalStakeAmount = bets.Sum(s => s.StakeAmount),
            //        TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
            //        TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
            //        TotalCommission = bets.Sum(s => s.Commission),
            //        TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
            //        TotalMemberCommission = bets.Sum(s => s.MemberCommission),
            //        TotalMemberWLC = bets.Sum(s => s.MemberWLC),
            //        TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
            //        TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
            //        TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
            //        TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
            //        TotalAgentCommission = bets.Sum(s => s.AgentCommission),
            //        TotalAgentWLC = bets.Sum(s => s.AgentWLC),
            //        TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
            //        TotalUplineCommission = bets.Sum(s => s.UplineCommission),
            //        TotalUplineWLC = bets.Sum(s => s.UplineWLC),
            //        TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
            //        Bets = bets.Where(s => s.StakeCount > 0).Skip(skip).Take(pageSize).ToList()
            //    }
            //};

            #endregion

            #region W/O BACK AND LAY

            var skip = (page - 1) * pageSize;
            double rowCount = 0;

            var productTypes = product.Split(',').Select(int.Parse).ToList();
            string[] currencies = new string[0];

            if (!string.IsNullOrEmpty(currency))
            {
                currencies = currency.Split(',');
            }

            var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };

            var iQueryableData = GetProfitLossAggregateQueryableParentUserIdAndDirectUserId(parentAgentId, directUserId, fromDate, toDate, currencies, dateFilter, productTypes).AsNoTracking();
            var membersCount = GetDistinctMembers(fromDate, toDate, currencies, dateFilter, productTypes);

            var bets = iQueryableData.Select(s => new
            {
                IndirectUserId = s.IndirectUserId,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                BetId = s.BetId,
                ReferenceId = s.ReferenceId,
                Stake = s.Stake,
                TrueStake = s.TrueStake,
                StatusId = s.StatusId,
                Commission = s.Commission,
                MemberWinLoss = s.MemberWinLoss,
                MemberCommission = s.MemberCommission,
                MemberWLC = s.MemberWLC,
                AgentWinLoss = s.AgentWinLoss,
                AgentCommission = s.AgentCommission,
                AgentWLC = s.AgentWLC,
                DownlineWinLoss = s.DownlineWinLoss,
                DownlineCommission = s.DownlineCommission,
                DownlineWLC = s.DownlineWLC,
                UplineWinLoss = s.UplineWinLoss,
                UplineCommission = s.UplineCommission,
                UplineWLC = s.UplineWLC,
                ExchangeRate = s.ExchangeRate
            }).ToList().GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).Select(d => new WinLossModel()
            {
                UserId = d.Key.IndirectUserId,
                Username = d.Key.Username,
                UserTypeId = d.Key.UserTypeId,
                CurrencyCode = d.Key.CurrencyCode,
                StakeCount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).ToList().Distinct().Count(),
                StakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.Stake, ss.ExchangeRate }).ToList().Distinct().Sum(x => x.Stake / x.ExchangeRate),
                //ValidStakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.TrueStake, ss.ExchangeRate, ss.StatusId }).Where(ss => states.ToArray().Contains(ss.StatusId)).ToList().Distinct().Sum(x => x.TrueStake / x.ExchangeRate),
                ValidStakeAmount = d.Sum(x => x.TrueStake / x.ExchangeRate),
                Commission = d.Sum(s => s.Commission / s.ExchangeRate),
                MemberWinLoss = d.Sum(s => s.MemberWinLoss / s.ExchangeRate),
                MemberCommission = d.Sum(s => s.MemberCommission / s.ExchangeRate),
                MemberWLC = d.Sum(s => s.MemberWLC / s.ExchangeRate),
                AgentWinLoss = d.Sum(s => s.AgentWinLoss / s.ExchangeRate),
                AgentCommission = d.Sum(s => s.AgentCommission / s.ExchangeRate),
                AgentWLC = d.Sum(s => s.AgentWLC / s.ExchangeRate),
                DownlineWinLoss = d.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
                DownlineCommission = d.Sum(s => s.DownlineCommission / s.ExchangeRate),
                DownlineWLC = d.Sum(s => s.DownlineWLC / s.ExchangeRate),
                UplineWinLoss = d.Sum(s => s.UplineWinLoss / s.ExchangeRate),
                UplineCommission = d.Sum(s => s.UplineCommission / s.ExchangeRate),
                UplineWLC = d.Sum(s => s.UplineWLC / s.ExchangeRate),
                MemberCount = d.First().UserTypeId == (int)UserType.Player ? 1 : membersCount.First(m => m.Item1 == d.Key.IndirectUserId).Item2
            }).ToList();

            rowCount = bets.Count();
            var totalMemberCount = bets.Select(b => b.MemberCount).Sum();

            return new BaseResponseDTO<WinLossResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossResponse
                {
                    CurrentPage = page,
                    PageCount = (int)Math.Ceiling(rowCount / pageSize),
                    PageSize = pageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = bets.Sum(s => s.StakeAmount),
                    TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
                    TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
                    TotalCommission = bets.Sum(s => s.Commission),
                    TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
                    TotalMemberCommission = bets.Sum(s => s.MemberCommission),
                    TotalMemberWLC = bets.Sum(s => s.MemberWLC),
                    TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
                    TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
                    TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
                    TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
                    TotalAgentCommission = bets.Sum(s => s.AgentCommission),
                    TotalAgentWLC = bets.Sum(s => s.AgentWLC),
                    TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
                    TotalUplineCommission = bets.Sum(s => s.UplineCommission),
                    TotalUplineWLC = bets.Sum(s => s.UplineWLC),
                    TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
                    Bets = bets.Skip(skip).Take(pageSize).ToList(),
                    TotalMemberCount = totalMemberCount
                }
            };

            #endregion
        }
        public BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetails(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter)
        {
            #region W/ BACK AND LAY

            //var skip = (page - 1) * pageSize;
            //double rowCount = 0;

            //var result = new List<WinLossDetailsModel>();

            //var productTypes = product.Split(',').Select(int.Parse).ToList();
            //string[] currencies = new string[0];

            //if (!string.IsNullOrEmpty(currency))
            //{
            //    currencies = currency.Split(',');
            //}

            //string[] statuses = new string[0];

            //if (!string.IsNullOrEmpty(status))
            //{
            //    statuses = status.Split(',');
            //}

            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };


            //#region BACK AND LAY

            //IQueryable<WinLoss> profitLossDetails;

            //if (dateFilter == "transactiondate")
            //{
            //    profitLossDetails = _context.WinLoss.Where(w =>
            //        w.ParentUserId == parentAgentId &&
            //        w.IndirectUserId == directUserId &&
            //        w.TransactionDateOffset >= fromDate &&
            //        w.TransactionDateOffset < toDate.AddDays(1) &&
            //        currencies.Contains(w.CurrencyCode) &&
            //        productTypes.Contains(w.ProductId)).AsNoTracking();



            //}
            //else
            //{
            //    profitLossDetails = _context.WinLoss.Where(w =>
            //        w.ParentUserId == parentAgentId &&
            //        w.IndirectUserId == directUserId &&
            //        w.SettledDateOffset >= fromDate &&
            //        w.SettledDateOffset < toDate.AddDays(1) &&
            //        currencies.Contains(w.CurrencyCode) &&
            //        productTypes.Contains(w.ProductId)).AsNoTracking();
            //}

            //var backAndLayBetIds = new List<Guid?>();
            //var currentLiabilities = new List<BLCurrentLiabilitiesResponse>();
            //var sheets = new List<BackAndLayWalletResponse>();
            //var totalDetails = profitLossDetails;
            //if (profitLossDetails.Any(s => s.MarketTypeId > 0))
            //{
            //    var betIdFilter = new List<Guid?>();
            //    var backAndLayRejectedBetIds = new List<Guid?>();

            //    sheets = _backAndLaySheetHelper.GetBackAndLaySheet(directUserId).Result;
            //    currentLiabilities = _backAndLaySheetHelper.GetBackAndLayLiabilities(new List<long>(){ directUserId }).Where(s => s.FinalStatus != null).ToList();

            //    backAndLayBetIds = sheets.Select(s => s.BetId).ToList();

            //    var statusFilter = new List<int>();
            //    statusFilter.Add((int)StatusType.Won);
            //    statusFilter.Add((int)StatusType.Lost);
            //    //get the last transaction for each market groupId
            //    var bnlLastBetIds = sheets.Where(w => statusFilter.Contains(w.StatusId)).GroupBy(g => g.MarketGroupId).Select(s => s.First().BetId).ToList();

            //    betIdFilter.AddRange(bnlLastBetIds);

            //    var voidAndRejectedStatus = new int[] { (int)StatusType.Rejected, (int)StatusType.Void };

            //    foreach (var data in sheets.GroupBy(s => s.MarketGroupId))
            //    {
            //        if (data.Any(s => !voidAndRejectedStatus.Contains(s.StatusId))) continue;

            //        backAndLayRejectedBetIds.Add(data.Last().BetId);
            //    }

            //    betIdFilter.AddRange(backAndLayRejectedBetIds);

            //    profitLossDetails = profitLossDetails.Where(c => !backAndLayBetIds.Contains(c.BetId) || betIdFilter.Contains(c.BetId)).AsNoTracking();
            //}

            //rowCount = profitLossDetails.Count();

            //var bets = profitLossDetails.OrderBy(s => s.TransactionDateOffset).Skip(skip).Take(pageSize).ToList();

            //foreach (var bet in bets)
            //{
            //    if (!backAndLayBetIds.Contains(bet.BetId))
            //    {
            //        result.Add(new WinLossDetailsModel()
            //        {
            //            UserId = bet.IndirectUserId,
            //            BetId = string.IsNullOrEmpty(bet.ReferenceId) ? bet.BetId : Guid.Parse(bet.ReferenceId),
            //            TransactionDate = bet.TransactionDate,
            //            SettlementDate = bet.SettledDate,
            //            Username = bet.Username,
            //            UserTypeId = bet.UserTypeId,
            //            CurrencyCode = bet.CurrencyCode,
            //            StakeAmount = bet.Stake / bet.ExchangeRate,
            //            ValidStakeAmount = bet.TrueStake / bet.ExchangeRate,
            //            Commission = bet.Commission / bet.ExchangeRate,
            //            MemberWinLoss = bet.MemberWinLoss / bet.ExchangeRate,
            //            MemberCommission = bet.MemberCommission / bet.ExchangeRate,
            //            MemberWLC = bet.MemberWLC / bet.ExchangeRate,
            //            DownlineWinLoss = bet.DownlineWinLoss / bet.ExchangeRate,
            //            DownlineCommission = bet.DownlineCommission / bet.ExchangeRate,
            //            DownlineWLC = bet.DownlineWLC / bet.ExchangeRate,
            //            AgentWinLoss = bet.AgentWinLoss / bet.ExchangeRate,
            //            AgentCommission = bet.AgentCommission / bet.ExchangeRate,
            //            AgentWLC = bet.AgentWLC / bet.ExchangeRate,
            //            UplineCommission = bet.UplineCommission / bet.ExchangeRate,
            //            UplineWinLoss = bet.UplineWinLoss / bet.ExchangeRate,
            //            UplineWLC = bet.UplineWLC / bet.ExchangeRate,
            //            IsParlay = bet.IsParlay,
            //            Stake = bet.Stake / bet.ExchangeRate,
            //            TrueStake = bet.TrueStake / bet.ExchangeRate,
            //            Odds = bet.Odds,
            //            OddsTypeId = bet.OddsTypeId,
            //            OddsTypeName = bet.OddsTypeName,
            //            StatusId = bet.StatusId,
            //            StatusName = bet.StatusName,
            //            VirtualSportName = bet.ProductId == 2 ? bet.SegmentName : string.Empty,
            //            ProductId = bet.ProductId,
            //            ProductName = bet.ProductName,
            //            LeagueId = bet.LeagueId,
            //            LeagueName = string.IsNullOrEmpty(bet.LeagueName) ? bet.SegmentName : bet.LeagueName,
            //            FixtureId = bet.FixtureId,
            //            FixtureName = bet.FixtureName,
            //            FixtureStartDate = bet.FixtureStartDate,
            //            MarketTemplateId = bet.MarketTemplateId,
            //            MarketName = bet.MarketName,
            //            SelectionTemplateId = bet.SelectionTemplateId,
            //            SelectionName = bet.SelectionName,
            //            Score = bet.Score,
            //            FixtureStatusId = bet.FixtureStatusId,
            //            FixtureStatusName = bet.FixtureStatusName,
            //            Line = MarketLine(bet.Line, bet.MarketTemplateId, bet.SelectionName),
            //            MarketTemplateGroupId = bet.MarketTemplateGroupId,
            //            MarketTemplateGroupName = bet.MarketTemplateGroupName,
            //            PositionTakingDetails = JsonConvert.DeserializeObject<PositionTakingDetails>(bet.PositionTakingDetails),
            //            CommissionDetails = JsonConvert.DeserializeObject<List<CommissionDetails>>(bet.CommissionDetails),
            //            CasinoDetails = GetCasinoDetailForPL(bet.CasinoDetails, bet.ExchangeRate),
            //            Reason = string.IsNullOrEmpty(bet.ReferenceId) ? string.Empty : $"ADJUSTMENT - {bet.StatusName}",
            //            BetScore = (!bet.IsParlay && bet.ProductId == (int)Product.Sportsbook) ? GetScore(1, bet.BetId) : "",
            //            FinalScore = (!bet.IsParlay && bet.ProductId == (int)Product.Sportsbook) ? GetScore(2, bet.BetId) : "",
            //            MarketTypeId = bet.MarketTypeId,
            //            MarketGroupId = bet.MarketGroupId
            //        });
            //    }
            //    else
            //    {
            //        var betIds = sheets.Where(s => s.MarketGroupId == bet.MarketGroupId).Select(s => s.BetId);

            //        var details = _context.WinLoss.Where(w =>
            //                w.ParentUserId == parentAgentId && w.IndirectUserId == directUserId &&
            //                betIds.Contains(w.BetId))
            //            .AsNoTracking();

            //        result.Add(new WinLossDetailsModel()
            //        {
            //            UserId = bet.IndirectUserId,
            //            BetId = string.IsNullOrEmpty(bet.ReferenceId) ? bet.BetId : Guid.Parse(bet.ReferenceId),
            //            TransactionDate = bet.TransactionDate,
            //            SettlementDate = bet.SettledDate,
            //            Username = bet.Username,
            //            UserTypeId = bet.UserTypeId,
            //            CurrencyCode = bet.CurrencyCode,
            //            StakeAmount = GetBackAndLayStake(currentLiabilities, bet.IndirectUserId, bet.MarketGroupId) / bet.ExchangeRate,
            //            ValidStakeAmount = GetBackAndLayStake(currentLiabilities, bet.IndirectUserId, bet.MarketGroupId) / bet.ExchangeRate,
            //            Commission = details.Sum(s => s.Commission / s.ExchangeRate),
            //            MemberWinLoss = details.Sum(s => s.MemberWinLoss / s.ExchangeRate),
            //            MemberCommission = details.Sum(s => s.MemberCommission / s.ExchangeRate),
            //            MemberWLC = details.Sum(s => s.MemberWLC / s.ExchangeRate),
            //            DownlineWinLoss = details.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
            //            DownlineCommission = details.Sum(s => s.DownlineCommission / s.ExchangeRate),
            //            DownlineWLC = details.Sum(s => s.DownlineWLC / s.ExchangeRate),
            //            AgentWinLoss = details.Sum(s => s.AgentWinLoss / s.ExchangeRate),
            //            AgentCommission = details.Sum(s => s.AgentCommission / s.ExchangeRate),
            //            AgentWLC = details.Sum(s => s.AgentWLC / s.ExchangeRate),
            //            UplineCommission = details.Sum(s => s.UplineCommission / s.ExchangeRate),
            //            UplineWinLoss = details.Sum(s => s.UplineWinLoss / s.ExchangeRate),
            //            UplineWLC = details.Sum(s => s.UplineWLC / s.ExchangeRate),
            //            IsParlay = GetBackAndLayIsGrouped(currentLiabilities, bet.IndirectUserId, bet.MarketGroupId),
            //            Stake = GetBackAndLayStake(currentLiabilities, bet.IndirectUserId, bet.MarketGroupId) / bet.ExchangeRate,
            //            TrueStake = GetBackAndLayStake(currentLiabilities, bet.IndirectUserId, bet.MarketGroupId) / bet.ExchangeRate,
            //            Odds = bet.Odds,
            //            OddsTypeId = bet.OddsTypeId,
            //            OddsTypeName = bet.OddsTypeName,
            //            StatusId = bet.StatusId,
            //            StatusName = bet.StatusName,
            //            VirtualSportName = bet.ProductId == 2 ? bet.SegmentName : string.Empty,
            //            ProductId = bet.ProductId,
            //            ProductName = bet.ProductName,
            //            LeagueId = bet.LeagueId,
            //            LeagueName = string.IsNullOrEmpty(bet.LeagueName) ? bet.SegmentName : bet.LeagueName,
            //            FixtureId = bet.FixtureId,
            //            FixtureName = bet.FixtureName,
            //            FixtureStartDate = bet.FixtureStartDate,
            //            MarketTemplateId = bet.MarketTemplateId,
            //            MarketName = bet.MarketName,
            //            SelectionTemplateId = bet.SelectionTemplateId,
            //            SelectionName = bet.SelectionName,
            //            Score = bet.Score,
            //            FixtureStatusId = bet.FixtureStatusId,
            //            FixtureStatusName = bet.FixtureStatusName,
            //            Line = MarketLine(bet.Line, bet.MarketTemplateId, bet.SelectionName),
            //            MarketTemplateGroupId = bet.MarketTemplateGroupId,
            //            MarketTemplateGroupName = bet.MarketTemplateGroupName,
            //            PositionTakingDetails = JsonConvert.DeserializeObject<PositionTakingDetails>(bet.PositionTakingDetails),
            //            CommissionDetails = JsonConvert.DeserializeObject<List<CommissionDetails>>(bet.CommissionDetails),
            //            CasinoDetails = JsonConvert.DeserializeObject<CasinoDetail>(bet.CasinoDetails),
            //            Reason = string.IsNullOrEmpty(bet.ReferenceId) ? string.Empty : $"ADJUSTMENT - {bet.StatusName}",
            //            BetScore = (!bet.IsParlay && bet.ProductId == (int)Product.Sportsbook) ? GetScore(1, bet.BetId) : "",
            //            FinalScore = (!bet.IsParlay && bet.ProductId == (int)Product.Sportsbook) ? GetScore(2, bet.BetId) : "",
            //            MarketTypeId = bet.MarketTypeId,
            //            MarketGroupId = bet.MarketGroupId
            //        });
            //    }
            //}


            //var totalStakeAmount = profitLossDetails.Where(s => !backAndLayBetIds.Contains(s.BetId)).Sum(s => s.Stake / s.ExchangeRate);
            //totalStakeAmount += currentLiabilities.Sum(s => s.CurrentStake * -1 / s.ExchangeRate);

            //var totalValidStakeAmount = profitLossDetails.Where(s => !backAndLayBetIds.Contains(s.BetId)).Sum(s => s.TrueStake / s.ExchangeRate);
            //totalValidStakeAmount += currentLiabilities.Sum(s => s.CurrentStake * -1 / s.ExchangeRate);

            //var totalCommission = totalDetails.Sum(s => s.MemberCommission / s.ExchangeRate);

            //var totalMemberWinLoss = totalDetails.Sum(s => s.MemberWinLoss / s.ExchangeRate);
            //var totalMemberCommission = totalDetails.Sum(s => s.MemberCommission / s.ExchangeRate);
            //var totalMemberWlc = totalDetails.Sum(s => s.MemberWLC / s.ExchangeRate);

            //var totalDownLineWinLoss = totalDetails.Sum(s => s.DownlineWinLoss / s.ExchangeRate);
            //var totalDownLineCommission = totalDetails.Sum(s => s.DownlineCommission / s.ExchangeRate);
            //var totalDownLineWlc = totalDetails.Sum(s => s.DownlineWLC / s.ExchangeRate);

            //var totalAgentWinLoss = totalDetails.Sum(s => s.AgentWinLoss / s.ExchangeRate);
            //var totalAgentCommission = totalDetails.Sum(s => s.AgentWinLoss / s.ExchangeRate);
            //var totalAgentWlc = totalDetails.Sum(s => s.AgentWLC / s.ExchangeRate);

            //var totalUplineWinLoss = totalDetails.Sum(s => s.UplineWinLoss / s.ExchangeRate);
            //var totalUplineCommission = totalDetails.Sum(s => s.UplineCommission / s.ExchangeRate);
            //var totalUplineWlc = totalDetails.Sum(s => s.UplineWLC / s.ExchangeRate);

            //#endregion

            //return new BaseResponseDTO<WinLossDetailsResponse>
            //{
            //    StatusCode = (int)ResultType.Success,
            //    StatusName = ResultType.Success.ToString(),
            //    Result = new WinLossDetailsResponse
            //    {
            //        CurrentPage = page,
            //        PageCount = (int)Math.Ceiling(rowCount / pageSize),
            //        PageSize = pageSize,
            //        RowCount = (int)rowCount,
            //        TotalStakeAmount = totalStakeAmount,
            //        TotalValidStakeAmount = totalValidStakeAmount,
            //        TotalCommission = totalCommission,
            //        TotalMemberWinLoss = totalMemberWinLoss,
            //        TotalMemberCommission = totalMemberCommission,
            //        TotalMemberWLC = totalMemberWlc,
            //        TotalDownlineWinLoss = totalDownLineWinLoss,
            //        TotalDownlineCommission = totalDownLineCommission,
            //        TotalDownlineWLC = totalDownLineWlc,
            //        TotalAgentWinLoss = totalAgentWinLoss,
            //        TotalAgentCommission = totalAgentCommission,
            //        TotalAgentWLC = totalAgentWlc,
            //        TotalUplineWinLoss = totalUplineWinLoss,
            //        TotalUplineCommission = totalUplineCommission,
            //        TotalUplineWLC = totalUplineWlc,
            //        Bets = result
            //    }
            //};

            #endregion

            #region W/O BACK AND LAY

            var skip = (page - 1) * pageSize;
            var productTypes = product.Split(',').Select(int.Parse).ToList();
            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            IQueryable<WinLoss> profitLossDetails;
            string[] currencies = new string[0];

            if (!string.IsNullOrEmpty(currency))
            {
                currencies = currency.Split(',');
            }

            if (dateFilter == "transactiondate")
            {
                profitLossDetails = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentAgentId &&
                    w.IndirectUserId == directUserId &&
                    w.TransactionDateOffset >= fromDate &&
                    w.TransactionDateOffset < toDate.AddDays(1) &&
                    currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }
            else
            {
                profitLossDetails = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentAgentId &&
                    w.IndirectUserId == directUserId &&
                    w.SettledDateOffset >= fromDate &&
                    w.SettledDateOffset < toDate.AddDays(1) &&
                    currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId))).AsNoTracking();
            }
            else
            {
                profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId)).AsNoTracking();
            }

            var betsRaw = profitLossDetails.Select(ss => new
            {
                ss.SettledDate,
                ss.IndirectUserId,
                ss.ReferenceId,
                ss.BetId,
                ss.TrueStake,
                ss.TransactionDate,
                ss.Username,
                ss.UserTypeId,
                ss.CurrencyCode,
                ss.Stake,
                ss.Commission,
                ss.MemberCommission,
                ss.MemberWinLoss,
                ss.MemberWLC,
                ss.DownlineCommission,
                ss.DownlineWLC,
                ss.DownlineWinLoss,
                ss.AgentWinLoss,
                ss.AgentCommission,
                ss.AgentWLC,
                ss.UplineCommission,
                ss.UplineWinLoss,
                ss.UplineWLC,
                ss.IsParlay,
                ss.Odds,
                ss.OddsTypeId,
                ss.OddsTypeName,
                ss.StatusId,
                ss.StatusName,
                ss.ProductId,
                ss.SegmentName,
                ss.ProductName,
                ss.LeagueId,
                ss.LeagueName,
                ss.FixtureId,
                ss.FixtureName,
                ss.FixtureStartDate,
                ss.MarketTemplateId,
                ss.MarketName,
                ss.SelectionTemplateId,
                ss.SelectionName,
                ss.Score,
                ss.FixtureStatusId,
                ss.FixtureStatusName,
                ss.Line,
                ss.MarketTemplateGroupId,
                ss.MarketTemplateGroupName,
                ss.PositionTakingDetails,
                ss.CommissionDetails,
                ss.CasinoDetails,
                ss.MarketTypeId,
                ss.MarketGroupId,
                ss.ExchangeRate
            }).ToList();

            double rowCount = betsRaw.Count();

            if (rowCount == 0)
            {
                return new BaseResponseDTO<WinLossDetailsResponse>()
                {
                    Result = new WinLossDetailsResponse()
                    {
                        Bets = new List<WinLossDetailsModel>(),
                        CurrentPage = pageSize,
                        PageCount = 0,
                        PageSize = pageSize
                    },
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString()
                };
            }

            var totalStakeAmount = betsRaw.Sum(s => s.Stake / s.ExchangeRate);
            var totalValidStakeAmount = betsRaw.Sum(s => s.TrueStake / s.ExchangeRate);

            var bets = betsRaw.OrderBy(p => p.SettledDate).Skip(skip).Take(pageSize).ToList().Select(s => new WinLossDetailsModel()
            {
                UserId = s.IndirectUserId,
                BetId = string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId),
                TransactionDate = s.TransactionDate,
                SettlementDate = s.SettledDate,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                StakeAmount = s.Stake / s.ExchangeRate,
                ValidStakeAmount = s.TrueStake / s.ExchangeRate,
                Commission = s.Commission / s.ExchangeRate,
                MemberWinLoss = s.MemberWinLoss / s.ExchangeRate,
                MemberCommission = s.MemberCommission / s.ExchangeRate,
                MemberWLC = s.MemberWLC / s.ExchangeRate,
                DownlineWinLoss = s.DownlineWinLoss / s.ExchangeRate,
                DownlineCommission = s.DownlineCommission / s.ExchangeRate,
                DownlineWLC = s.DownlineWLC / s.ExchangeRate,
                AgentWinLoss = s.AgentWinLoss / s.ExchangeRate,
                AgentCommission = s.AgentCommission / s.ExchangeRate,
                AgentWLC = s.AgentWLC / s.ExchangeRate,
                UplineCommission = s.UplineCommission / s.ExchangeRate,
                UplineWinLoss = s.UplineWinLoss / s.ExchangeRate,
                UplineWLC = s.UplineWLC / s.ExchangeRate,
                IsParlay = s.IsParlay,
                Stake = s.Stake / s.ExchangeRate,
                TrueStake = s.TrueStake / s.ExchangeRate,
                Odds = s.Odds,
                OddsTypeId = s.OddsTypeId,
                OddsTypeName = s.OddsTypeName,
                StatusId = s.StatusId,
                StatusName = s.StatusName,
                VirtualSportName = s.ProductId == 2 ? s.SegmentName : string.Empty,
                ProductId = s.ProductId,
                ProductName = s.ProductName,
                LeagueId = s.LeagueId,
                LeagueName = string.IsNullOrEmpty(s.LeagueName) ? s.SegmentName : s.LeagueName,
                FixtureId = s.FixtureId,
                FixtureName = s.FixtureName,
                FixtureStartDate = s.FixtureStartDate,
                MarketTemplateId = s.MarketTemplateId,
                MarketName = s.MarketName,
                SelectionTemplateId = s.SelectionTemplateId,
                SelectionName = s.SelectionName,
                Score = s.Score,
                FixtureStatusId = s.FixtureStatusId,
                FixtureStatusName = s.FixtureStatusName,
                Line = s.Line,
                MarketTemplateGroupId = s.MarketTemplateGroupId,
                MarketTemplateGroupName = s.MarketTemplateGroupName,
                PositionTakingDetails = JsonConvert.DeserializeObject<PositionTakingDetails>(s.PositionTakingDetails),
                CommissionDetails = JsonConvert.DeserializeObject<List<CommissionDetails>>(s.CommissionDetails),
                CasinoDetails = GetCasinoDetailForPL(s.CasinoDetails, s.ExchangeRate),
                Reason = string.IsNullOrEmpty(s.ReferenceId) ? string.Empty : $"ADJUSTMENT - {s.StatusName}",
                BetScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(1, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                FinalScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(2, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                HalftimeScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(3, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                MarketTypeId = s.MarketTypeId,
                MarketGroupId = s.MarketGroupId
            }).ToList();

            return new BaseResponseDTO<WinLossDetailsResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossDetailsResponse
                {
                    CurrentPage = page,
                    PageCount = (int)Math.Ceiling(rowCount / pageSize),
                    PageSize = pageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = totalStakeAmount,
                    TotalValidStakeAmount = totalValidStakeAmount,
                    TotalCommission = betsRaw.Sum(s => s.Commission / s.ExchangeRate),
                    TotalMemberWinLoss = betsRaw.Sum(s => s.MemberWinLoss / s.ExchangeRate),
                    TotalMemberCommission = betsRaw.Sum(s => s.MemberCommission / s.ExchangeRate),
                    TotalMemberWLC = betsRaw.Sum(s => s.MemberWLC / s.ExchangeRate),
                    TotalDownlineWinLoss = betsRaw.Sum(s => s.DownlineWinLoss / s.ExchangeRate),
                    TotalDownlineCommission = betsRaw.Sum(s => s.DownlineCommission / s.ExchangeRate),
                    TotalDownlineWLC = betsRaw.Sum(s => s.DownlineWLC / s.ExchangeRate),
                    TotalAgentWinLoss = betsRaw.Sum(s => s.AgentWinLoss / s.ExchangeRate),
                    TotalAgentCommission = betsRaw.Sum(s => s.AgentCommission / s.ExchangeRate),
                    TotalAgentWLC = betsRaw.Sum(s => s.AgentWLC / s.ExchangeRate),
                    TotalUplineWinLoss = betsRaw.Sum(s => s.UplineWinLoss / s.ExchangeRate),
                    TotalUplineCommission = betsRaw.Sum(s => s.UplineCommission / s.ExchangeRate),
                    TotalUplineWLC = betsRaw.Sum(s => s.UplineWLC / s.ExchangeRate),
                    Bets = bets.OrderBy(s => s.TransactionDate).ToList()
                }
            };

            #endregion
        }
        public BaseResponseDTO<GGRResponse> GetGGRV2(long parentAgentId, long directUserId, WinLossAccountRequestExtended request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            double rowCount = 0;

            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost, (int)StatusType.Void };

            if (request.Currency == Currency.All && parentAgentId > 0)
                request.Currency = Enum.Parse<Currency>(GetUserDetails(parentAgentId).Result.Currency);

            var iQueryableData = GetWinLossAggregateQueryableParentAndAgentId2(parentAgentId, directUserId, true, request).AsNoTracking();
            var membersCount = GetDistinctMembers2(request);

            var data = iQueryableData.Select(s => new
            {
                IndirectUserId = s.IndirectUserId,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                BetId = s.BetId,
                ReferenceId = s.ReferenceId,
                TrueStake = s.TrueStake,
                Payout = s.Payout.Value,
                GGR = s.GGR.Value,
                ExchangeRate = s.ExchangeRate,
                StatusId = s.StatusId,
                SettledDate = s.SettledDate
            }).ToList();

            var excludestates = new int[] { (int)StatusType.Rejected, (int)StatusType.Cancelled };
            var settledBets = data.Where(w => string.IsNullOrEmpty(w.ReferenceId) && !excludestates.Contains(w.StatusId)).ToList();
            var reSettledBets = data.Where(w => !string.IsNullOrEmpty(w.ReferenceId)).ToList();
            if (reSettledBets.Count() > 0)
            {
                var resettledIds = reSettledBets.Select(r => Guid.Parse(r.ReferenceId)).ToList();
                var origResettledBets = settledBets.Where(s => resettledIds.Contains(s.BetId)).ToList();
                settledBets = settledBets.Where(s => !resettledIds.Contains(s.BetId)).ToList();
                //settledBets = settledBets.Where(r => states.Contains(r.StatusId)).ToList();
                settledBets.AddRange(origResettledBets);
            }

            var betData = settledBets.ToList();

            betData.AddRange(reSettledBets);

            var bets = betData.GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).Select(d => new GGRModelResponse()
            {
                UserId = d.Key.IndirectUserId,
                Username = d.Key.Username,
                IsAgent = d.FirstOrDefault().UserTypeId == (int)UserType.Agent ? true : false,
                CurrencyCode = d.Key.CurrencyCode,
                StakeCount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).ToList().Distinct().Count(),
                StakeAmount = d.Sum(x => x.TrueStake),
                Payout = d.Sum(s => s.Payout),
                GGR = d.Sum(ss => ss.GGR),
                MemberCount = d.First().UserTypeId == (int)UserType.Player ? 1 : membersCount.First(m => m.Item1 == d.Key.IndirectUserId).Item2
            });

            rowCount = bets.Count();
            var totalMemberCount = bets.Select(b => b.MemberCount).Sum();

            return new BaseResponseDTO<GGRResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new GGRResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = bets.Sum(s => s.StakeAmount),
                    TotalStakeCount = bets.Sum(s => s.StakeCount),
                    TotalPayout = bets.Sum(s => s.Payout),
                    TotalGGR = bets.Sum(s => s.GGR),
                    Bets = bets.Skip(skip).Take(request.PageSize).ToList(),
                    TotalMemberCount = totalMemberCount
                }
            };
        }
        public BaseResponseDTO<GGRDetailsResponse> GetGGRDetailsV2(long agentId, long playerId, WinLossAccountRequestExtended request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            var productTypes = request.Product.Split(',').Select(int.Parse).ToList();
            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            IQueryable<WinLoss> profitLossDetails;
            var currencies = new List<string>() { request.Currency.ToString() };
            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost, (int)StatusType.Void };

            var accounts = new List<string>();
            if (!string.IsNullOrEmpty(request.Accounts))
            {
                accounts = request.Accounts.Split(',').ToList();
            }

            var betIds = new List<string>();
            if (!string.IsNullOrEmpty(request.BetIds))
            {
                betIds = request.BetIds.Split(',').ToList();
            }

            var status = new List<string>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                status = request.Status.Split(',').Select(s => s.ToUpper().Replace("-", "")).ToList();
            }

            if (request.DateFilter == "transactiondate")
            {
                profitLossDetails = _context.WinLoss.Where(w =>
                    w.ParentUserId == agentId &&
                    w.IndirectUserId == playerId &&
                    w.TransactionDateOffset >= request.FromDate &&
                    w.TransactionDateOffset < request.ToDate.AddDays(1)).AsNoTracking();
            }
            else
            {
                profitLossDetails = _context.WinLoss.Where(w =>
                    w.ParentUserId == agentId &&
                    w.IndirectUserId == playerId &&
                    w.SettledDateOffset >= request.FromDate &&
                    w.SettledDateOffset < request.ToDate.AddDays(1)).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            if (accounts.Any())
            {
                profitLossDetails = profitLossDetails.Where(r => accounts.Contains(r.Username));
            }

            if (status.Any())
            {
                profitLossDetails = profitLossDetails.Where(p => status.Any(s => s.Equals((string.IsNullOrEmpty(p.ReferenceId) || p.StatusName.ToUpper() == "CANCELLED") ? p.StatusName.ToUpper() : $"ADJUSTMENT{p.StatusName.ToUpper()}")));
            }

            if (betIds.Any())
            {
                //profitLossDetails = profitLossDetails.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));                
                profitLossDetails = profitLossDetails.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
            }

            double rowCount = profitLossDetails.AsNoTracking().Count();

            if (rowCount == 0)
            {
                return new BaseResponseDTO<GGRDetailsResponse>()
                {
                    Result = new GGRDetailsResponse()
                    {
                        Bets = new List<GGRDetailsModelResponse>(),
                        CurrentPage = request.PageSize,
                        PageCount = 0,
                        PageSize = request.PageSize,
                        RowCount = (int)rowCount,
                        TotalStakeAmount = 0,
                        TotalStakeCount = 0,
                        TotalPayout = 0,
                        TotalGGR = 0
                    },
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString()
                };
            }

            var data = profitLossDetails.Select(s => new
            {
                IndirectUserId = s.IndirectUserId,
                BetId = s.BetId,
                ReferenceId = s.ReferenceId,
                TransactionDate = s.TransactionDate,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                IsParlay = s.IsParlay,
                Odds = s.Odds,
                OddsTypeId = s.OddsTypeId,
                OddsTypeName = s.OddsTypeName,
                StatusId = s.StatusId,
                StatusName = s.StatusName,
                ProductId = s.ProductId,
                ProductName = s.ProductName,
                LeagueId = s.LeagueId,
                FixtureId = s.FixtureId,
                FixtureName = s.FixtureName,
                FixtureStartDate = s.FixtureStartDate,
                MarketTemplateId = s.MarketTemplateId,
                MarketName = s.MarketName,
                SelectionTemplateId = s.SelectionTemplateId,
                SelectionName = s.SelectionName,
                Score = s.Score,
                FixtureStatusId = s.FixtureStatusId,
                FixtureStatusName = s.FixtureStatusName,
                MarketTemplateGroupId = s.MarketTemplateGroupId,
                MarketTemplateGroupName = s.MarketTemplateGroupName,
                MarketTypeId = s.MarketTypeId,
                Payout = s.Payout.Value,
                GGR = s.GGR.Value,
                SettledDate = s.SettledDate,
                ExchangeRate = s.ExchangeRate,
                TrueStake = s.TrueStake,
                SegmentName = s.SegmentName,
                LeagueName = s.LeagueName,
                Line = s.Line,
                CasinoDetails = s.CasinoDetails
            }).ToList();

            var excludestates = new int[] { (int)StatusType.Rejected, (int)StatusType.Cancelled };
            var settledBets = data.Where(w => string.IsNullOrEmpty(w.ReferenceId) && !excludestates.Contains(w.StatusId)).ToList();
            var reSettledBets = data.Where(w => !string.IsNullOrEmpty(w.ReferenceId)).ToList();
            if (reSettledBets.Count() > 0)
            {
                var resettledIds = reSettledBets.Select(r => Guid.Parse(r.ReferenceId)).ToList();
                var origResettledBets = settledBets.Where(s => resettledIds.Contains(s.BetId)).ToList();
                settledBets = settledBets.Where(s => !resettledIds.Contains(s.BetId)).ToList();
                //settledBets = settledBets.Where(r => states.Contains(r.StatusId)).ToList();
                settledBets.AddRange(origResettledBets);
            }

            var betData = settledBets.ToList();

            betData.AddRange(reSettledBets);

            betData = betData.OrderBy(b => b.SettledDate).ToList();

            var totalStakeAmount = betData.Sum(s => s.TrueStake);
            var totalPayout = betData.Sum(s => s.Payout);
            var totalGGR = betData.Sum(s => s.GGR);

            var result = betData.OrderBy(b => b.SettledDate).Skip(skip).Take(request.PageSize).Select(s => new GGRDetailsModelResponse()
            {
                UserId = s.IndirectUserId,
                BetId = string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId),
                TransactionDate = s.TransactionDate,
                SettlementDate = s.SettledDate,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                StakeAmount = s.TrueStake,
                IsParlay = s.IsParlay,
                Odds = s.Odds,
                OddsTypeId = s.OddsTypeId,
                OddsTypeName = s.OddsTypeName,
                StatusId = s.StatusId,
                StatusName = (string.IsNullOrEmpty(s.ReferenceId) || s.StatusName.ToUpper() == "CANCELLED") ? s.StatusName : $"Adjustment-{s.StatusName}",
                VirtualSportName = s.ProductId == 2 ? s.SegmentName : string.Empty,
                ProductId = s.ProductId,
                ProductName = s.ProductName,
                LeagueId = s.LeagueId,
                LeagueName = string.IsNullOrEmpty(s.LeagueName) ? s.SegmentName : s.LeagueName,
                FixtureId = s.FixtureId,
                FixtureName = s.FixtureName,
                FixtureStartDate = s.FixtureStartDate,
                MarketTemplateId = s.MarketTemplateId,
                MarketName = s.MarketName,
                SelectionTemplateId = s.SelectionTemplateId,
                SelectionName = s.SelectionName,
                Score = s.Score,
                FixtureStatusId = s.FixtureStatusId,
                FixtureStatusName = s.FixtureStatusName,
                Line = s.Line,
                MarketTemplateGroupId = s.MarketTemplateGroupId,
                MarketTemplateGroupName = s.MarketTemplateGroupName,
                CasinoDetails = JsonConvert.DeserializeObject<CasinoDetail>(s.CasinoDetails),
                Reason = string.IsNullOrEmpty(s.ReferenceId) ? string.Empty : $"ADJUSTMENT - {s.StatusName}",
                BetScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(1, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                FinalScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(2, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                HalftimeScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(3, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
                MarketTypeId = s.MarketTypeId,
                Payout = s.Payout,
                GGR = s.GGR
            }).ToList();

            return new BaseResponseDTO<GGRDetailsResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new GGRDetailsResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    TotalStakeAmount = totalStakeAmount,
                    TotalStakeCount = (int)rowCount,
                    TotalPayout = totalPayout,
                    TotalGGR = totalGGR,
                    Bets = result
                }
            };
        }
        #endregion

        #region Utilities

        private IQueryable<WinLoss> GetProfitLossAggregateQueryable(long parentUserId, DateTime fromDate, DateTime toDate, string[] currencies, string dateFilter, List<int> productTypes)
        {
            IQueryable<WinLoss> result;
            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (dateFilter == "transactiondate")
            {
                result = _context.WinLoss.Where(w =>
                   w.ParentUserId == parentUserId &&
                   w.DirectUserId == parentUserId &&
                   w.TransactionDateOffset >= fromDate.Date &&
                   w.TransactionDateOffset < toDate.AddDays(1).Date &&
                   currencies.Contains(w.CurrencyCode) &&
                   w.UserTypeId == (int)UserType.Agent).AsNoTracking();
            }

            else
            {
                result = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentUserId &&
                    w.DirectUserId == parentUserId &&
                    w.SettledDateOffset >= fromDate.Date &&
                    w.SettledDateOffset < toDate.AddDays(1).Date &&
                    currencies.Contains(w.CurrencyCode) &&
                    w.UserTypeId == (int)UserType.Agent).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            return result;
        }
        private IQueryable<WinLoss> GetProfitLossAggregateQueryable2(long parentUserId, ProfitLossRequest request)
        {

            var productTypes = request.Product.Split(',').Select(int.Parse).ToList();
            string[] currencies = new string[0];

            if (!string.IsNullOrEmpty(request.Currency))
            {
                currencies = request.Currency.Split(',');
            }

            var accounts = new List<string>();
            if (!string.IsNullOrEmpty(request.Accounts))
            {
                accounts = request.Accounts.Split(',').ToList();
            }

            var betIds = new List<string>();
            if (!string.IsNullOrEmpty(request.BetIds))
            {
                betIds = request.BetIds.Split(',').ToList();
            }

            var status = new List<string>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                status = request.Status.Split(',').Select(s => s.ToUpper().Replace("-", "")).ToList();
            }

            IQueryable<WinLoss> result;
            IQueryable<Tuple<long, string, string>> accountsResult;

            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (request.DateFilter == "transactiondate")
            {
                result = _context.WinLoss.Where(w =>
                   w.ParentUserId == parentUserId &&
                   w.DirectUserId == parentUserId &&
                   w.TransactionDateOffset >= request.FromDate.Date &&
                   w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                   currencies.Contains(w.CurrencyCode) &&
                   w.UserTypeId == (int)UserType.Agent).AsNoTracking();
            }

            else
            {
                result = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentUserId &&
                    w.DirectUserId == parentUserId &&
                    w.SettledDateOffset >= request.FromDate.Date &&
                    w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                    currencies.Contains(w.CurrencyCode) &&
                    w.UserTypeId == (int)UserType.Agent).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            if (accounts.Any())
            {
                if (request.DateFilter == "transactiondate")
                {
                    accountsResult = _context.WinLoss.AsNoTracking().Where(w =>
                           w.TransactionDateOffset >= request.FromDate.Date &&
                           w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                           currencies.Contains(w.CurrencyCode)).AsNoTracking()
                            .Select(s => new Tuple<long, string, string>(s.IndirectUserId, s.Username, s.BetId.ToString())).Distinct();
                }
                else
                {
                    accountsResult = _context.WinLoss.AsNoTracking().Where(w =>
                           w.SettledDateOffset >= request.FromDate.Date &&
                           w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                           currencies.Contains(w.CurrencyCode)).AsNoTracking()
                            .Select(s => new Tuple<long, string, string>(s.IndirectUserId, s.Username, s.BetId.ToString())).Distinct();
                }

                var indirectUserIds = new List<long>();
                foreach (var account in accounts)
                {
                    //var indirectUserId = accountsResult.Where(a => account == a.Item2).FirstOrDefault();
                    //if (indirectUserId != null)
                    //{
                    //    indirectUserIds.AddRange(accountsResult.Where(a => a.Item1 <= indirectUserId.Item1 && a.Item3 == indirectUserId.Item3).Select(s => s.Item1).Distinct().ToList());
                    //}
                    var indirectUserId = accountsResult.Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();

                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }

                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                result = result.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
            }

            if (status.Any())
            {
                result = result.Where(p => status.Any(s => s.Equals((string.IsNullOrEmpty(p.ReferenceId) || p.StatusName.ToUpper() == "CANCELLED") ? p.StatusName.ToUpper() : $"ADJUSTMENT{p.StatusName.ToUpper()}")));
            }

            return result;
        }
        private List<Tuple<long, int>> GetDistinctMembers(DateTime fromDate, DateTime toDate, string[] currencies, string dateFilter, List<int> productTypes)
        {
            var result = new List<Tuple<long, int>>();
            IQueryable<WinLoss> winLossRaw;
            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (dateFilter == "transactiondate")
            {
                winLossRaw = _context.WinLoss.Where(w =>
                   w.TransactionDateOffset >= fromDate.Date &&
                   w.TransactionDateOffset < toDate.AddDays(1).Date &&
                   (!currencies.Any() || (currencies.Any() && currencies.Contains(w.CurrencyCode))) &&
                  w.UserTypeId == (int)UserType.Player).AsNoTracking();
            }

            else
            {
                winLossRaw = _context.WinLoss.Where(w =>
                    w.SettledDateOffset >= fromDate.Date &&
                    w.SettledDateOffset < toDate.AddDays(1).Date &&
                    (!currencies.Any() || (currencies.Any() && currencies.Contains(w.CurrencyCode))) &&
                   w.UserTypeId == (int)UserType.Player).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                winLossRaw = winLossRaw.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                winLossRaw = winLossRaw.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            result = winLossRaw.Select(w => new { w.ParentUserId, w.IndirectUserId }).ToList()
               .GroupBy(g => g.ParentUserId).Select(d => new Tuple<long, int>(d.Key, d.Select(dd => dd.IndirectUserId).Distinct().Count())).ToList();

            return result;
        }
        private List<Tuple<long, int>> GetDistinctMembers2(WinLossAccountRequest request)
        {
            var result = new List<Tuple<long, int>>();
            IQueryable<WinLoss> winLossRaw;

            var productTypes = new List<int>();
            if (!string.IsNullOrEmpty(request.Product))
            {
                productTypes = request.Product.Split(',').Select(int.Parse).ToList();
            }

            var betIds = new List<string>();
            if (!string.IsNullOrEmpty(request.BetIds))
            {
                betIds = request.BetIds.Split(',').ToList();
            }

            var playerNames = new List<string>();
            if (!string.IsNullOrEmpty(request.Accounts))
            {
                playerNames = request.Accounts.Split(',').ToList();
            }

            var currencies = new List<string>() { request.Currency.ToString() };

            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (request.DateFilter == "transactiondate")
            {
                winLossRaw = _context.WinLoss.Where(w =>
                   w.TransactionDateOffset >= request.FromDate.Date &&
                   w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                   currencies.Contains(w.CurrencyCode) &&
                   w.UserTypeId == (int)UserType.Player).AsNoTracking();
            }
            else
            {
                winLossRaw = _context.WinLoss.Where(w =>
                       w.SettledDateOffset >= request.FromDate.Date &&
                       w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                       currencies.Contains(w.CurrencyCode) &&
                       w.UserTypeId == (int)UserType.Player).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                winLossRaw = winLossRaw.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                winLossRaw = winLossRaw.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            if (betIds.Any())
            {
                //winLossRaw = winLossRaw.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                winLossRaw = winLossRaw.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
            }

            if (playerNames.Any())
            {
                foreach (var player in playerNames)
                {
                    if (winLossRaw.Any(p => p.Username == player))
                        winLossRaw = winLossRaw.Where(p => p.Username == player);
                }
            }


            result = winLossRaw.Select(w => new { w.ParentUserId, w.IndirectUserId }).ToList()
                .GroupBy(g => g.ParentUserId).Select(d => new Tuple<long, int>(d.Key, d.Select(dd => dd.IndirectUserId).Distinct().Count())).ToList();

            return result;
        }
        private List<Tuple<long, int>> GetDistinctMembers2(ProfitLossRequest request)
        {
            var productTypes = request.Product.Split(',').Select(int.Parse).ToList();
            string[] currencies = new string[0];

            if (!string.IsNullOrEmpty(request.Currency))
            {
                currencies = request.Currency.Split(',');
            }

            var betIds = new List<string>();
            if (!string.IsNullOrEmpty(request.BetIds))
            {
                betIds = request.BetIds.Split(',').ToList();
            }

            var playerNames = new List<string>();
            if (!string.IsNullOrEmpty(request.Accounts))
            {
                playerNames = request.Accounts.Split(',').ToList();
            }

            var result = new List<Tuple<long, int>>();
            IQueryable<WinLoss> winLossRaw;
            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (request.DateFilter == "transactiondate")
            {
                winLossRaw = _context.WinLoss.Where(w =>
                   w.TransactionDateOffset >= request.FromDate.Date &&
                   w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                   (!currencies.Any() || (currencies.Any() && currencies.Contains(w.CurrencyCode))) &&
                  w.UserTypeId == (int)UserType.Player).AsNoTracking();
            }
            else
            {
                winLossRaw = _context.WinLoss.Where(w =>
                    w.SettledDateOffset >= request.FromDate.Date &&
                    w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                    (!currencies.Any() || (currencies.Any() && currencies.Contains(w.CurrencyCode))) &&
                   w.UserTypeId == (int)UserType.Player).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                winLossRaw = winLossRaw.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                winLossRaw = winLossRaw.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            if (betIds.Any())
            {
                //winLossRaw = winLossRaw.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                winLossRaw = winLossRaw.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
            }

            if (playerNames.Any())
            {
                foreach (var player in playerNames)
                {
                    if (winLossRaw.Any(p => p.Username == player))
                        winLossRaw = winLossRaw.Where(p => p.Username == player);
                }
            }


            result = winLossRaw.Select(w => new { w.ParentUserId, w.IndirectUserId }).ToList()
               .GroupBy(g => g.ParentUserId).Select(d => new Tuple<long, int>(d.Key, d.Select(dd => dd.IndirectUserId).Distinct().Count())).ToList();

            return result;
        }
        private IQueryable<WinLoss> GetProfitLossAggregateQueryableParentUserIdAndDirectUserId(long parentUserId, long directUserId, DateTime fromDate, DateTime toDate, string[] currencies, string dateFilter, List<int> productTypes)
        {
            IQueryable<WinLoss> result;
            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (dateFilter == "transactiondate")
            {
                result = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentUserId &&
                    w.DirectUserId == directUserId &&
                    w.TransactionDateOffset >= fromDate.Date &&
                    w.TransactionDateOffset < toDate.AddDays(1).Date &&
                    currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }
            else
            {
                result = _context.WinLoss.Where(w =>
                     w.ParentUserId == parentUserId &&
                     w.DirectUserId == directUserId &&
                     w.SettledDateOffset >= fromDate.Date &&
                     w.SettledDateOffset < toDate.AddDays(1).Date &&
                     currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            return result;
        }
        private IQueryable<WinLoss> GetProfitLossAggregateQueryableParentUserIdAndDirectUserId2(long parentUserId, long directUserId, ProfitLossRequest request)
        {
            var productTypes = request.Product.Split(',').Select(int.Parse).ToList();
            string[] currencies = new string[0];

            if (!string.IsNullOrEmpty(request.Currency))
            {
                currencies = request.Currency.Split(',');
            }

            var accounts = new List<string>();
            if (!string.IsNullOrEmpty(request.Accounts))
            {
                accounts = request.Accounts.Split(',').ToList();
            }

            var betIds = new List<string>();
            if (!string.IsNullOrEmpty(request.BetIds))
            {
                betIds = request.BetIds.Split(',').ToList();
            }

            var status = new List<string>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                status = request.Status.Split(',').Select(s => s.ToUpper().Replace("-", "")).ToList();
            }

            IQueryable<WinLoss> result;
            IQueryable<Tuple<long, string, string>> accountsResult;

            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (request.DateFilter == "transactiondate")
            {
                result = _context.WinLoss.Where(w =>
                    w.ParentUserId == parentUserId &&
                    w.DirectUserId == directUserId &&
                    w.TransactionDateOffset >= request.FromDate.Date &&
                    w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                    currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }
            else
            {
                result = _context.WinLoss.Where(w =>
                     w.ParentUserId == parentUserId &&
                     w.DirectUserId == directUserId &&
                     w.SettledDateOffset >= request.FromDate.Date &&
                     w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                     currencies.Contains(w.CurrencyCode)).AsNoTracking();
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            if (accounts.Any())
            {
                if (request.DateFilter == "transactiondate")
                {
                    accountsResult = _context.WinLoss.AsNoTracking().Where(w =>
                           w.TransactionDateOffset >= request.FromDate.Date &&
                           w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                           currencies.Contains(w.CurrencyCode)).AsNoTracking()
                            .Select(s => new Tuple<long, string, string>(s.IndirectUserId, s.Username, s.BetId.ToString())).Distinct();
                }
                else
                {
                    accountsResult = _context.WinLoss.AsNoTracking().Where(w =>
                           w.SettledDateOffset >= request.FromDate.Date &&
                           w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                           currencies.Contains(w.CurrencyCode)).AsNoTracking()
                            .Select(s => new Tuple<long, string, string>(s.IndirectUserId, s.Username, s.BetId.ToString())).Distinct();
                }

                var indirectUserIds = new List<long>();
                foreach (var account in accounts)
                {
                    //var indirectUserId = accountsResult.Where(a => account == a.Item2).FirstOrDefault();
                    //if (indirectUserId != null)
                    //{
                    //    indirectUserIds.AddRange(accountsResult.Where(a => a.Item1 <= indirectUserId.Item1 && a.Item3 == indirectUserId.Item3).Select(s => s.Item1).Distinct().ToList());
                    //}

                    var indirectUserId = accountsResult.Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();

                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }

                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                result = result.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
            }

            if (status.Any())
            {
                result = result.Where(p => status.Any(s => s.Equals((string.IsNullOrEmpty(p.ReferenceId) || p.StatusName.ToUpper() == "CANCELLED") ? p.StatusName.ToUpper() : $"ADJUSTMENT{p.StatusName.ToUpper()}")));
            }

            return result;
        }
        private CasinoDetail GetCasinoDetailForPL(string details, decimal exchangeRate)
        {
            if (string.IsNullOrEmpty(details))
                return null;

            var obj = JsonConvert.DeserializeObject<CasinoDetail>(details);
            obj.Payout = obj.Payout / exchangeRate;

            return obj;

        }
        private string GetScore(int scope, Guid betId)
        {
            var score = "0 - 0";
            var betSelection = _context.BetSelection.AsNoTracking().FirstOrDefault(x => x.BetId == betId);

            if (betSelection != null && scope == 1)
                return betSelection.FirstScore;

            if (betSelection != null && scope == 2)
                return betSelection.SecondScore;

            if (betSelection != null && scope == 3)
                return betSelection.HalftimeScore;

            return score;
        }
        private BaseResponseDTO<UserDetailsResponse> GetUserDetails(long userId)
        {
            var client = _httpClient.CreateClient();
            var response = client.GetAsync($"{_config["AgentUrl"]}/user/details/{userId}").Result;

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<BaseResponseDTO<UserDetailsResponse>>(response.Content
                    .ReadAsStringAsync().Result);
            }

            return new BaseResponseDTO<UserDetailsResponse>();
        }
        private IQueryable<WinLoss> GetWinLossAggregateQueryableParentAndAgentId2(long parentUserId, long directUserId, bool isAgent, WinLossAccountRequestExtended request)
        {
            IQueryable<WinLoss> result;
            IQueryable<Tuple<long, string, string>> accountsResult;

            var productTypes = request.Product.Split(',').Select(int.Parse).ToList();

            var accounts = new List<string>();
            if (!string.IsNullOrEmpty(request.Accounts))
            {
                accounts = request.Accounts.Split(',').ToList();
            }

            var betIds = new List<string>();
            if (!string.IsNullOrEmpty(request.BetIds))
            {
                betIds = request.BetIds.Split(',').ToList();
            }

            var status = new List<string>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                status = request.Status.Split(',').Select(s => s.ToUpper().Replace("-", "")).ToList();
            }

            var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
            var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
            var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();
            var sportsProductFilter = productTypes.Where(p => p < 300).ToList();

            if (request.DateFilter == "transactiondate")
            {
                if (isAgent)
                {
                    result = _context.WinLoss.Where(w =>
                       w.ParentUserId == parentUserId &&
                       w.DirectUserId == directUserId &&
                       w.TransactionDateOffset >= request.FromDate.Date &&
                       w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                       w.CurrencyCode == request.Currency.ToString()).AsNoTracking();
                }
                else
                {
                    result = _context.WinLoss.Where(w =>
                         w.ParentUserId == parentUserId &&
                         w.DirectUserId == directUserId &&
                         w.TransactionDateOffset >= request.FromDate.Date &&
                         w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                         w.CurrencyCode == request.Currency.ToString() &&
                         w.UserTypeId == (int)UserType.Agent).AsNoTracking();
                }
            }
            else
            {
                if (isAgent)
                {
                    result = _context.WinLoss.Where(w =>
                         w.ParentUserId == parentUserId &&
                         w.DirectUserId == directUserId &&
                         w.SettledDateOffset >= request.FromDate.Date &&
                         w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                         w.CurrencyCode == request.Currency.ToString()).AsNoTracking();
                }
                else
                {
                    result = _context.WinLoss.Where(w =>
                         w.ParentUserId == parentUserId &&
                         w.DirectUserId == directUserId &&
                         w.SettledDateOffset >= request.FromDate.Date &&
                         w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                         w.CurrencyCode == request.Currency.ToString() &&
                         w.UserTypeId == (int)UserType.Agent).AsNoTracking();
                }
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Games && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
               (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId)));
            }
            else
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId));
            }

            if (accounts.Any())
            {
                if (request.DateFilter == "transactiondate")
                {
                    accountsResult = _context.WinLoss.AsNoTracking().Where(w =>
                           w.TransactionDateOffset >= request.FromDate.Date &&
                           w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                           w.CurrencyCode == request.Currency.ToString()).AsNoTracking()
                            .Select(s => new Tuple<long, string, string>(s.IndirectUserId, s.Username, s.BetId.ToString())).Distinct();
                }
                else
                {
                    accountsResult = _context.WinLoss.AsNoTracking().Where(w =>
                           w.SettledDateOffset >= request.FromDate.Date &&
                           w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                           w.CurrencyCode == request.Currency.ToString()).AsNoTracking()
                            .Select(s => new Tuple<long, string, string>(s.IndirectUserId, s.Username, s.BetId.ToString())).Distinct();
                }

                var indirectUserIds = new List<long>();
                foreach (var account in accounts)
                {
                    //var indirectUserId = accountsResult.Where(a => account == a.Item2).FirstOrDefault();                 
                    //if (indirectUserId != null)
                    //{
                    //    indirectUserIds.AddRange(accountsResult.Where(a => a.Item1 <= indirectUserId.Item1 && a.Item3 == indirectUserId.Item3).Select(s => s.Item1).Distinct().ToList());
                    //}
                    var indirectUserId = accountsResult.Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();
                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }


                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                result = result.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
            }

            if (status.Any())
            {
                result = result.Where(p => status.Any(s => s.Equals((string.IsNullOrEmpty(p.ReferenceId) || p.StatusName.ToUpper() == "CANCELLED") ? p.StatusName.ToUpper() : $"ADJUSTMENT{p.StatusName.ToUpper()}")));
            }

            return result;
        }
        #endregion
    }
}
