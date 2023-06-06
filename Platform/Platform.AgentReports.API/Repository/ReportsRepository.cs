using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Data.Reports;
using Platform.Shared.Data.Reports.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static Platform.AgentReports.API.Models.Response.OutstandingBetFixtureResponse;
using CommissionDetails = Platform.AgentReports.API.Models.Response.CommissionDetails;
using PositionTakingDetails = Platform.AgentReports.API.Models.Response.PositionTakingDetails;

namespace Platform.AgentReports.API.Repository
{
    public class ReportsRepository : IReportsRepository
    {
        #region Fields

        private readonly AgentReportsContext _context;
        private readonly IConfiguration _config;
        private readonly IAgentRepository _agentRepository;

        #endregion

        #region Ctor

        public ReportsRepository(AgentReportsContext context, IAgentRepository agentRepository, IConfiguration config)
        {
            _context = context;
            _config = config;
            _agentRepository = agentRepository;
        }

        #endregion

        #region Methods

        //public async Task<BaseResponseDTO<WinLossResponse>> GetWinLoss(long parentAgentId, bool isAgents, WinLossAccountRequest request)
        //{

        //    #region W/O BACK AND LAY

        //    var skip = (request.Page - 1) * request.PageSize;
        //    double rowCount = 0;

        //    var states = new int[] { (int)BetStatus.Won, (int)BetStatus.Lost, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost };

        //    if (request.Currency == CurrencyCode.ALL && parentAgentId > 0)
        //        request.Currency = Enum.Parse<CurrencyCode>((await _agentRepository.GetUserDetails(parentAgentId)).Result.Currency);

        //    var iQueryableData = GetWinLossAggregateQueryable2(parentAgentId, isAgents, request).AsNoTracking();
        //    var membersCount = GetDistinctMembers2(request);

        //    var tempbets = iQueryableData.Select(s => new
        //    {
        //        IndirectUserId = s.IndirectUserId,
        //        Username = s.Username,
        //        UserTypeId = s.UserTypeId,
        //        CurrencyCode = s.CurrencyCode,
        //        BetId = s.BetId,
        //        ReferenceId = s.ReferenceId,
        //        Stake = s.Stake,
        //        TrueStake = s.TrueStake,
        //        StatusId = s.StatusId,
        //        Commission = s.Commission,
        //        MemberWinLoss = s.MemberWinLoss,
        //        MemberCommission = s.MemberCommission,
        //        MemberWLC = s.MemberWLC,
        //        AgentWinLoss = s.AgentWinLoss,
        //        AgentCommission = s.AgentCommission,
        //        AgentWLC = s.AgentWLC,
        //        DownlineWinLoss = s.DownlineWinLoss,
        //        DownlineCommission = s.DownlineCommission,
        //        DownlineWLC = s.DownlineWLC,
        //        UplineWinLoss = s.UplineWinLoss,
        //        UplineCommission = s.UplineCommission,
        //        UplineWLC = s.UplineWLC
        //    }).ToList();
            
        //    var bets = tempbets.GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).Select(d => new WinLossModel()
        //    {
        //        UserId = d.Key.IndirectUserId,
        //        Username = d.Key.Username,
        //        UserTypeId = d.Key.UserTypeId,
        //        CurrencyCode = d.Key.CurrencyCode,
        //        StakeCount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).ToList().Distinct().Count(),
        //        StakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.Stake }).ToList().Distinct().Sum(x => x.Stake),
        //        //ValidStakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.TrueStake, ss.StatusId }).Where(ss => states.ToArray().Contains(ss.StatusId)).ToList().Distinct().Sum(x => x.TrueStake),
        //        ValidStakeAmount = d.Sum(x => x.TrueStake),
        //        Commission = d.Sum(s => s.Commission),
        //        MemberWinLoss = d.Sum(s => s.MemberWinLoss),
        //        MemberCommission = d.Sum(s => s.MemberCommission),
        //        MemberWLC = d.Sum(s => s.MemberWLC),
        //        AgentWinLoss = d.Sum(s => s.AgentWinLoss),
        //        AgentCommission = d.Sum(s => s.AgentCommission),
        //        AgentWLC = d.Sum(s => s.AgentWLC),
        //        DownlineWinLoss = d.Sum(s => s.DownlineWinLoss),
        //        DownlineCommission = d.Sum(s => s.DownlineCommission),
        //        DownlineWLC = d.Sum(s => s.DownlineWLC),
        //        UplineWinLoss = d.Sum(s => s.UplineWinLoss),
        //        UplineCommission = d.Sum(s => s.UplineCommission),
        //        UplineWLC = d.Sum(s => s.UplineWLC),
        //        Transfer = d.Sum(ss => ss.AgentWLC + ss.UplineWLC) * -1,
        //        MemberCount = d.First().UserTypeId == (int)UserType.Player ? 1 : membersCount.First(m => m.Item1 == d.Key.IndirectUserId).Item2
        //    }).ToList();

        //    rowCount = bets.Count();
        //    var totalMemberCount = bets.Select(b => b.MemberCount).Sum();

        //    return new BaseResponseDTO<WinLossResponse>
        //    {
        //        StatusCode = StatusCode.Success,
        //        Message = StatusCode.Success.ToString(),
        //        Result = new WinLossResponse
        //        {
        //            CurrentPage = request.Page,
        //            PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
        //            PageSize = request.PageSize,
        //            RowCount = (int)rowCount,
        //            TotalStakeAmount = bets.Sum(s => s.StakeAmount),
        //            TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
        //            TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
        //            TotalCommission = bets.Sum(s => s.Commission),
        //            TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
        //            TotalMemberCommission = bets.Sum(s => s.MemberCommission),
        //            TotalMemberWLC = bets.Sum(s => s.MemberWLC),
        //            TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
        //            TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
        //            TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
        //            TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
        //            TotalAgentCommission = bets.Sum(s => s.AgentCommission),
        //            TotalAgentWLC = bets.Sum(s => s.AgentWLC),
        //            TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
        //            TotalUplineCommission = bets.Sum(s => s.UplineCommission),
        //            TotalUplineWLC = bets.Sum(s => s.UplineWLC),
        //            TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
        //            Bets = bets.Skip(skip).Take(request.PageSize).ToList(),
        //            TotalMemberCount = totalMemberCount
        //        }
        //    };

        //    #endregion
        //}

        //public async Task<BaseResponseDTO<WinLossResponse>> GetWinLoss(long parentAgentId, long directUserId, bool isAgents, WinLossAccountRequestExtended request)
        //{

        //    #region W/O BACK AND LAY

        //    var skip = (request.Page - 1) * request.PageSize;
        //    double rowCount = 0;

        //    //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost };

        //    if (request.Currency == CurrencyCode.ALL && parentAgentId > 0)
        //        request.Currency = Enum.Parse<CurrencyCode>((await _agentRepository.GetUserDetails(parentAgentId)).Result.Currency);

        //    var iQueryableData = GetWinLossAggregateQueryableParentAndAgentId2(parentAgentId, directUserId, isAgents, request).AsNoTracking();
        //    var membersCount = GetDistinctMembers2(request);

        //    var bets = iQueryableData.Select(s => new
        //    {
        //        IndirectUserId = s.IndirectUserId,
        //        Username = s.Username,
        //        UserTypeId = s.UserTypeId,
        //        CurrencyCode = s.CurrencyCode,
        //        BetId = s.BetId,
        //        ReferenceId = s.ReferenceId,
        //        Stake = s.Stake,
        //        TrueStake = s.TrueStake,
        //        StatusId = s.StatusId,
        //        Commission = s.Commission,
        //        MemberWinLoss = s.MemberWinLoss,
        //        MemberCommission = s.MemberCommission,
        //        MemberWLC = s.MemberWLC,
        //        AgentWinLoss = s.AgentWinLoss,
        //        AgentCommission = s.AgentCommission,
        //        AgentWLC = s.AgentWLC,
        //        DownlineWinLoss = s.DownlineWinLoss,
        //        DownlineCommission = s.DownlineCommission,
        //        DownlineWLC = s.DownlineWLC,
        //        UplineWinLoss = s.UplineWinLoss,
        //        UplineCommission = s.UplineCommission,
        //        UplineWLC = s.UplineWLC
        //    }).ToList().GroupBy(g => new { g.IndirectUserId, g.Username, g.CurrencyCode, g.UserTypeId }).Select(d => new WinLossModel()
        //    {
        //        UserId = d.Key.IndirectUserId,
        //        Username = d.Key.Username,
        //        UserTypeId = d.Key.UserTypeId,
        //        CurrencyCode = d.Key.CurrencyCode,
        //        StakeCount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).ToList().Distinct().Count(),
        //        StakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.Stake }).ToList().Distinct().Sum(x => x.Stake),
        //        //ValidStakeAmount = d.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), ss.TrueStake, ss.StatusId }).Where(ss => states.ToArray().Contains(ss.StatusId)).ToList().Distinct().Sum(x => x.TrueStake),
        //        ValidStakeAmount = d.Sum(x => x.TrueStake),
        //        Commission = d.Sum(s => s.Commission),
        //        MemberWinLoss = d.Sum(s => s.MemberWinLoss),
        //        MemberCommission = d.Sum(s => s.MemberCommission),
        //        MemberWLC = d.Sum(s => s.MemberWLC),
        //        AgentWinLoss = d.Sum(s => s.AgentWinLoss),
        //        AgentCommission = d.Sum(s => s.AgentCommission),
        //        AgentWLC = d.Sum(s => s.AgentWLC),
        //        DownlineWinLoss = d.Sum(s => s.DownlineWinLoss),
        //        DownlineCommission = d.Sum(s => s.DownlineCommission),
        //        DownlineWLC = d.Sum(s => s.DownlineWLC),
        //        UplineWinLoss = d.Sum(s => s.UplineWinLoss),
        //        UplineCommission = d.Sum(s => s.UplineCommission),
        //        UplineWLC = d.Sum(s => s.UplineWLC),
        //        Transfer = d.Sum(ss => ss.AgentWLC + ss.UplineWLC) * -1,
        //        MemberCount = d.First().UserTypeId == (int)UserType.Player ? 1 : membersCount.First(m => m.Item1 == d.Key.IndirectUserId).Item2
        //    }).ToList();

        //    rowCount = bets.Count();
        //    var totalMemberCount = bets.Select(b => b.MemberCount).Sum();

        //    return new BaseResponseDTO<WinLossResponse>
        //    {
        //        StatusCode = StatusCode.Success,
        //        Message = StatusCode.Success.ToString(),
        //        Result = new WinLossResponse
        //        {
        //            CurrentPage = request.Page,
        //            PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
        //            PageSize = request.PageSize,
        //            RowCount = (int)rowCount,
        //            TotalStakeAmount = bets.Sum(s => s.StakeAmount),
        //            TotalValidStakeAmount = bets.Sum(s => s.ValidStakeAmount),
        //            TotalStakeCount = (int)bets.Sum(s => s.StakeCount),
        //            TotalCommission = bets.Sum(s => s.Commission),
        //            TotalMemberWinLoss = bets.Sum(s => s.MemberWinLoss),
        //            TotalMemberCommission = bets.Sum(s => s.MemberCommission),
        //            TotalMemberWLC = bets.Sum(s => s.MemberWLC),
        //            TotalDownlineWinLoss = bets.Sum(s => s.DownlineWinLoss),
        //            TotalDownlineCommission = bets.Sum(s => s.DownlineCommission),
        //            TotalDownlineWLC = bets.Sum(s => s.DownlineWLC),
        //            TotalAgentWinLoss = bets.Sum(s => s.AgentWinLoss),
        //            TotalAgentCommission = bets.Sum(s => s.AgentCommission),
        //            TotalAgentWLC = bets.Sum(s => s.AgentWLC),
        //            TotalUplineWinLoss = bets.Sum(s => s.UplineWinLoss),
        //            TotalUplineCommission = bets.Sum(s => s.UplineCommission),
        //            TotalUplineWLC = bets.Sum(s => s.UplineWLC),
        //            TotalTransfer = bets.Sum(s => s.AgentWLC + s.UplineWLC) * -1,
        //            Bets = bets.Skip(skip).Take(request.PageSize).ToList(),
        //            TotalMemberCount = totalMemberCount
        //        }
        //    };

        //    #endregion
        //}

        public async Task<BaseResponseDTO<WinLossResponse>> GetWinLoss2(long parentAgentId, long directUserId, bool isAgents, WinLossAccountRequestExtended request)
        {

            #region W/O BACK AND LAY

            var currencyCode = request.Currency.ToString();
            if (request.Currency == CurrencyCode.ALL && parentAgentId > 0)
                currencyCode = Enum.Parse<CurrencyCode>((await _agentRepository.GetUserDetails(parentAgentId)).Result.Currency).ToString();
            else if (request.Currency == CurrencyCode.ALL)
                currencyCode = string.Empty;

            var productTypes = request.Product;

            var accounts = request.Accounts;

            var betIds = request.BetIds;

            var status = request.Status;

            var agentUserTypeId = isAgents ? (int)UserType.Agent : (int)UserType.Company;
            
            var sqlRaw = $"GetWinLossSummary {parentAgentId}, {directUserId}, {isAgents}, {agentUserTypeId}, {request.ProductId}, '{productTypes}', '{accounts}', '{betIds}', '{status}', '{request.FromDate.ToString("yyyy-MM-dd")}', '{request.ToDate.ToString("yyyy-MM-dd 23:59:59")}', '{currencyCode}', {request.Page}, {request.PageSize}";
            var response = await _context.ReadWinLossSummary.FromSqlRaw(sqlRaw).ToListAsync();
            var totalResponse = response.FirstOrDefault();

            if(totalResponse == null)
            {
                return new BaseResponseDTO<WinLossResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new WinLossResponse()
                };
            }

