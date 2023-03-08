using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IReportsRepository
    {   
        Task<BaseResponseDTO<WinLossResponse>> GetWinLoss(long parentAgentId, bool isAgents, WinLossAccountRequest request);
        Task<BaseResponseDTO<WinLossResponse>> GetWinLoss(long parentAgentId, long directUserId, bool isAgents, WinLossAccountRequestExtended request);
        Task<BaseResponseDTO<WinLossDetailsResponse>> GetWinLossDetails(long parentAgentId, long directUserId, bool isAgents, WinLossAccountRequestExtended request);
        Task<BaseResponseDTO<TotalBetOtherResponse>> GetTotalBetsOthers(TotalBetsRequest request);
        Task<BaseResponseDTO<OutstandingBetResponse>> GetAllBets(SearchBetRequest request);
        Task<BaseResponseDTO<OutstandingBetResponse>> GetParlayBets(SearchBetRequest request);
        Task<BaseResponseDTO<TotalBetResponse>> GetTotalBets(TotalBetsRequest request);
        Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, ProfitLossRequest request);
        Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, long directUserId, ProfitLossRequest request);
        Task<BaseResponseDTO<WinLossDetailsResponse>> GetProfitLossDetails(long parentAgentId, long directUserId, ProfitLossRequest request);
        Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        Task<BaseResponseDTO<WinLossResponse>> GetProfitLoss(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        Task<BaseResponseDTO<WinLossDetailsResponse>> GetProfitLossDetails(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSiz, string currency, string status, string dateFilter);
        Task<BaseResponseDTO<GGRResponse>> GetGGR(long parentAgentId, long directUserId, WinLossAccountRequestExtendedDTO request);
        Task<BaseResponseDTO<GGRDetailsResponse>> GetGGRDetails(long agentId, long playerId, WinLossAccountRequestExtendedDTO request);
        Task<BaseResponseDTO<OutstandingBetFixtureResponse>> GetOutstandingBetFixtures(long agentId, int page, int pageSize, CurrencyCode currency);
        Task<BaseResponseDTO<List<MonthlyGrossGamingRevenueResponse>>> GetPartnerMonthlyGGR(long userId, int year, string currencyCode);
    }
}
