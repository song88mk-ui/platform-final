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
        Task<BaseResponseDTO<OutstandingBetResponseDTO>> GetAllBets(SearchBetRequest request);
        Task<BaseResponseDTO<OutstandingBetResponseDTO>> GetParlayBets(SearchBetRequest request);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLossV2(long parentAgentId, ProfitLossRequest request);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLossV2(long parentAgentId, long directUserId, ProfitLossRequest request);
        BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetailsV2(long parentAgentId, long directUserId, ProfitLossRequest request);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLoss(long parentAgentId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLoss(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetails(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSiz, string currency, string status, string dateFilter);
        BaseResponseDTO<GGRResponseDTO> GetGGRV2(long parentAgentId, long directUserId, WinLossAccountRequestExtendedDTO request);
        BaseResponseDTO<GGRDetailsResponse> GetGGRDetailsV2(long agentId, long playerId, WinLossAccountRequestExtendedDTO request);
        Task<BaseResponseDTO<OutstandingBetFixtureResponseDTO>> GetOutstandingBetFixtures(long agentId, int page, int pageSize, CurrencyCode currency);
        Task<BaseResponseDTO<List<MonthlyGrossGamingRevenueResponse>>> GetPartnerMonthlyGGR(long userId, int year, string currencyCode);
    }
}