            return new BaseResponseDTO<WinLossResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossResponse
                {
                    CurrentPage = totalResponse.CurrentPage,
                    PageCount = totalResponse.PageCount,
                    PageSize = totalResponse.PageSize,
                    RowCount = totalResponse.RowCount,
                    TotalStakeAmount = totalResponse.TotalStakeAmount,
                    TotalValidStakeAmount = totalResponse.TotalValidStakeAmount,
                    TotalStakeCount = totalResponse.TotalStakeCount,
                    TotalCommission = totalResponse.TotalCommission,
                    TotalMemberWinLoss = totalResponse.TotalMemberWinLoss,
                    TotalMemberCommission = totalResponse.TotalMemberCommission,
                    TotalMemberWLC = totalResponse.TotalMemberWLC,
                    TotalDownlineWinLoss = totalResponse.TotalDownlineWinLoss,
                    TotalDownlineCommission = totalResponse.TotalDownlineCommission,
                    TotalDownlineWLC = totalResponse.TotalDownlineWLC,
                    TotalAgentWinLoss = totalResponse.TotalAgentWinLoss,
                    TotalAgentCommission = totalResponse.TotalAgentCommission,
                    TotalAgentWLC = totalResponse.TotalAgentWLC,
                    TotalUplineWinLoss = totalResponse.TotalUplineWinLoss,
                    TotalUplineCommission = totalResponse.TotalUplineCommission,
                    TotalUplineWLC = totalResponse.TotalUplineWLC,
                    TotalTransfer = totalResponse.TotalTransfer,
                    Bets = response.Select(b => new WinLossModel
                    {
                        AgentCommission = b.AgentCommission,
                        AgentWinLoss = b.AgentWinLoss,
                        UplineCommission = b.UplineCommission,
                        UplineWinLoss = b.UplineWinLoss,
                        Commission = b.Commission,
                        AgentWLC = b.AgentWLC,
                        CurrencyCode = b.CurrencyCode,
                        DownlineCommission = b.DownlineCommission,
                        DownlineWLC = b.DownlineWLC,
                        DownlineWinLoss = b.DownlineWinLoss,
                        MemberCommission = b.MemberCommission,
                        MemberCount = b.MemberCount,
                        MemberWLC = b.MemberWLC,
                        MemberWinLoss = b.MemberWinLoss,
                        ProductId = b.ProductId,
                        ProductName = b.ProductName,
                        StakeAmount = b.StakeAmount,
                        StakeCount = b.StakeCount,
                        StatusId = b.StatusId,
                        StatusName = b.StatusName,
                        Transfer = b.Transfer,
                        UplineWLC = b.UplineWLC,
                        UserId = b.UserId,
                        Username = b.Username,
                        UserTypeId = b.UserTypeId,
                        ValidStakeAmount = b.ValidStakeAmount,
                    }).ToList(),
                    TotalMemberCount = totalResponse.TotalMemberCount
                }
            };


