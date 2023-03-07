using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IReportsRepository
    {
        Task<BaseResponseDTO<OutstandingBetFixtureResponse>> GetOutstandingBetFixtures(long agentId, int page, int pageSize, CurrencyCode currency);
        BaseResponseDTO<WinLossResponse> GetProfitLossV2(long parentAgentId, ProfitLossRequest request);
        BaseResponseDTO<WinLossResponse> GetProfitLossV2(long parentAgentId, long directUserId, ProfitLossRequest request);
        BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetailsV2(long parentAgentId, long directUserId, ProfitLossRequest request);
        BaseResponseDTO<WinLossResponse> GetProfitLoss(long parentAgentId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        BaseResponseDTO<WinLossResponse> GetProfitLoss(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetails(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSiz, string currency, string status, string dateFilter);
        BaseResponseDTO<GGRResponse> GetGGRV2(long parentAgentId, long directUserId, WinLossAccountRequestExtended request);
        BaseResponseDTO<GGRDetailsResponse> GetGGRDetailsV2(long agentId, long playerId, WinLossAccountRequestExtended request);
    }
}