            #endregion
        }

        //public async Task<BaseResponseDTO<WinLossDetailsResponse>> GetWinLossDetails(long parentAgentId, long directUserId, bool isAgents, WinLossAccountRequestExtended request)
        //{

        //    #region W/O BACK AND LAY

        //    var skip = (request.Page - 1) * request.PageSize;

        //    var status = new List<string>();
        //    if (!string.IsNullOrEmpty(request.Status))
        //    {
        //        status = request.Status.Split(',').Select(s => s.ToUpper().Replace("-", "")).ToList();
        //    }

        //    var productTypes = new List<int>();
        //    if (!string.IsNullOrEmpty(request.Product))
        //    {
        //        productTypes = request.Product.Split(',').Select(int.Parse).ToList();
        //    }
        //    var gameProvidersFilter = productTypes.Where(p => p > 300 && p < 400).Select(pp => pp - 300).ToList();
        //    var liveCasinoProvidersFilter = productTypes.Where(p => p > 400 && p < 500).Select(pp => pp - 400).ToList();
        //    var sportsProductFilter = productTypes.Where(p => p < 300).ToList();
        //    var virtualProvidersFilter = productTypes.Where(p => p > 200 && p < 300).Select(pp => pp - 200).ToList();

        //    IQueryable<WinLoss> profitLossDetails;

        //    var accounts = new List<string>();
        //    if (!string.IsNullOrEmpty(request.Accounts))
        //    {
        //        accounts = request.Accounts.Split(',').ToList();
        //    }

        //    if (request.DateFilter == "transactiondate")
        //    {
        //        profitLossDetails = _context.WinLoss.Where(w =>
        //            w.ParentUserId == parentAgentId &&
        //            w.IndirectUserId == directUserId &&
        //            w.TransactionDateOffset >= request.FromDate &&
        //            w.TransactionDateOffset < request.ToDate.AddDays(1)).AsNoTracking();
        //    }
        //    else
        //    {
        //        profitLossDetails = _context.WinLoss.Where(w =>
        //            w.ParentUserId == parentAgentId &&
        //            w.IndirectUserId == directUserId &&
        //            w.SettledDateOffset >= request.FromDate &&
        //            w.SettledDateOffset < request.ToDate.AddDays(1)).AsNoTracking();
        //    }

        //    if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
        //    {
        //        profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId) ||
        //       (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
        //       (r.ProductId == (int)Product.LiveCasino && liveCasinoProvidersFilter.Contains(r.ProviderBrandId)) ||
        //       (r.ProductId == (int)Product.VirtualGames && virtualProvidersFilter.Contains(r.ProviderBrandId))).AsNoTracking();
        //    }
        //    else
        //    {
        //        profitLossDetails = profitLossDetails.Where(r => sportsProductFilter.Contains(r.ProductId)).AsNoTracking();
        //    }

        //    if (accounts.Any())
        //    {
        //        profitLossDetails = profitLossDetails.Where(r => accounts.Contains(r.Username));
        //    }

        //    if (status.Any())
        //    {
        //        profitLossDetails = profitLossDetails.Where(p => status.Contains(p.StatusName.ToUpper()));
        //    }

        //    if (!string.IsNullOrEmpty(request.BetIds))
        //    {
        //        //profitLossDetails = profitLossDetails.Where(p => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper() == request.BetIds.ToUpper());
        //        profitLossDetails = profitLossDetails.Where(p => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(request.BetIds.Trim().ToUpper()));
        //    }

        //    var betsRaw = profitLossDetails.Select(ss => new
        //    {
        //        ss.SettledDate,
        //        ss.IndirectUserId,
        //        ss.ReferenceId,
        //        ss.BetId,
        //        ss.TrueStake,
        //        ss.TransactionDate,
        //        ss.Username,
        //        ss.UserTypeId,
        //        ss.CurrencyCode,
        //        ss.Stake,
        //        ss.Commission,
        //        ss.MemberCommission,
        //        ss.MemberWinLoss,
        //        ss.MemberWLC,
        //        ss.DownlineCommission,
        //        ss.DownlineWLC,
        //        ss.DownlineWinLoss,
        //        ss.AgentWinLoss,
        //        ss.AgentCommission,
        //        ss.AgentWLC,
        //        ss.UplineCommission,
        //        ss.UplineWinLoss,
        //        ss.UplineWLC,
        //        ss.IsParlay,
        //        ss.Odds,
        //        ss.OddsTypeId,
        //        ss.OddsTypeName,
        //        ss.StatusId,
        //        ss.StatusName,
        //        ss.ProductId,
        //        ss.SegmentName,
        //        ss.ProductName,
        //        ss.LeagueId,
        //        ss.LeagueName,
        //        ss.FixtureId,
        //        ss.FixtureName,
        //        ss.FixtureStartDate,
        //        ss.MarketTemplateId,
        //        ss.MarketName,
        //        ss.SelectionTemplateId,
        //        ss.SelectionName,
        //        ss.Score,
        //        ss.FixtureStatusId,
        //        ss.FixtureStatusName,
        //        ss.Line,
        //        ss.MarketTemplateGroupId,
        //        ss.MarketTemplateGroupName,
        //        ss.PositionTakingDetails,
        //        ss.CommissionDetails,
        //        ss.CasinoDetails,
        //        ss.MarketTypeId,
        //        ss.MarketGroupId
        //    }).ToList();

        //    double rowCount = betsRaw.Count();

        //    if (rowCount == 0)
        //    {
        //        return new BaseResponseDTO<WinLossDetailsResponse>()
        //        {
        //            StatusCode = StatusCode.Success,
        //            Message = StatusCode.Success.ToString(),
        //            Result = new WinLossDetailsResponse()
        //            {
        //                Bets = new List<WinLossDetailsModel>(),
        //                CurrentPage = request.PageSize,
        //                PageCount = 0,
        //                PageSize = request.PageSize
        //            }
        //        };
        //    }

        //    var totalStakeAmount = betsRaw.Sum(s => s.Stake);
        //    var totalValidStakeAmount = betsRaw.Sum(s => s.TrueStake);

        //    var betIds = betsRaw.Select(m => string.IsNullOrEmpty(m.ReferenceId) ? m.BetId : Guid.Parse(m.ReferenceId)).ToList();

        //    var bets = betsRaw.OrderBy(p => p.SettledDate).Skip(skip).Take(request.PageSize).Select(s => new WinLossDetailsModel()
        //    {
        //        UserId = s.IndirectUserId,
        //        BetId = string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId),
        //        TransactionDate = s.TransactionDate,
        //        SettlementDate = s.SettledDate,
        //        Username = s.Username,
        //        UserTypeId = s.UserTypeId,
        //        CurrencyCode = s.CurrencyCode,
        //        StakeAmount = s.Stake,
        //        ValidStakeAmount = s.TrueStake,
        //        Commission = s.Commission,
        //        MemberWinLoss = s.MemberWinLoss,
        //        MemberCommission = s.MemberCommission,
        //        MemberWLC = s.MemberWLC,
        //        DownlineWinLoss = s.DownlineWinLoss,
        //        DownlineCommission = s.DownlineCommission,
        //        DownlineWLC = s.DownlineWLC,
        //        AgentWinLoss = s.AgentWinLoss,
        //        AgentCommission = s.AgentCommission,
        //        AgentWLC = s.AgentWLC,
        //        UplineCommission = s.UplineCommission,
        //        UplineWinLoss = s.UplineWinLoss,
        //        UplineWLC = s.UplineWLC,
        //        IsParlay = s.IsParlay,
        //        Stake = s.Stake,
        //        TrueStake = s.TrueStake,
        //        Odds = s.Odds,
        //        OddsTypeId = s.OddsTypeId,
        //        OddsTypeName = s.OddsTypeName,
        //        StatusId = s.StatusId,
        //        StatusName = s.StatusName,
        //        VirtualSportName = s.ProductId == 2 ? s.SegmentName : string.Empty,
        //        ProductId = s.ProductId,
        //        ProductName = s.ProductName,
        //        LeagueId = s.LeagueId,
        //        LeagueName = string.IsNullOrEmpty(s.LeagueName) ? s.SegmentName : s.LeagueName,
        //        FixtureId = s.FixtureId,
        //        FixtureName = s.FixtureName,
        //        FixtureStartDate = s.FixtureStartDate,
        //        MarketTemplateId = s.MarketTemplateId,
        //        MarketName = s.MarketName,
        //        SelectionTemplateId = s.SelectionTemplateId,
        //        SelectionName = s.SelectionName,
        //        Score = s.Score,
        //        FixtureStatusId = s.FixtureStatusId,
        //        FixtureStatusName = s.FixtureStatusName,
        //        Line = MarketLine(s.Line, s.MarketTemplateId, s.SelectionName),
        //        MarketTemplateGroupId = s.MarketTemplateGroupId,
        //        MarketTemplateGroupName = s.MarketTemplateGroupName,
        //        PositionTakingDetails = JsonConvert.DeserializeObject<PositionTakingDetails>(s.PositionTakingDetails),
        //        CommissionDetails = JsonConvert.DeserializeObject<List<CommissionDetails>>(s.CommissionDetails), //CasinoDetails = JsonConvert.DeserializeObject<CasinoDetail>(s.CasinoDetails),
        //        Reason = string.IsNullOrEmpty(s.ReferenceId) ? string.Empty : $"ADJUSTMENT - {s.StatusName}",
        //        BetScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(1, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
        //        FinalScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(2, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
        //        HalftimeScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(3, string.IsNullOrEmpty(s.ReferenceId) ? s.BetId : Guid.Parse(s.ReferenceId)) : "",
        //        MarketTypeId = s.MarketTypeId,
        //        MarketGroupId = s.MarketGroupId
        //    }).ToList();

        //    return new BaseResponseDTO<WinLossDetailsResponse>
        //    {
        //        StatusCode = StatusCode.Success,
        //        Message = StatusCode.Success.ToString(),
        //        Result = new WinLossDetailsResponse
        //        {
        //            CurrentPage = request.Page,
        //            PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
        //            PageSize = request.PageSize,
        //            RowCount = (int)rowCount,
        //            TotalStakeAmount = totalStakeAmount,
        //            TotalValidStakeAmount = totalValidStakeAmount,
        //            TotalCommission = betsRaw.Sum(s => s.Commission),
        //            TotalMemberWinLoss = betsRaw.Sum(s => s.MemberWinLoss),
        //            TotalMemberCommission = betsRaw.Sum(s => s.MemberCommission),
        //            TotalMemberWLC = betsRaw.Sum(s => s.MemberWLC),
        //            TotalDownlineWinLoss = betsRaw.Sum(s => s.DownlineWinLoss),
        //            TotalDownlineCommission = betsRaw.Sum(s => s.DownlineCommission),
        //            TotalDownlineWLC = betsRaw.Sum(s => s.DownlineWLC),
        //            TotalAgentWinLoss = betsRaw.Sum(s => s.AgentWinLoss),
        //            TotalAgentCommission = betsRaw.Sum(s => s.AgentCommission),
        //            TotalAgentWLC = betsRaw.Sum(s => s.AgentWLC),
        //            TotalUplineWinLoss = betsRaw.Sum(s => s.UplineWinLoss),
        //            TotalUplineCommission = betsRaw.Sum(s => s.UplineCommission),
        //            TotalUplineWLC = betsRaw.Sum(s => s.UplineWLC),
        //            Bets = bets.OrderBy(s => s.TransactionDate).ToList()
        //        }
        //    };

        //    #endregion
        //}

        public async Task<BaseResponseDTO<WinLossDetailsResponse>> GetWinLossDetails2(long parentAgentId, long directUserId, bool isAgents, WinLossAccountRequestExtended request)
        {

            #region W/O BACK AND LAY

            var currencyCode = request.Currency.ToString();
            if (request.Currency == CurrencyCode.ALL && parentAgentId > 0)
                currencyCode = Enum.Parse<CurrencyCode>((await _agentRepository.GetUserDetails(parentAgentId)).Result.Currency).ToString();
            else if (request.Currency == CurrencyCode.ALL)
                currencyCode = string.Empty;

            var productTypes = request.Product;

            var accounts = request.Accounts;

            var betIds = request.BetIds;

            var status = request.Status;

            var agentUserTypeId = isAgents ? (int)UserType.Agent : (int)UserType.Company;

            var sqlRaw = $"GetWinLossDetails {parentAgentId}, {directUserId}, {isAgents}, {agentUserTypeId}, {request.ProductId}, '{productTypes}', '{accounts}', '{betIds}', '{status}', '{request.FromDate.ToString("yyyy-MM-dd")}', '{request.ToDate.ToString("yyyy-MM-dd 23:59:59")}', '{currencyCode}', {request.Page}, {request.PageSize}";
            var response = await _context.ReadWinLossDetails.FromSqlRaw(sqlRaw).ToListAsync();
            var totalResponse = response.FirstOrDefault();

            if (totalResponse == null)
            {
                return new BaseResponseDTO<WinLossDetailsResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new WinLossDetailsResponse()
                };
            }
            
            var bets = response.Select(s => new WinLossDetailsModel()
            {
                UserId = s.UserId,
                BetId = s.BetId,
                TransactionDate = s.TransactionDate,
                SettlementDate = s.SettlementDate,
                Username = s.Username,
                UserTypeId = s.UserTypeId,
                CurrencyCode = s.CurrencyCode,
                StakeAmount = s.StakeAmount,
                ValidStakeAmount = s.ValidStakeAmount,
                Commission = s.Commission,
                MemberWinLoss = s.MemberWinLoss,
                MemberCommission = s.MemberCommission,
                MemberWLC = s.MemberWLC,
                DownlineWinLoss = s.DownlineWinLoss,
                DownlineCommission = s.DownlineCommission,
                DownlineWLC = s.DownlineWLC,
                AgentWinLoss = s.AgentWinLoss,
                AgentCommission = s.AgentCommission,
                AgentWLC = s.AgentWLC,
                UplineCommission = s.UplineCommission,
                UplineWinLoss = s.UplineWinLoss,
                UplineWLC = s.UplineWLC,
                IsParlay = s.IsParlay,
                Stake = s.StakeAmount,
                TrueStake = s.ValidStakeAmount,
                Odds = s.Odds,
                OddsTypeId = s.OddsTypeId,
                OddsTypeName = s.OddsTypeName,
                StatusId = s.StatusId,
                StatusName = s.StatusName,
                VirtualSportName = s.VirtualSportName,
                ProductId = s.ProductId,
                ProductName = s.ProductName,
                LeagueId = s.LeagueId,
                LeagueName = s.LeagueName,
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
                Line = MarketLine(s.Line, s.MarketTemplateId, s.SelectionName),
                MarketTemplateGroupId = s.MarketTemplateGroupId,
                MarketTemplateGroupName = s.MarketTemplateGroupName,
                PositionTakingDetails = JsonConvert.DeserializeObject<PositionTakingDetails>(s.PositionTakingDetails),
                CommissionDetails = JsonConvert.DeserializeObject<List<CommissionDetails>>(s.CommissionDetails), //CasinoDetails = JsonConvert.DeserializeObject<CasinoDetail>(s.CasinoDetails),
                Reason = s.Reason,
                BetScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(1, s.BetId) : "",
                FinalScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(2, s.BetId) : "",
                HalftimeScore = (!s.IsParlay && s.ProductId == (int)Product.Sportsbook) ? GetScore(3, s.BetId) : "",
                MarketTypeId = s.MarketTypeId,
                MarketGroupId = s.MarketGroupId
            }).ToList();

            return new BaseResponseDTO<WinLossDetailsResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WinLossDetailsResponse
                {
                    CurrentPage = totalResponse.CurrentPage,
                    PageCount = totalResponse.PageCount,
                    PageSize = totalResponse.PageSize,
                    RowCount = totalResponse.RowCount,
                    TotalStakeAmount = totalResponse.TotalStakeAmount,
                    TotalValidStakeAmount = totalResponse.TotalValidStakeAmount,
                    TotalCommission = totalResponse.TotalCommission,
                    TotalMemberWinLoss = totalResponse.TotalMemberWinLoss,
                    TotalMemberCommission = totalResponse.TotalMemberCommission,
                    TotalMemberWLC = totalResponse.TotalMemberWLC,
                    TotalDownlineWinLoss = totalResponse.TotalDownlineWinLoss,
                    TotalDownlineCommission = totalResponse.TotalDownlineCommission,
                    TotalDownlineWLC = totalResponse.TotalDownlineWLC,
                    TotalAgentWinLoss = totalResponse.TotalAgentWinLoss,
                    TotalAgentCommission = totalResponse.TotalAgentCommission,
                    TotalAgentWLC = totalResponse.TotalAgentWLC,
                    TotalUplineWinLoss = totalResponse.TotalUplineWinLoss,
                    TotalUplineCommission = totalResponse.TotalUplineCommission,
                    TotalUplineWLC = totalResponse.TotalUplineWLC,
                    Bets = bets.OrderBy(s => s.TransactionDate).ToList()
                }
            };


            
            
            #endregion
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

            var records = _context.Bet.Where(c => c.ParentUserId == request.AgentId && c.StatusId == (int)BetStatus.Running && supportedProduct.Contains(c.ProductId)).AsNoTracking().ToList();

            if (request.FromDate.HasValue)
                records = records.Where(c => c.TransactionDateOffset.Date >= request.FromDate.Value.Date).ToList();

            if (request.ToDate.HasValue)
                records = records.Where(c => c.TransactionDateOffset.Date <= request.ToDate.Value.Date).ToList();

            if (!string.IsNullOrEmpty(request.UserName))
                records = records.Where(c => c.Username.Trim().ToLower().Contains(request.UserName.Trim().ToLower())).ToList();

            if (!string.IsNullOrEmpty(request.AgentPartnerUserName))
                records = records.Where(c => c.AgentUsername.ToString().Trim().ToLower().Contains(request.AgentPartnerUserName.Trim().ToLower())).ToList();

            if (!string.IsNullOrEmpty(request.BetId))
                records = records.Where(c => c.BetId.ToString().Trim().ToLower().Contains(request.BetId.Trim().ToLower())).ToList();

            if (request.CurrencyCode != CurrencyCode.ALL)
            {
                var currencyCode = request.CurrencyCode.ToString();
                records = records.Where(c => c.CurrencyCode == currencyCode).ToList();
            }

            rowCount = records.Count();
            totalReceive = records.Sum(s => s.PositionTaking * s.TrueStake);
            totalStake = records.Sum(s => s.Stake);
            var bets = records.OrderByDescending(o => o.TransactionDate).ToList();

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

            foreach (var bet in bets)
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
                    TotalReceive = totalReceive,
                    TotalStake = totalStake,
                    Bets = result
                }
            };
        }

        public async Task<BaseResponseDTO<OutstandingBetResponse>> GetParlayBets(SearchBetRequest request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            var rowCount = (double)0;
            var result = new List<OutstandingBetResponse.Bet>();
            var totalReceive = 0m;
            var totalStake = 0m;

            var query = _context.Bet.Where(c => c.ParentUserId == request.AgentId && c.StatusId == (int)BetStatus.Running && c.Count > 1);

            if (!query.Any())
            {
                return new BaseResponseDTO<OutstandingBetResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new OutstandingBetResponse
                    {
                        CurrentPage = request.Page,
                        PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize),
                        PageSize = request.PageSize,
                        RowCount = (int)rowCount,
                        Bets = new List<OutstandingBetResponse.Bet>()
                    }
                };
            }

            rowCount = query.Count();

            var bets = query.OrderByDescending(o => o.TransactionDate)
                .Skip(skip).Take(request.PageSize)
                .Select(s => new
                {
                    BetId = s.BetId,
                    Stake = s.Stake,
                    PositionTaking = s.PositionTaking,
                    TrueStake = s.TrueStake,
                    TransactionDate = s.TransactionDate,
                    ParentUserId = s.ParentUserId,
                    UserId = s.UserId,
                    Username = s.Username,
                    Commission = s.Commission,
                    Odds = s.Odds,
                    OddsTypeId = s.OddsTypeId,
                    OddsTypeName = s.OddsTypeName,
                    Payout = s.Payout,
                    WinLoss = s.WinLoss,
                    StatusId = s.StatusId,
                    StatusName = s.StatusName,
                    Reason = s.Reason,
                    CurrencyCode = s.CurrencyCode,
                    IPAddress = s.IPAddress,
                    MarketName = s.MarketName,
                    MarketTemplateId = s.MarketTemplateId,
                    MarketTypeId = s.MarketTypeId
                }).ToList();

            totalReceive = query.Sum(s => s.ReceivePositionTaking);
            totalStake = query.Sum(s => s.Stake);

            var betIds = bets.Select(s => s.BetId).Distinct().ToList();
            var betSelections = _context.BetSelection.Where(c => c.ParentUserId == request.AgentId && betIds.Contains(c.BetId)).ToList();

            foreach (var bet in bets)
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
                    Score = s.Score
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
                    BetSelections = selections
                });
            }

            return new BaseResponseDTO<OutstandingBetResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new OutstandingBetResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    TotalReceive = totalReceive,
                    TotalStake = totalStake,
                    Bets = result,
                }
            };
        }

        public async Task<BaseResponseDTO<TotalBetResponse>> GetTotalBets(TotalBetsRequest request)
        {
            var startDate = GetStartDate();
            var endDate = GetEndDate(startDate);
            var statuses = new int[] { (int)StatusType.Void, (int)StatusType.Rejected };

            var currentBetQuery = _context.Bet.AsNoTracking().Where(c => c.ParentUserId == request.AgentId && c.ProductId == (int)Product.Sportsbook && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1));
            var currentWinLossQuery = _context.WinLoss.AsNoTracking().Where(c => c.ParentUserId == request.AgentId && c.ProductId == (int)Product.Sportsbook && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1));
            var currentLeaderboardQuery = _context.WinLoss.AsNoTracking().Where(c => c.ParentUserId == request.AgentId && c.UserTypeId == (int)UserType.Player && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1));

            if (request.CurrencyCode != null && request.CurrencyCode != CurrencyCode.ALL)
            {
                currentBetQuery = currentBetQuery.Where(c => c.CurrencyCode == request.CurrencyCode.ToString());
                currentWinLossQuery = currentWinLossQuery.Where(c => c.CurrencyCode == request.CurrencyCode.ToString());
                currentLeaderboardQuery = currentLeaderboardQuery.Where(c => c.CurrencyCode == request.CurrencyCode.ToString());
            }

            var currentBets = currentBetQuery
                .Select(s => new
                {
                    ParentUserId = s.ParentUserId,
                    ProductId = s.ProductId,
                    TransactionDateOffset = s.TransactionDateOffset,
                    StatusId = s.StatusId,
                    AgentId = s.AgentId,
                    TrueStake = s.TrueStake,
                    UserId = s.UserId,
                    CurrencyCode = s.CurrencyCode,
                    AgentUsername = s.AgentUsername
                }).ToList();

            var currentWinLoss = currentWinLossQuery
                .Select(s => new
                {
                    ParentUserId = s.ParentUserId,
                    DirectUserId = s.DirectUserId,
                    ProductId = s.ProductId,
                    TransactionDateOffset = s.TransactionDateOffset,
                    MemberWLC = s.MemberWLC,
                    AgentWLC = s.AgentWLC,
                    StatusId = s.StatusId,
                    IsParlay = s.IsParlay,
                    SportId = s.SportId,
                    TrueStake = s.TrueStake,
                    SportName = s.SportName,
                    CurrencyCode = s.CurrencyCode,
                    AgentUsername = s.AgentUsername,
                    Username = s.Username
                })
                .ToList();

            var currentLeaderBoard = currentLeaderboardQuery
                .Select(s => new
                {
                    ParentUserId = s.ParentUserId,
                    IndirectUserId = s.IndirectUserId,
                    UserTypeId = s.UserTypeId,
                    TransactionDateOffset = s.TransactionDateOffset,
                    Username = s.Username,
                    MemberWLC = s.MemberWLC,
                    AgentUsername = s.AgentUsername,
                    CurrencyCode = s.CurrencyCode
                })
                .ToList();

            if (!string.IsNullOrEmpty(request.AgentPartnerUserName))
            {
                var agentPartnerList = request.AgentPartnerUserName.Split(',').ToList();
                currentBets = currentBets.Where(b => agentPartnerList.Contains(b.AgentUsername)).ToList();

                currentWinLoss = currentWinLoss.Where(b => agentPartnerList.Contains(b.AgentUsername.Trim().ToUpper().Equals("COMPANY") ? b.Username : b.AgentUsername)).ToList();

                currentLeaderBoard = currentLeaderBoard.Where(b => agentPartnerList.Contains(b.AgentUsername.Trim().ToUpper().Equals("COMPANY") ? b.Username : b.AgentUsername)).ToList();
            }

            var totalStakeCount = currentBets.Where(c => c.ParentUserId == request.AgentId && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Count();
            var totalStakeAmount = currentBets.Where(c => c.ParentUserId == request.AgentId && !statuses.ToArray().Contains(c.StatusId) && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Sum(s => s.TrueStake);
            var totalAgentcount = currentBets.Where(c => c.ParentUserId == request.AgentId && c.AgentId != request.AgentId && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Select(s => s.AgentId).Distinct().Count();
            var totalMemberCount = currentBets.Where(c => c.ParentUserId == request.AgentId && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Select(s => s.UserId).Distinct().Count();

            var stakes = currentBets.Where(c => c.ParentUserId == request.AgentId && c.ProductId == (int)Product.Sportsbook && c.CurrencyCode == request.CurrencyCode.ToString() && !statuses.ToArray().Contains(c.StatusId) && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Select(s => s.TrueStake).ToList();

            var averageStake = stakes.Count == 0 ? 0 : totalStakeAmount / totalStakeCount;

            var totalMemberWLC = currentWinLoss.Where(c => c.ParentUserId == request.AgentId && c.DirectUserId == request.AgentId && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Sum(s => s.MemberWLC);
            var totalAgentWLC = currentWinLoss.Where(c => c.ParentUserId == request.AgentId && c.DirectUserId == request.AgentId && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Sum(s => s.AgentWLC);
            var totalTurnover = currentWinLoss.Where(c => c.ParentUserId == request.AgentId && c.DirectUserId == request.AgentId && !statuses.ToArray().Contains(c.StatusId) && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).Sum(s => s.TrueStake);

            var betTotalDaily = currentBets.Where(c => c.ParentUserId == request.AgentId && !statuses.ToArray().Contains(c.StatusId) && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1)).GroupBy(g => g.TransactionDateOffset.Date)
                .Select(s => new TotalBetResponse.DailyBetTotal
                {
                    Day = s.Key.DayOfWeek.ToString(),
                    TotalStakeCount = s.Count(),
                    TotalStakeAmount = s.Sum(ss => ss.TrueStake)
                }).ToList();

            var leaderBoard = currentLeaderBoard.Where(c => c.ParentUserId == request.AgentId && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1))
                .GroupBy(g => g.IndirectUserId).Select(s => new TotalBetResponse.BetTotalLeaderboard
                {
                    Username = s.Select(ss => ss.Username).FirstOrDefault(),
                    TotalAmount = s.Sum(ss => ss.MemberWLC),
                    AgentName = s.Select(ss => ss.AgentUsername).FirstOrDefault()

                }).ToList();

            //won
            var leaderBoardWon = leaderBoard.Where(l => l.TotalAmount > 0).OrderByDescending(o => o.TotalAmount).Take(5)
                .ToList();

            //lost
            var leaderBoardLost = leaderBoard.Where(l => l.TotalAmount < 0).OrderBy(o => o.TotalAmount).Take(5)
                .ToList();

            var sportsBetTotal = currentWinLoss.Where(c => c.ParentUserId == request.AgentId && c.DirectUserId == request.AgentId && c.IsParlay == false && !statuses.ToArray().Contains(c.StatusId) && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1))
                .GroupBy(g => g.SportId).Select(s => new TotalBetResponse.SportBetTotal
                {
                    SportId = s.Key,
                    SportName = s.Select(ss => ss.SportName).FirstOrDefault(),
                    TotalStakeCount = s.Count(),
                    TotalMemberWLC = s.Sum(ss => ss.MemberWLC),
                    TotalAgentWLC = s.Sum(ss => ss.AgentWLC),
                    TotalTurnover = s.Sum(ss => ss.TrueStake),
                    TotalMargin = s.Sum(ss => ss.AgentWLC) < 0 ? ((Math.Abs(s.Sum(ss => ss.AgentWLC)) / Math.Abs(s.Sum(ss => ss.TrueStake))) * 100) * -1 : s.Sum(ss => ss.AgentWLC) == 0 ? 0 : (Math.Abs(s.Sum(ss => ss.AgentWLC)) / Math.Abs(s.Sum(ss => ss.TrueStake))) * 100
                }).ToList();

            var parlayBetTotal = currentWinLoss.Where(c => c.ParentUserId == request.AgentId && c.DirectUserId == request.AgentId && c.IsParlay == true && !statuses.ToArray().Contains(c.StatusId) && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1))
                .GroupBy(g => g.SportId).Select(s => new TotalBetResponse.SportBetTotal
                {
                    SportId = 0,
                    SportName = "Parlay",
                    TotalStakeCount = s.Count(),
                    TotalMemberWLC = s.Sum(ss => ss.MemberWLC),
                    TotalAgentWLC = s.Sum(ss => ss.AgentWLC),
                    TotalTurnover = s.Sum(ss => ss.TrueStake),
                    TotalMargin = s.Sum(ss => ss.AgentWLC) < 0 ? ((Math.Abs(s.Sum(ss => ss.AgentWLC)) / Math.Abs(s.Sum(ss => ss.TrueStake))) * 100) * -1 : s.Sum(ss => ss.AgentWLC) == 0 ? 0 : (Math.Abs(s.Sum(ss => ss.AgentWLC)) / Math.Abs(s.Sum(ss => ss.TrueStake))) * 100
                }).FirstOrDefault();

            if (parlayBetTotal != null)
                sportsBetTotal.Add(parlayBetTotal);

            var betTotal = new TotalBetResponse
            {
                TotalStakeCount = totalStakeCount,
                TotalStakeAmount = totalStakeAmount,
                TotalAgentCount = totalAgentcount,
                TotalMemberCount = totalMemberCount,
                TotalMemberWLC = totalMemberWLC,
                TotalAgentWLC = totalAgentWLC,
                TotalTurnover = totalTurnover,
                TotalMargin = totalAgentWLC < 0 ? (Math.Abs(totalAgentWLC) / Math.Abs(totalTurnover) * 100) * -1 : totalAgentWLC == 0 ? 0 : (Math.Abs(totalAgentWLC) / Math.Abs(totalTurnover)) * 100,
                AverageStake = averageStake,
                StartWeek = startDate,
                EndWeek = endDate,
                BetTotalDaily = betTotalDaily,
                LeaderboardWon = leaderBoardWon,
                LeaderboardLost = leaderBoardLost,
                SportsBetTotal = sportsBetTotal
            };

            return new BaseResponseDTO<TotalBetResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = betTotal
            };

        }

        public async Task<BaseResponseDTO<TotalBetOtherResponse>> GetTotalBetsOthers(TotalBetsRequest request)
        {
            var startDate = GetStartDate();
            var endDate = GetEndDate(startDate);

            var productBetTotalQuery = _context.WinLoss.AsNoTracking().Where(c => c.ParentUserId == request.AgentId && c.DirectUserId == request.AgentId && c.ProductId > (int)Product.Sportsbook && c.IsParlay == false && c.TransactionDateOffset >= startDate && c.TransactionDateOffset < endDate.AddDays(1));

            if (request.CurrencyCode != null && request.CurrencyCode != CurrencyCode.ALL)
            {
                productBetTotalQuery = productBetTotalQuery.Where(c => c.CurrencyCode == request.CurrencyCode.ToString());
            }

            var productBetTotal = productBetTotalQuery
                .Select(s => new
                {
                    ParentUserId = s.ParentUserId,
                    ProductId = s.ProductId,
                    TransactionDateOffset = s.TransactionDateOffset,
                    StatusId = s.StatusId,
                    SportId = s.SportId,
                    SportName = s.SportName,
                    ReferenceId = s.ReferenceId,
                    AgentWLC = s.AgentWLC,
                    MemberWLC = s.MemberWLC,
                    BetId = s.BetId,
                    TrueStake = s.TrueStake,
                    AgentUsername = s.AgentUsername,
                    Username = s.Username
                }).ToList();

            if (!string.IsNullOrEmpty(request.AgentPartnerUserName))
            {
                var agentPartnerList = request.AgentPartnerUserName.Split(',').ToList();
                productBetTotal = productBetTotal.Where(b => agentPartnerList.Contains(b.AgentUsername.Trim().ToUpper().Equals("COMPANY") ? b.Username : b.AgentUsername)).ToList();
            }

            var newProductBetTotal = productBetTotal.GroupBy(g => g.SportId).Select(s => new TotalBetOtherResponse.TotalBetOther
            {
                ProductId = s.Key,
                ProductName = s.Select(ss => ss.SportName).FirstOrDefault(),
                TotalStakeCount = s.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper() }).Distinct().Count(),
                TotalMemberWLC = s.Sum(ss => ss.MemberWLC),
                TotalAgentWLC = s.Sum(ss => ss.AgentWLC),
                TotalTurnover = s.Select(ss => new { BetId = string.IsNullOrEmpty(ss.ReferenceId) ? ss.BetId.ToString().ToUpper() : ss.ReferenceId.ToUpper(), TrueStake = ss.TrueStake }).Distinct().Sum(ss => ss.TrueStake),
                TotalMargin = s.Sum(ss => ss.AgentWLC) < 0 ? ((Math.Abs(s.Sum(ss => ss.AgentWLC)) / Math.Abs(s.Sum(ss => ss.TrueStake))) * 100) * -1 : s.Sum(ss => ss.AgentWLC) == 0 ? 0 : (Math.Abs(s.Sum(ss => ss.AgentWLC)) / Math.Abs(s.Sum(ss => ss.TrueStake))) * 100
            }).ToList();

            var betTotal = new TotalBetOtherResponse
            {
                ProductBetTotal = newProductBetTotal
            };

            return new BaseResponseDTO<TotalBetOtherResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = betTotal
            };
        }

        public async Task<BaseResponseDTO<OutstandingBetFixtureResponse>> GetOutstandingBetFixtures(long agentId, int page, int pageSize, CurrencyCode currency)
        {
            var skip = (page - 1) * pageSize;
            double rowCount = 0;
            var result = new List<BetFixtureModel>();

            var fullTimeMarketTemplateIds = new List<string> { "1", "2", "3", "22", "23" };
            var halfTimeMarketTemplateIds = new List<string> { "8", "9", "10" };

            var marketTemplateIds = new List<string> { "1", "2", "3", "22", "23", "8", "9", "10" };

            var currencies = currency == CurrencyCode.ALL
                ? Enum.GetValues(typeof(CurrencyCode)).Cast<CurrencyCode>().Select(s => s.ToString()).ToList()
                : new List<string>() { currency.ToString() };

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

        public async Task<BaseResponseDTO<List<MonthlyGrossGamingRevenueResponse>>> GetPartnerMonthlyGGR(long userId, int year, string currencyCode)
        {
            if (year > DateTime.UtcNow.AddHours(-4).Year)
                return new BaseResponseDTO<List<MonthlyGrossGamingRevenueResponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new List<MonthlyGrossGamingRevenueResponse>()
                };

            var result = new List<MonthlyGrossGamingRevenueResponse>();

            var partnerRevenueShare = _context.PartnerRevenueShare.AsNoTracking().Where(p => p.UserId == userId && p.EffectivityDate.Year == year).OrderByDescending(o => o.EffectivityDate) ??
                                        _context.PartnerRevenueShare.AsNoTracking().Where(p => p.UserId == userId && p.EffectivityDate.Year < year).OrderByDescending(x => x.EffectivityDate);

            if (!partnerRevenueShare.Any()) return new BaseResponseDTO<List<MonthlyGrossGamingRevenueResponse>>()
            {
                StatusCode = StatusCode.Success,
                Message = "Partner cannot be found.",
                Result = null
            };

            if (!partnerRevenueShare.First().IsGGREnabled) return new BaseResponseDTO<List<MonthlyGrossGamingRevenueResponse>>()
            {
                StatusCode = StatusCode.Success,
                Message = "GGR is disabled.",
                Result = null
            };

            var winLoss = _context.WinLoss.Where(w => w.ParentUserId == 0 && w.DirectUserId == userId && w.SettledDateOffset.Year == year && w.CurrencyCode == currencyCode).AsNoTracking().ToList();

            var noOfMonths = DateTime.UtcNow.AddHours(-4).Month;

            if (winLoss.Any())
            {
                for (var i = 1; i < (noOfMonths + 1); i++)
                {
                    var winLossPerMonth = winLoss.Where(w => w.SettledDateOffset.Month == i).ToList();
                    var date = new DateTime(year, i, 1);
                    if (!winLossPerMonth.Any())
                    {
                        var partnerConfig = partnerRevenueShare.OrderByDescending(p => p.Id).FirstOrDefault(p => p.EffectivityDate.Month <= date.Month && p.EffectivityDate.Year <= date.Year);

                        var monthlyFee = 0.00m;
                        var companyShare = 0.00m;
                        if (partnerConfig != null)
                        {
                            monthlyFee = partnerConfig.MonthlyFee;
                            companyShare = partnerConfig.CompanyGGR;
                        }

                        result.Add(new MonthlyGrossGamingRevenueResponse()
                        {
                            Month = $"{date:MMM}-{date:yyyy}",
                            CompanyGGR = 0.00m,
                            CompanyShare = companyShare * 100,
                            GGR = 0.00m,
                            MonthlyFee = monthlyFee,
                            SettlementAmount = monthlyFee

                        });
                    }
                    else
                    {
                        var effectiveDate = winLoss.Select(ss => ss.SettledDateOffset).FirstOrDefault();

                        var partnerConfig = partnerRevenueShare.FirstOrDefault(p => p.EffectivityDate.Month == effectiveDate.Month) ??
                                            partnerRevenueShare.First();

                        var companyShare = partnerConfig.CompanyGGR;
                        var monthlyFee = partnerConfig.MonthlyFee;
                        var totalGgr = winLossPerMonth.Sum(s => s.GGR).GetValueOrDefault(0);
                        var companyGgr = totalGgr < 0 ? 0 : totalGgr * companyShare;

                        var companyGgrShare = totalGgr * companyShare;

                        result.Add(new MonthlyGrossGamingRevenueResponse()
                        {
                            Month = $"{date:MMM}-{date:yyyy}",
                            GGR = totalGgr,
                            CompanyGGR = companyGgr,
                            CompanyShare = companyShare * 100,
                            MonthlyFee = monthlyFee,
                            SettlementAmount = companyGgr == 0 ? monthlyFee : (companyGgrShare + monthlyFee)
                        });
                    }
                }
            }

            return new BaseResponseDTO<List<MonthlyGrossGamingRevenueResponse>>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

        public async Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, ProfitLossRequest request)
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
        public async Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, long directUserId, ProfitLossRequest request)
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

        public async Task<BaseResponseDTO<WinLossDetailsResponse>> GetProfitLossDetails(long parentAgentId, long directUserId, ProfitLossRequest request)
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

            if (!string.IsNullOrEmpty(request.Currency.ToString()))
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                profitLossDetails = profitLossDetails.Where(p => status.Contains(p.StatusName.ToUpper()));
            }

            if (betIds.Any())
            {
                //profitLossDetails = profitLossDetails.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                profitLossDetails = profitLossDetails.Where(p => betIds.Contains(p.BetId.ToString()));
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
        public async Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter)
        {
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
        public async Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter)
        {
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

        public async Task<BaseResponseDTO<WinLossDetailsResponse>> GetProfitLossDetails(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter)
        {
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
        public async Task<BaseResponseDTO<GGRResponse>> GetGGR(long parentAgentId, long directUserId, WinLossAccountRequestExtendedDTO request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            double rowCount = 0;

            //var states = new int[] { (int)StatusType.Won, (int)StatusType.Lost, (int)StatusType.HalfWon, (int)StatusType.HalfLost, (int)StatusType.Void };

            if (request.Currency == CurrencyCode.ALL && parentAgentId > 0)
                request.Currency = Enum.Parse<CurrencyCode>((await _agentRepository.GetUserDetails(parentAgentId)).Result.Currency);

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
        public async Task<BaseResponseDTO<GGRDetailsResponse>> GetGGRDetails(long agentId, long playerId, WinLossAccountRequestExtendedDTO request)
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                profitLossDetails = profitLossDetails.Where(p => status.Contains(p.StatusName.ToUpper()));
            }

            if (betIds.Any())
            {
                //profitLossDetails = profitLossDetails.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));                
                profitLossDetails = profitLossDetails.ToList().Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper()))).AsQueryable();
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
                CasinoDetails = JsonConvert.DeserializeObject<CasinoDetailDTO>(s.CasinoDetails),
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

        public async Task<BaseResponseDTO<TransferHistoryResponse>> GetPaymentTransferHistory(TransferHistoryRequest request)
        {
            var skip = (request.Page - 1) * request.PageSize;

            var records = _context.TransferHistory.AsNoTracking().Where(c => c.ParentUserId == request.AgentId && c.UserTypeId == request.UserTypeId && c.IsCashPlayer == request.IsCashMarket);

            if (request.FromDate != null)
                records = records.Where(c => c.TransactionDateOffset.Date >= request.FromDate.Value.Date);

            if (request.ToDate != null)
                records = records.Where(c => c.TransactionDateOffset.Date <= request.ToDate.Value.AddDays(1).Date);

            if (!string.IsNullOrEmpty(request.CurrencyCode))
                records = records.Where(c => c.CurrencyCode.Trim() == request.CurrencyCode.Trim());

            if (!string.IsNullOrEmpty(request.Username))
                records = records.Where(c => c.Username.Trim().ToLower().Contains(request.Username.Trim().ToLower()));

            var rowCount = (double)records.Count();
            var transferHistory = records.OrderByDescending(o => o.TransactionDateOffset)
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();

            var result = transferHistory.Select(s => new TransferHistoryResponse.Detail
            {
                UserId = s.UserId,
                Username = s.Username,
                TransactionDate = s.TransactionDate,
                CurrencyCode = s.CurrencyCode,
                TransferredAmount = GetTransferredAmount(s.Amount, (TransactionType)s.TransactionTypeId),
                RemainingCashBalance = request.IsCashMarket ? s.PostBalance : s.PostBalance * -1
            }).ToList();

            return new BaseResponseDTO<TransferHistoryResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TransferHistoryResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = (int)rowCount,
                    Transfers = result
                }
            };
        }

        public async Task<BaseResponseDTO<PartnerRevenueSettlementResponse>> GetPartnerRevenueForSettlement(PartnerRevenueSettlementRequest request)
        {
            var result = new PartnerRevenueSettlementResponse();

            //get partners
            var partnerQuery = _context.PartnerRevenueShare.Where(c => c.IsGGREnabled)
                .Select(s => new { s.UserId, s.UserName })
                .Distinct();

            if (!string.IsNullOrEmpty(request.Username))
            {
                partnerQuery = partnerQuery.Where(c => c.UserName.Contains(request.Username));
            }

            var partners = partnerQuery.ToList();

            var rowCount = 0;
            var skip = (request.Page - 1) * request.PageSize;

            var monthlyFee = 0.00m;
            var companyShare = 0.00m;

            var currentDate = new DateTime(DateTime.UtcNow.AddHours(-4).Year, DateTime.UtcNow.AddHours(-4).Month, 1);

            foreach (var partner in partners)
            {
                var userDetails = await _agentRepository.GetUserDetails(partner.UserId);

                if (userDetails.Result.Currency != request.CurrencyCode)
                    continue;

                rowCount += 1;

                var lastSettlement = _context.SettlementHistory.Where(c => c.UserId == partner.UserId).OrderByDescending(o => o.Id).FirstOrDefault();

                if (lastSettlement == null)
                {
                    var winLoss = _context.WinLoss.Where(c => c.ParentUserId == 0 && c.DirectUserId == partner.UserId && c.CurrencyCode == request.CurrencyCode).AsNoTracking().ToList();

                    if (winLoss.Any())
                    {
                        var winLossGrouped = winLoss.GroupBy(g => new { g.SettledDateOffset.Year, g.SettledDateOffset.Month });

                        foreach (var winLossGroup in winLossGrouped)
                        {
                            var winLossGroupDate = new DateTime(winLossGroup.Key.Year, winLossGroup.Key.Month, 1);

                            var revShare = _context.PartnerRevenueShare.Where(c => c.UserId == partner.UserId &&
                                                           c.EffectivityDate.Month == winLossGroupDate.Month &&
                                                           c.EffectivityDate.Year == winLossGroupDate.Year)
                                               .OrderByDescending(o => o.CreatedDate).FirstOrDefault() ??
                                           _context.PartnerRevenueShare
                                               .Where(c => c.UserId == partner.UserId && c.EffectivityDate < winLossGroupDate)
                                               .OrderByDescending(o => o.CreatedDate).FirstOrDefault();

                            monthlyFee = revShare?.MonthlyFee ?? 0.00m;
                            companyShare = revShare?.CompanyGGR ?? 0.00m;

                            if (winLossGroupDate < currentDate)
                            {
                                //var balance = (winLossGroup.Sum(w => w.GGR < 0 ? 0 : w.GGR).GetValueOrDefault(0) * companyShare) + monthlyFee;
                                var balance = winLossGroup.Sum(s => s.GGR) < 0 ? monthlyFee : ((winLossGroup.Sum(s => s.GGR) * companyShare) + monthlyFee);

                                _context.SettlementHistory.Add(new SettlementHistory
                                {
                                    UserId = partner.UserId,
                                    UserName = partner.UserName,
                                    CurrencyCode = userDetails.Result.Currency,
                                    Coverage = $"{winLossGroup.FirstOrDefault()?.SettledDateOffset:MM}/{winLossGroup.FirstOrDefault()?.SettledDateOffset:yyyy}",
                                    Balance = balance.Value,
                                    IsSettled = false
                                });

                                _context.SaveChanges();
                            }
                        }
                    }

                }
                else
                {
                    var lastSettlementMonth = Convert.ToInt32(lastSettlement.Coverage.Split("/")[0]);
                    var lastSettlementYear = Convert.ToInt32(lastSettlement.Coverage.Split("/")[1]);
                    var lastSettlementDate = new DateTime(lastSettlementYear, lastSettlementMonth, 1);

                    var winLoss = _context.WinLoss.Where(c => c.ParentUserId == 0 && c.DirectUserId == partner.UserId && c.SettledDateOffset > lastSettlementDate.AddMonths(1) && c.SettledDateOffset < currentDate && c.CurrencyCode == request.CurrencyCode).AsNoTracking().AsEnumerable();
                    var winLossGrouped = winLoss.GroupBy(g => new { g.SettledDateOffset.Year, g.SettledDateOffset.Month }).ToList();

                    foreach (var winLossGroup in winLossGrouped)
                    {
                        var winLossGroupDate = new DateTime(winLossGroup.Key.Year, winLossGroup.Key.Month, 1);

                        var revShare = _context.PartnerRevenueShare
                                           .Where(c => c.UserId == partner.UserId &&
                                                       c.EffectivityDate.Month == winLossGroupDate.Month &&
                                                       c.EffectivityDate.Year == winLossGroupDate.Year)
                                           .OrderByDescending(o => o.CreatedDate).FirstOrDefault() ??
                                       _context.PartnerRevenueShare
                                           .Where(c => c.UserId == partner.UserId && c.EffectivityDate < winLossGroupDate)
                                           .OrderByDescending(o => o.CreatedDate).FirstOrDefault();

                        monthlyFee = revShare?.MonthlyFee ?? 0.00m;
                        companyShare = revShare?.CompanyGGR ?? 0.00m;

                        if (winLossGroupDate < currentDate)
                        {
                            //var balance = (winLossGroup.Sum(w => w.GGR < 0 ? 0 : w.GGR).GetValueOrDefault(0) * companyShare) + monthlyFee;
                            var balance = winLossGroup.Sum(s => s.GGR) < 0 ? monthlyFee : ((winLossGroup.Sum(s => s.GGR) * companyShare) + monthlyFee);

                            _context.SettlementHistory.Add(new SettlementHistory
                            {
                                UserId = partner.UserId,
                                UserName = partner.UserName,
                                CurrencyCode = userDetails.Result.Currency,
                                Coverage = $"{winLossGroup.FirstOrDefault()?.SettledDateOffset:MM}/{winLossGroup.FirstOrDefault()?.SettledDateOffset:yyyy}",
                                Balance = balance.Value,
                                IsSettled = false
                            });

                            _context.SaveChanges();

                        }
                    }
                }

                var revenueShare = _context.PartnerRevenueShare
                    .Where(p => p.UserId == partner.UserId).OrderByDescending(o => o.CreatedDate).FirstOrDefault();

                var unsettledRevenue = _context.SettlementHistory.Where(c => c.UserId == partner.UserId && c.IsSettled == false).AsNoTracking().OrderBy(o => o.Id).ToList();
                var currentProfitLoss = _context.WinLoss.Where(c => c.ParentUserId == 0 && c.DirectUserId == partner.UserId && c.SettledDateOffset >= currentDate).AsNoTracking().ToList();

                if (revenueShare != null)
                {
                    monthlyFee = revenueShare.MonthlyFee;
                    companyShare = revenueShare.CompanyGGR;
                }

                result.RevenueSettlementDetails.Add(new PartnerRevenueSettlementResponse.Detail
                {
                    UserId = partner.UserId,
                    UserName = partner.UserName,
                    CurrentMonthBalance = (currentProfitLoss?.Sum(s => s.GGR) < 0 ? monthlyFee : ((currentProfitLoss?.Sum(s => s.GGR) * companyShare) + monthlyFee)).GetValueOrDefault(0).ToString(CultureInfo.InvariantCulture),
                    UnsettledAmount = unsettledRevenue.Sum(s => s.Balance).ToString(CultureInfo.InvariantCulture),
                    SettlementAmount = unsettledRevenue.Sum(s => s.Balance).ToString(CultureInfo.InvariantCulture),
                });
            }

            result.RowCount = rowCount;
            result.CurrentPage = request.Page;
            result.PageSize = request.PageSize;
            result.PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize);

            result.TotalUnsettledAmount = result.RevenueSettlementDetails.Sum(r => Convert.ToDecimal(r.UnsettledAmount)).ToString(CultureInfo.InvariantCulture);
            result.TotalSettlementAmount = result.RevenueSettlementDetails.Sum(r => Convert.ToDecimal(r.SettlementAmount)).ToString(CultureInfo.InvariantCulture);

            result.RevenueSettlementDetails = result.RevenueSettlementDetails.Skip(skip).Take(request.PageSize).ToList();

            return new BaseResponseDTO<PartnerRevenueSettlementResponse>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

        public async Task<BaseResponseDTO<PartnerRevenueSettlementHistoryResponse>> GetPartnerRevenueForSettlementHistory(PartnerRevenueSettlementHistoryRequest request)
        {
            var result = new PartnerRevenueSettlementHistoryResponse();
            var skip = (request.Page - 1) * request.PageSize;

            var settlementHistory = _context.SettlementHistory.Where(c => c.IsSettled && c.CurrencyCode == request.CurrencyCode && c.DateSettled.Value.AddHours(-4).Date >= request.DateFrom.Date && c.DateSettled.Value.AddHours(-4).Date <= request.DateTo.Date).AsNoTracking().OrderBy(o => o.DateSettled).ToList();

            if (!string.IsNullOrEmpty(request.UserIds))
            {
                var userIdsAsLong = request.UserIds.Split(',').Select(long.Parse).ToList();
                settlementHistory = settlementHistory.Where(c => userIdsAsLong.Contains(c.UserId)).ToList();
            }

            if (!string.IsNullOrEmpty(request.SettledBy))
            {
                settlementHistory = settlementHistory.Where(c => c.SettledBy.ToLower().Trim().Contains(request.SettledBy.ToLower().Trim())).ToList();
            }

            var groupedSettlementHistory = settlementHistory.GroupBy(g => new { g.UserId, g.DateSettled }).ToList();
            var rowCount = groupedSettlementHistory.Count();
            groupedSettlementHistory = groupedSettlementHistory.Skip(skip).Take(request.PageSize).ToList();

            result.Page = request.Page;
            result.PageSize = request.PageSize;
            result.PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize);
            result.RowCount = rowCount;
            result.RevenueSettlementHistoryDetails = new List<PartnerRevenueSettlementHistoryResponse.Detail>();

            foreach (var settlement in groupedSettlementHistory)
            {
                var firstSettlementMonth = Convert.ToInt32(settlement.OrderBy(o => o.Coverage).First().Coverage.Split("/")[0]);
                var firstSettlementYear = Convert.ToInt32(settlement.OrderBy(o => o.Coverage).First().Coverage.Split("/")[1]);
                DateTime firstSettlementDate = new DateTime(firstSettlementYear, firstSettlementMonth, 1);
                var coverage = $"{firstSettlementDate:MMM}-{firstSettlementDate:yyyy}";
                if (settlement.Count() > 1)
                {
                    var lastSettlementMonth = Convert.ToInt32(settlement.OrderBy(s => s.Coverage).Last().Coverage.Split("/")[0]);
                    var lastSettlementYear = Convert.ToInt32(settlement.OrderBy(s => s.Coverage).Last().Coverage.Split("/")[1]);
                    DateTime lastSettlementDate = new DateTime(lastSettlementYear, lastSettlementMonth, 1);
                    coverage += $" - {lastSettlementDate:MMM}-{lastSettlementDate:yyyy}";
                }

                result.RevenueSettlementHistoryDetails.Add(new PartnerRevenueSettlementHistoryResponse.Detail()
                {
                    UserName = settlement.FirstOrDefault().UserName,
                    SettlementDate = settlement.FirstOrDefault().DateSettled.Value,
                    SettledAmount = settlement.FirstOrDefault().AmountSettled.Value,
                    MonthSettled = coverage,
                    SettledBy = settlement.FirstOrDefault().SettledBy
                });
            }

            return new BaseResponseDTO<PartnerRevenueSettlementHistoryResponse>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

        public async Task<BaseResponseDTO<CancelBetHistoryResponse>> GetCancelBetHistory(CancelBetHistoryRequest request)
        {
            var clientName = "song88";

            if (!string.IsNullOrEmpty(request.PartnerName) && clientName.IndexOf(request.PartnerName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return new BaseResponseDTO<CancelBetHistoryResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CancelBetHistoryResponse()
                };
            }

            var query = _context.WinLoss.AsNoTracking()
                .Where(m => m.ParentUserId == 0
                    && m.UserTypeId == (int)UserType.Player
                    && m.SettledDate >= request.FromSettledDate
                    && m.SettledDate <= request.ToSettledDate.AddDays(1)
                    && (m.StatusId == (int)StatusType.Void || m.StatusId == (int)StatusType.Cancelled));

            if (request.BetIds.Count() > 0)
                query = query.Where(m => request.BetIds.Contains(string.IsNullOrEmpty(m.ReferenceId) ? m.BetId.ToString() : m.ReferenceId));

            if (!string.IsNullOrEmpty(request.MemberName))
                query = query.Where(m => m.Username.IndexOf(request.MemberName, StringComparison.OrdinalIgnoreCase) >= 0);

            // Get cancelled bets in the WinLoss table
            var betsRaw = query
                .Select(m => new
                {
                    m.SettledDate,
                    m.IndirectUserId,
                    m.ReferenceId,
                    m.BetId,
                    m.TrueStake,
                    m.TransactionDate,
                    m.Username,
                    m.UserTypeId,
                    m.CurrencyCode,
                    m.Stake,
                    m.Commission,
                    m.MemberCommission,
                    m.MemberWinLoss,
                    m.MemberWLC,
                    m.DownlineCommission,
                    m.DownlineWLC,
                    m.DownlineWinLoss,
                    m.AgentWinLoss,
                    m.AgentCommission,
                    m.AgentWLC,
                    m.UplineCommission,
                    m.UplineWinLoss,
                    m.UplineWLC,
                    m.IsParlay,
                    m.Odds,
                    m.OddsTypeId,
                    m.OddsTypeName,
                    m.StatusId,
                    m.StatusName,
                    m.ProductId,
                    m.SegmentName,
                    m.ProductName,
                    m.LeagueId,
                    m.LeagueName,
                    m.FixtureId,
                    m.FixtureName,
                    m.FixtureStartDate,
                    m.MarketTemplateId,
                    m.MarketName,
                    m.SelectionTemplateId,
                    m.SelectionName,
                    m.Score,
                    m.FixtureStatusId,
                    m.FixtureStatusName,
                    m.Line,
                    m.MarketTemplateGroupId,
                    m.MarketTemplateGroupName,
                    m.PositionTakingDetails,
                    m.CommissionDetails,
                    m.CasinoDetails,
                    m.MarketTypeId,
                    m.MarketGroupId
                })
                .ToList();

            var selectedBetIds = betsRaw
                .Select(m =>
                {
                    var betId = string.IsNullOrEmpty(m.ReferenceId) ? m.BetId : Guid.Parse(m.ReferenceId);
                    return betId;
                })
                .Distinct()
                .ToList();

            var bets = _context.Bet.AsNoTracking().Where(m => selectedBetIds.Contains(m.BetId))
                .Select(m => new
                {
                    m.BetId,
                    m.Reason
                })
                .ToList();

            var betSelections = _context.BetSelection.AsNoTracking().Where(m => m.ParentUserId == 0 && selectedBetIds.Contains(m.BetId)).ToList();

            var betsResponse = betsRaw.OrderBy(p => p.SettledDate)
                .Select(m => new CancelBetHistoryResponse.Bet()
                {
                    UserId = m.IndirectUserId,
                    BetId = string.IsNullOrEmpty(m.ReferenceId) ? m.BetId : Guid.Parse(m.ReferenceId),
                    TransactionDate = m.TransactionDate,
                    SettlementDate = m.SettledDate,
                    Username = m.Username,
                    UserTypeId = m.UserTypeId,
                    CurrencyCode = m.CurrencyCode,
                    StakeAmount = m.Stake,
                    IsParlay = m.IsParlay,
                    Stake = m.Stake,
                    TrueStake = m.TrueStake,
                    Odds = m.Odds,
                    OddsTypeId = m.OddsTypeId,
                    OddsTypeName = m.OddsTypeName,
                    StatusId = m.StatusId,
                    StatusName = m.StatusName,
                    VirtualSportName = m.ProductId == 2 ? m.SegmentName : string.Empty,
                    ProductId = m.ProductId,
                    ProductName = m.ProductName,
                    LeagueId = m.LeagueId,
                    LeagueName = string.IsNullOrEmpty(m.LeagueName) ? m.SegmentName : m.LeagueName,
                    FixtureId = m.FixtureId,
                    FixtureName = m.FixtureName,
                    FixtureStartDate = m.FixtureStartDate,
                    MarketTemplateId = m.MarketTemplateId,
                    MarketName = m.MarketName,
                    SelectionTemplateId = m.SelectionTemplateId,
                    SelectionName = m.SelectionName,
                    Score = m.Score,
                    FixtureStatusId = m.FixtureStatusId,
                    FixtureStatusName = m.FixtureStatusName,
                    Line = MarketLine(m.Line, m.MarketTemplateId, m.SelectionName),
                    MarketTemplateGroupId = m.MarketTemplateGroupId,
                    MarketTemplateGroupName = m.MarketTemplateGroupName,
                    Reason = string.IsNullOrEmpty(m.ReferenceId) ? bets.FirstOrDefault(b => b.BetId == m.BetId)?.Reason : bets.FirstOrDefault(b => b.BetId == Guid.Parse(m.ReferenceId))?.Reason,
                    BetScore = (!m.IsParlay && m.ProductId == (int)Product.Sportsbook) ? GetScore(1, string.IsNullOrEmpty(m.ReferenceId) ? m.BetId : Guid.Parse(m.ReferenceId)) : "",
                    FinalScore = (!m.IsParlay && m.ProductId == (int)Product.Sportsbook) ? GetScore(2, string.IsNullOrEmpty(m.ReferenceId) ? m.BetId : Guid.Parse(m.ReferenceId)) : "",
                    MarketTypeId = m.MarketTypeId,
                    MarketGroupId = m.MarketGroupId,
                    ClientName = "Song88",
                    ModifiedByBackofficeUser = string.Empty,
                    BetSelections = betSelections.Where(b => b.BetId == (string.IsNullOrEmpty(m.ReferenceId) ? m.BetId : Guid.Parse(m.ReferenceId)))
                    .Select(b => new CancelBetHistoryResponse.BetSelection
                    {
                        ParentUserId = b.ParentUserId,
                        BetId = b.BetId,
                        BetSelectionId = b.BetSelectionId,
                        UserId = b.UserId,
                        SportId = b.SportId,
                        SportName = b.SportName,
                        SegmentId = b.SegmentId,
                        SegmentName = b.SegmentName,
                        LeagueId = b.LeagueId,
                        LeagueName = string.IsNullOrEmpty(b.LeagueName) ? b.SegmentName : b.LeagueName,
                        FixtureId = b.FixtureId,
                        FixtureName = b.FixtureName,
                        MarketId = b.MarketId,
                        MarketName = b.MarketName,
                        SelectionId = b.SelectionId,
                        SelectionName = b.SelectionName,
                        FixtureStatusId = b.FixtureStatusId,
                        FixtureStatusName = b.FixtureStatusName,
                        Line = b.Line,
                        Odds = b.Odds,
                        StatusId = b.StatusId,
                        StatusName = b.StatusName,
                        IsSettled = b.IsSettled,
                        IsResettled = b.IsResettled,
                        FixtureStartDate = b.FixtureStartDate,
                        MarketTemplateGroupId = b.MarketTemplateGroupId,
                        MarketTemplateGroupName = GetMarketTemplateGroupName(b.MarketTemplateGroupId),
                        MarketTemplateId = b.MarketTemplateId,
                        SelectionTemplateId = b.SelectionTemplateId,
                        Score = b.Score
                    }).ToList()
                })
                .ToList();

            return new BaseResponseDTO<CancelBetHistoryResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new CancelBetHistoryResponse
                {
                    RowCount = betsResponse.Count(),
                    Bets = betsResponse,
                }
            };
        }

        public async Task<BaseResponseDTO<ProfileResponse>> GetProfile(long userId)
        {
            var startDateLastWeek = GetStartDate().AddDays(-7);
            var endDateLastWeek = GetEndDate(startDateLastWeek);

            var agentRunningBalance = _context.RunningBalance.Where(c => c.ParentUserId == userId).AsNoTracking().ToList();
            var downlineRunningBalance = _context.RunningBalance.Where(c => c.ParentUserId == userId && c.UserTypeId == (int)UserType.Player && c.IsCashPlayer == false).AsNoTracking().ToList();
            var totalOutstandingCount = _context.Bet.Where(c => c.ParentUserId == userId && c.UserTypeId == (int)UserType.Player && c.StatusId == (int)StatusType.Running).AsNoTracking().Count();
            var totalOutstanding = _context.Bet.Where(c => c.ParentUserId == userId && c.UserTypeId == (int)UserType.Player && c.StatusId == (int)StatusType.Running).AsNoTracking();
            var agentBalance = _context.RunningBalance.Where(c => c.ParentUserId == userId && c.UserTypeId == (int)UserType.Agent && c.IsCashPlayer == false).AsNoTracking();
            var memberBalance = _context.RunningBalance.Where(c => c.ParentUserId == userId && c.UserTypeId == (int)UserType.Player && c.IsCashPlayer == false).AsNoTracking();

            var cashBalance = 0m;
            cashBalance += (agentBalance.Sum(x => x.TrueCashBalance) * -1);
            cashBalance += memberBalance.Sum(x => x.TrueCashBalance);

            //check if the userId is player
            //need to refactor this. need to get the userTypeId from agentApi (Internal)
            var winLoss = _context.WinLoss.FirstOrDefault(c => c.ParentUserId == 0 && c.IndirectUserId == userId);

            var userType = (int)UserType.Agent;

            if (winLoss != null)
            {
                userType = winLoss.UserTypeId;
            }

            var wlcToday = 0.0m;
            var wlcYesterday = 0.0m;
            var wlcLastWeek = 0.0m;

            if (userType == (int)UserType.Player)
            {
                wlcToday = _context.WinLoss
                    .Where(c => c.ParentUserId == 0 && c.IndirectUserId == userId &&
                                c.TransactionDateOffset.Date == DateTime.UtcNow.AddHours(-4).Date)
                    .AsNoTracking().Sum(s => s.MemberWLC);
                wlcYesterday = _context.WinLoss
                    .Where(c => c.ParentUserId == 0 && c.IndirectUserId == userId &&
                                c.TransactionDateOffset.Date == DateTime.UtcNow.AddHours(-4).AddDays(-1).Date)
                    .AsNoTracking().Sum(s => s.MemberWLC);
                wlcLastWeek = _context.WinLoss
                    .Where(c => c.ParentUserId == 0 && c.IndirectUserId == userId &&
                                c.TransactionDateOffset >= startDateLastWeek &&
                                c.TransactionDateOffset <= endDateLastWeek).AsNoTracking().Sum(s => s.MemberWLC);
                cashBalance = _context.RunningBalance.First(r => r.UserId == userId).CashBalance * -1;
            }
            else if (userType == (int)UserType.Agent)
            {
                var currDate = DateTime.UtcNow.AddHours(-4).Day;

                wlcToday = _context.WinLoss
                    .Where(c => c.ParentUserId == userId && c.DirectUserId == userId &&
                                c.SettledDateOffset.Date == DateTime.UtcNow.AddHours(-4).Date).AsNoTracking()
                    .Sum(s => s.AgentWLC);
                wlcYesterday = _context.WinLoss
                    .Where(c => c.ParentUserId == userId && c.DirectUserId == userId &&
                                c.SettledDateOffset.Date == DateTime.UtcNow.AddHours(-4).AddDays(-1).Date)
                    .AsNoTracking().Sum(s => s.AgentWLC);
                wlcLastWeek = _context.WinLoss
                    .Where(c => c.ParentUserId == userId && c.DirectUserId == userId &&
                                c.SettledDateOffset.Date >= startDateLastWeek.Date &&
                                c.SettledDateOffset.Date <= endDateLastWeek.Date).AsNoTracking().Sum(s => s.AgentWLC);
            }

            return new BaseResponseDTO<ProfileResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ProfileResponse
                {
                    AgentId = agentRunningBalance.Select(s => s.ParentUserId).FirstOrDefault(),
                    PlayersBalance = downlineRunningBalance.Sum(s => s.CashBalance) * -1, //changed from true cash balance to cash balance (FCI-1309)
                    CashBalance = cashBalance,
                    TotalOutstanding = totalOutstanding.Sum(s => s.ReceivePositionTaking),
                    TotalOutstandingCount = totalOutstandingCount,
                    WLCToday = wlcToday,
                    WLCYesterday = wlcYesterday,
                    WLCLastWeek = wlcLastWeek
                }
            };
        }

        public async Task<Tuple<int, string, decimal, decimal>> GetRunningBalance(string userId)
        {
            var param = new SqlParameter[]
            {
                new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = userId
                },
                new SqlParameter
                {
                    ParameterName = "@StatusCode",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@StatusName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@OutstandingBalance",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@CashBalance",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Output
                }
            };

            await _context.Database.ExecuteSqlRawAsync($"EXEC GetRunningBalance @UserId, @StatusCode OUTPUT, @StatusName OUTPUT, @OutstandingBalance OUTPUT, @CashBalance OUTPUT", param);

            var statusCode = (int)param[1].Value;
            var statusName = (string)param[2].Value;
            var outstandingBalance = (decimal)param[3].Value;
            var cashBalance = (decimal)param[4].Value;

            return new Tuple<int, string, decimal, decimal>(statusCode, statusName, outstandingBalance, cashBalance);
        }

        public async Task<BaseResponseDTO<bool>> CreateRunningBalance(CreateRunningBalanceRequest request)
        {
            var runningBalance = _context.RunningBalance.FirstOrDefault(c => c.ParentUserId == request.ParentAgentId && c.UserId == request.UserId);

            if (runningBalance == null)
            {
                _context.RunningBalance.Add(new RunningBalance
                {
                    ParentUserId = request.ParentAgentId,
                    UserId = request.UserId,
                    Username = request.Username,
                    UserTypeId = request.UserType,
                    CashBalance = 0,
                    TrueCashBalance = 0,
                    OutstandingBalance = 0,
                    IsCashPlayer = request.PlayerType == 1 ? true : false,
                    CurrencyCode = ((CurrencyCode)request.CurrencyCode).ToString()
                });

                _context.SaveChanges();

                return new BaseResponseDTO<bool>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
                };
            }

            return new BaseResponseDTO<bool>
            {
                StatusCode = StatusCode.Record_Is_Already_Existing,
                Message = StatusCode.Record_Is_Already_Existing.ToString().Replace("_", " "),
                Result = false
            };
        }

        public async Task<BaseResponseDTO<bool>> CreateRevenueShareConfig(RevenueShareConfigRequest request)
        {
            var revShareConfig = _context.PartnerRevenueShare.Where(c => c.UserId == request.UserId);

            _context.PartnerRevenueShare.Add(new PartnerRevenueShare
            {
                UserId = request.UserId,
                UserName = request.UserName,
                IsGGREnabled = request.IsGGREnabled,
                ClientGGR = request.ClientGGR,
                CompanyGGR = request.CompanyGGR,
                MonthlyFee = request.MonthlyFee,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                EffectivityDate = !revShareConfig.Any() ? DateTime.UtcNow : GetFirstDayOfNextMonth()
            });

            _context.SaveChanges();

            return new BaseResponseDTO<bool>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = true
            };
        }
        
        public async Task<BaseResponseDTO<List<long>>> GetActiveMembers(long agentId, CurrencyCode currency, DateTime? fromDate, DateTime? toDate)
        {
            var currentBets = _context.Bet.Where(c => c.ParentUserId == agentId && (currency == CurrencyCode.ALL ? c.CurrencyCode == c.CurrencyCode : c.CurrencyCode == currency.ToString()))
                .Select(s => new
                {
                    TransactionDateOffset = s.TransactionDateOffset,
                    UserId = s.UserId
                }).ToList();

            if (fromDate != null && toDate != null)
                currentBets = currentBets.Where(c => c.TransactionDateOffset.Date >= fromDate.Value.Date && c.TransactionDateOffset.Date <= toDate.Value.Date).ToList();

            var userIds = currentBets.Select(c => c.UserId).Distinct().ToList();

            return new BaseResponseDTO<List<long>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = userIds
            };
        }

        public async Task<BaseResponseDTO<OutstandingBetResponse>> GetOutstandingBets(OutstandingBetRequest request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            var rowCount = (double)0;
            var result = new List<OutstandingBetResponse.Bet>();
            var supportedProduct = new[] { 1, 2 };
            var mergedResults = new List<OutstandingBetResponse.Bet>();

            var bets = _context.Bet.Where(c => c.ParentUserId == request.AgentId && c.StatusId == (int)StatusType.Running && supportedProduct.Contains(c.ProductId)).AsNoTracking();

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

            var totalStake = bets.Sum(s => s.Stake);
            var totalReceive = bets.Sum(s => s.PositionTaking * s.TrueStake);

            rowCount = bets.Count();

            bets = bets.Skip(skip).Take(request.PageSize);

            var betIds = bets.Select(s => s.BetId).Distinct().ToList();
            var betSelections = _context.BetSelection.Where(c => betIds.Contains(c.BetId)).ToList();

            foreach (var bet in bets)
            {
                var selections = betSelections.Where(c => c.BetId == bet.BetId && c.ParentUserId == request.ParentId)
                    .Select(s => new OutstandingBetResponse.BetSelection
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
                        Line = MarketLine(s.Line, s.MarketTemplateId, s.SelectionName),
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
                        Score = s.Score
                    }).Distinct().ToList();

                var fixtureName = selections.Select(s => s.FixtureName).FirstOrDefault();

                result.Add(new OutstandingBetResponse.Bet
                {
                    ParentUserId = bet.ParentUserId,
                    BetId = bet.BetId,
                    UserId = bet.UserId,
                    Username = bet.Username,
                    PositionTaking = bet.PositionTaking,
                    Commission = bet.Commission,
                    //Stake = bet.Stake,
                    Stake = bet.TrueStake,
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
                    TotalReceive = totalReceive,
                    TotalStake = totalStake,
                    RowCount = (int)rowCount,
                    Bets = result
                }
            };
        }

        public async Task<BaseResponseDTO<BetModelResponse>> GetBetHistory(long agentId, Guid betId)
        {
            var bet = _context.Bet.Find(agentId, betId);
            var betSelections = new List<BetSelection>();

            if (bet == null)
            {
                return new BaseResponseDTO<BetModelResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetModelResponse()
                };
            }


            return new BaseResponseDTO<BetModelResponse>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetModelResponse()
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
                    FixtureName = bet.FixtureName,
                    Receive = bet.Stake * bet.PositionTaking,
                    MarketTypeId = bet.MarketTypeId,
                    SelectionTemplateId = bet.SelectionTemplateId,
                    SelectionName = bet.SelectionName,
                    ExchangeRate = bet.ExchangeRate,
                    MarketName = bet.MarketName,
                    MarketTemplateId = bet.MarketTemplateId,
                    BetSelections = _context.BetSelection.Where(s => s.BetId == bet.BetId
                                     && s.ParentUserId == 0).AsNoTracking().ToList()
                                             .Select(s => new BetSelectionModelResponse()
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
                                                 Line = MarketLine(s.Line, s.MarketTemplateId, s.SelectionName),
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
                                                 MarketTypeId = _context.Bet.First(ss => ss.BetId == s.BetId).MarketTypeId,
                                                 BetScore = s.FirstScore,
                                                 FinalScore = s.SecondScore
                                             }).ToList()
                }

            };
        }
        public async Task<BaseResponseDTO<BetModelResponse>> GetBetHistoryProfitLoss(long agentId, Guid betId)
        {
            var bet = _context.Bet.Find(agentId, betId);
            var betSelections = new List<BetSelection>();

            if (bet == null)
            {
                return new BaseResponseDTO<BetModelResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetModelResponse()
                };
            }

            return new BaseResponseDTO<BetModelResponse>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetModelResponse()
                {
                    ParentUserId = bet.ParentUserId,
                    BetId = bet.BetId,
                    UserId = bet.UserId,
                    Username = bet.Username,
                    PositionTaking = bet.PositionTaking,
                    Commission = bet.Commission / bet.ExchangeRate,
                    Stake = bet.Stake / bet.ExchangeRate,
                    TrueStake = bet.TrueStake / bet.ExchangeRate,
                    Odds = bet.Odds,
                    OddsTypeId = bet.OddsTypeId,
                    OddsTypeName = bet.OddsTypeName,
                    Payout = bet.Payout / bet.ExchangeRate,
                    WinLoss = bet.WinLoss / bet.ExchangeRate,
                    StatusId = bet.StatusId,
                    StatusName = bet.StatusName,
                    Reason = bet.Reason,
                    CurrencyCode = bet.CurrencyCode,
                    IPAddress = bet.IPAddress,
                    TransactionDate = bet.TransactionDate,
                    FixtureName = bet.FixtureName,
                    Receive = bet.Stake * bet.PositionTaking,
                    MarketTypeId = bet.MarketTypeId,
                    SelectionTemplateId = bet.SelectionTemplateId,
                    SelectionName = bet.SelectionName,
                    ExchangeRate = bet.ExchangeRate,
                    MarketName = bet.MarketName,
                    MarketTemplateId = bet.MarketTemplateId,
                    BetSelections = _context.BetSelection.Where(s => s.BetId == bet.BetId
                                     && s.ParentUserId == 0).AsNoTracking().ToList()
                                             .Select(s => new BetSelectionModelResponse()
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
                                                 Line = MarketLine(s.Line, s.MarketTemplateId, s.SelectionName),
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
                                                 MarketTypeId = _context.Bet.First(ss => ss.BetId == s.BetId).MarketTypeId,
                                                 BetScore = s.FirstScore,
                                                 FinalScore = s.SecondScore
                                             }).ToList()
                }

            };
        }

        public async Task<BaseResponseDTO<ProductResponse>> GetProducts()
        {

            var productTypes = new List<ProductType>();

            var productType = new ProductType
            {
                ProductTypeId = (int)Product.Sportsbook,
                ProductTypeName = Product.Sportsbook.ToString(),
                Products = new List<ProductModel>()
                {
                    new ProductModel
                    {
                        ProductId = 1,
                        ProductName = "Soccer"
                    },
                    new ProductModel
                    {
                        ProductId = 2,
                        ProductName = "Basketball"
                    },
                    new ProductModel
                    {
                        ProductId = 3,
                        ProductName = "Baseball"
                    },
                    new ProductModel
                    {
                        ProductId = 5,
                        ProductName = "Tennis"
                    },
                    new ProductModel
                    {
                        ProductId = 12,
                        ProductName = "Rugby"
                    },
                    new ProductModel
                    {
                        ProductId = 12,
                        ProductName = "Rugby"
                    },
                    new ProductModel
                    {
                        ProductId = 13,
                        ProductName = "Aussie Rules"
                    },
                    new ProductModel
                    {
                        ProductId = 16,
                        ProductName = "American Football"
                    },
                    new ProductModel
                    {
                        ProductId = 20,
                        ProductName = "Table Tennis"
                    },
                    new ProductModel
                    {
                        ProductId = 21,
                        ProductName = "Cricket"
                    },
                    new ProductModel
                    {
                        ProductId = 22,
                        ProductName = "Darts"
                    },
                    new ProductModel
                    {
                        ProductId = 23,
                        ProductName = "Volleyball"
                    },
                    new ProductModel
                    {
                        ProductId = 1001,
                        ProductName = "Virtual Soccker"
                    },
                    new ProductModel
                    {
                        ProductId = 1002,
                        ProductName = "Virtual Basketball"
                    },
                    new ProductModel
                    {
                        ProductId = 1005,
                        ProductName = "Virtual Tennis"
                    },
                    new ProductModel
                    {
                        ProductId = 1021,
                        ProductName = "Virtual Cricket"
                    },
                    new ProductModel
                    {
                        ProductId = 1058,
                        ProductName = "Virtual Dog Racing"
                    }
                }
            };


            productTypes.Add(productType);

            productType = new ProductType
            {
                ProductTypeId = (int)Product.Casino,
                ProductTypeName = Product.Casino.ToString(),
                Products = new List<ProductModel>()
                {
                    new ProductModel
                    {
                        ProductId = 1,
                        ProductName = "Blueprint Gaming"
                    },
                    new ProductModel
                    {
                        ProductId = 2,
                        ProductName = "No Limit City"
                    },
                    new ProductModel
                    {
                        ProductId = 3,
                        ProductName = "Hacksaw Gaming"
                    },
                    new ProductModel
                    {
                        ProductId = 4,
                        ProductName = "Evoplay"
                    },
                    new ProductModel
                    {
                        ProductId = 5,
                        ProductName = "Quickspin"
                    }
                }
            };

            productTypes.Add(productType);

            productType = new ProductType
            {
                ProductTypeId = (int)Product.LiveCasino,
                ProductTypeName = Product.LiveCasino.ToString(),
                Products = new List<ProductModel>(){
                    new ProductModel
                    {
                        ProductId = 1,
                        ProductName = "Ezugi"
                    },
                    new ProductModel
                    {
                        ProductId = 2,
                        ProductName = "101 Bet Games"
                    },
                    new ProductModel
                    {
                        ProductId = 3,
                        ProductName = "Evolution"
                    },
                    new ProductModel
                    {
                        ProductId = 4,
                        ProductName = "Better Live"
                    },
                    new ProductModel
                    {
                        ProductId = 5,
                        ProductName = "Skywind Live"
                    }
                }
            };

            productTypes.Add(productType);


            productType = new ProductType
            {
                ProductTypeId = (int)Product.VirtualGames,
                ProductTypeName = Product.VirtualGames.ToString(),
                Products = new List<ProductModel>(){
                    new ProductModel
                    {
                        ProductId = 1,
                        ProductName = "1x2 Gaming"
                    },
                    new ProductModel
                    {
                        ProductId = 2,
                        ProductName = "101 Bet Games"
                    },
                    new ProductModel
                    {
                        ProductId = 3,
                        ProductName = "Kiron"
                    }
                }
            };

            productTypes.Add(productType);

            var products = new ProductResponse
            {
                ProductTypes = productTypes
            };

            return new BaseResponseDTO<ProductResponse>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = products
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

        private string MarketLine(string lineStr, string marketTemplateIdStr, string selectionName)
        {
            var line = string.IsNullOrEmpty(lineStr) ? 0m : Convert.ToDecimal(lineStr);
            var marketTemplateId = string.IsNullOrEmpty(marketTemplateIdStr) ? 0l : Convert.ToInt64(marketTemplateIdStr);

            //long[] handicapMarkets = { 2, 9, 22, 25, 26 };
            long[] handicapMarkets = { 2, 9, 22, 25, 26, 29, 32, 33, 36, 39, 42, 44, 49, 52, 108, 153, 161, 163, 204 };

            if (handicapMarkets.Contains(marketTemplateId))
            {
                if (line > 0)
                {
                    return selectionName.ToLower() == "home" ? Math.Abs(line).ToString() : (Math.Abs(line) * -1).ToString();
                }

                if (line < 0)
                {
                    return selectionName.ToLower() == "home" ? (Math.Abs(line) * -1).ToString() : Math.Abs(line).ToString();
                }
            }

            return line.ToString();
        }

        private IQueryable<WinLoss> GetWinLossAggregateQueryable2(long parentUserId, bool isAgent, WinLossAccountRequest request)
        {
            IQueryable<WinLoss> result;
            IQueryable<Tuple<long, string, string>> accountsResult;

            var productTypes = new List<int>();
            if (!string.IsNullOrEmpty(request.Product))
            {
                productTypes = request.Product.Split(',').Select(int.Parse).ToList();
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

            var currencies = new List<string>() { request.Currency.ToString() };
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
                         w.DirectUserId == parentUserId &&
                         w.TransactionDateOffset >= request.FromDate.Date &&
                         w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                         currencies.Contains(w.CurrencyCode)).AsNoTracking();
                }
                else
                {
                    result = _context.WinLoss.Where(w =>
                       w.ParentUserId == parentUserId &&
                       w.DirectUserId == parentUserId &&
                       w.TransactionDateOffset >= request.FromDate.Date &&
                       w.TransactionDateOffset < request.ToDate.AddDays(1).Date &&
                       currencies.Contains(w.CurrencyCode) &&
                       w.UserTypeId == (int)UserType.Agent).AsNoTracking();
                }
            }
            else
            {
                if (isAgent)
                {
                    result = _context.WinLoss.Where(w =>
                      w.ParentUserId == parentUserId &&
                      w.DirectUserId == parentUserId &&
                      w.SettledDateOffset >= request.FromDate.Date &&
                      w.SettledDateOffset < request.ToDate.AddDays(1).Date &&
                      currencies.Contains(w.CurrencyCode)).AsNoTracking();

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
            }

            if (gameProvidersFilter.Any() || liveCasinoProvidersFilter.Any() || virtualProvidersFilter.Any())
            {
                result = result.Where(r => sportsProductFilter.Contains(r.ProductId) ||
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                    var indirectUserId = accountsResult.ToList().Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();

                    //if (indirectUserId != null)
                    //{
                    //    indirectUserIds.AddRange(accountsResult.Where(a => a.Item1 <= indirectUserId.Item1 && a.Item3 == indirectUserId.Item3).Select(s => s.Item1).Distinct().ToList());
                    //}
                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.ToList().Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }


                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (status.Any())
            {
                result = result.Where(p => status.Contains(p.StatusName.ToUpper()));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                //result = result.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
                result = result.Where(p => betIds.Contains(p.BetId.ToString()));
            }

            return result;
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                    var indirectUserId = accountsResult.ToList().Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();
                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.ToList().Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }


                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                result = result.ToList().Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper()))).AsQueryable();
            }

            if (status.Any())
            {
                result = result.Where(p => status.Contains(p.StatusName.ToUpper()));
            }

            return result;
        }

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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                    var indirectUserId = accountsResult.ToList().Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();

                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.ToList().Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }

                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                result = result.ToList().Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper()))).AsQueryable();
            }

            if (status.Any())
            {
                result = result.Where(p => status.Contains(p.StatusName.ToUpper()));
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                //winLossRaw = winLossRaw.Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper())));
                winLossRaw = winLossRaw.Where(p => betIds.Contains(p.BetId.ToString()));
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                winLossRaw = winLossRaw.ToList().Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper()))).AsQueryable();
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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

                    var indirectUserId = accountsResult.ToList().Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();

                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.ToList().Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }

                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                result = result.ToList().Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper()))).AsQueryable();
            }

            if (status.Any())
            {
                result = result.Where(p => status.Contains(p.StatusName.ToUpper()));
            }

            return result;
        }

        private CasinoDetailDTO GetCasinoDetailForPL(string details, decimal exchangeRate)
        {
            if (string.IsNullOrEmpty(details))
                return null;

            var obj = JsonConvert.DeserializeObject<CasinoDetailDTO>(details);
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

        private IQueryable<WinLoss> GetWinLossAggregateQueryableParentAndAgentId2(long parentUserId, long directUserId, bool isAgent, WinLossAccountRequestExtendedDTO request)
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
               (r.ProductId == (int)Product.Casino && gameProvidersFilter.Contains(r.ProviderBrandId)) ||
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
                    var indirectUserId = accountsResult.ToList().Where(a => a.Item2.ToLower().Contains(account.Trim().ToLower())).ToList();
                    if (indirectUserId.Count > 0)
                    {
                        foreach (var item in indirectUserId)
                        {
                            indirectUserIds.AddRange(accountsResult.ToList().Where(a => a.Item1 <= item.Item1 && a.Item3 == item.Item3).Select(s => s.Item1).Distinct().ToList());
                        }
                    }
                }


                result = result.Where(r => indirectUserIds.Contains(r.IndirectUserId));
            }

            if (betIds.Any())
            {
                //result = result.Where(p => betIds.Select(s => s.ToUpper()).Contains(string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString().ToUpper() : p.ReferenceId.ToUpper()));
                result = result.ToList().Where(p => betIds.Any(p2 => (string.IsNullOrEmpty(p.ReferenceId) ? p.BetId.ToString() : p.ReferenceId).ToUpper().Contains(p2.Trim().ToUpper()))).AsQueryable();
            }

            if (status.Any())
            {
                result = result.Where(p => status.Contains(p.StatusName.ToUpper()));
            }

            return result;
        }

        private DateTime GetStartDate()
        {
            var startDate = new DateTime();

            switch (DateTime.UtcNow.AddHours(-4).DayOfWeek)
            {
                case DayOfWeek.Monday:
                    startDate = DateTime.UtcNow.AddHours(-4);
                    break;
                case DayOfWeek.Tuesday:
                    startDate = DateTime.UtcNow.AddHours(-4).AddDays(-1);
                    break;
                case DayOfWeek.Wednesday:
                    startDate = DateTime.UtcNow.AddHours(-4).AddDays(-2);
                    break;
                case DayOfWeek.Thursday:
                    startDate = DateTime.UtcNow.AddHours(-4).AddDays(-3);
                    break;
                case DayOfWeek.Friday:
                    startDate = DateTime.UtcNow.AddHours(-4).AddDays(-4);
                    break;
                case DayOfWeek.Saturday:
                    startDate = DateTime.UtcNow.AddHours(-4).AddDays(-5);
                    break;
                case DayOfWeek.Sunday:
                    startDate = DateTime.UtcNow.AddHours(-4).AddDays(-6);
                    break;
                default:
                    break;
            }

            return startDate.Date;
        }

        private DateTime GetEndDate(DateTime startDate)
        {
            var endDate = new DateTime();

            switch (startDate.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    endDate = startDate.AddDays(6);
                    break;
                case DayOfWeek.Tuesday:
                    endDate = startDate.AddDays(5);
                    break;
                case DayOfWeek.Wednesday:
                    endDate = startDate.AddDays(4);
                    break;
                case DayOfWeek.Thursday:
                    endDate = startDate.AddDays(3);
                    break;
                case DayOfWeek.Friday:
                    endDate = startDate.AddDays(2);
                    break;
                case DayOfWeek.Saturday:
                    endDate = startDate.AddDays(1);
                    break;
                case DayOfWeek.Sunday:
                    endDate = startDate;
                    break;
                default:
                    break;
            }

            return endDate.Date;
        }

        private DateTime GetFirstDayOfNextMonth()
        {
            DateTime ss = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            return ss.AddMonths(1);
        }

        private decimal GetTransferredAmount(decimal amount, TransactionType transactionType)
        {
            switch (transactionType)
            {
                //MDIO
                case TransactionType.Deposit:
                    return amount;
                case TransactionType.Withdrawal:
                    return amount * -1;
                case TransactionType.DebitPartialPaymentTransfer:
                case TransactionType.DebitFullPaymentTransfer:
                    return amount * -1;
                case TransactionType.CreditPartialPaymentTransfer:
                case TransactionType.CreditFullPaymentTransfer:
                    return amount * -1;
                default:
                    return amount;
            }
        }

        private string GetListAsString(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var newStr = string.Empty;
                var list = value.Split(',').ToList();
                foreach(var str in list)
                {
                    newStr = string.IsNullOrEmpty(newStr) ? $"\"{str}\"" :  $"{newStr},\"{str}\""; 
                }
                return newStr;
            }
            else
                return string.Empty;
        }
        #endregion
    }
}
