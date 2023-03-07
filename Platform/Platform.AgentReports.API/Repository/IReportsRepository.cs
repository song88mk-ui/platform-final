using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Request;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IReportsRepository
    {
        Task<BaseResponseDTO<OutstandingBetFixtureResponseDTO>> GetOutstandingBetFixtures(long agentId, int page, int pageSize, CurrencyCode currency); 
        Task<BaseResponseDTO<OutstandingBetResponseDTO>> GetAllBets(SearchBetRequest request);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLossV2(long parentAgentId, ProfitLossRequest request);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLossV2(long parentAgentId, long directUserId, ProfitLossRequest request);
        BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetailsV2(long parentAgentId, long directUserId, ProfitLossRequest request);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLoss(long parentAgentId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        BaseResponseDTO<WinLossResponseDTO> GetProfitLoss(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSize, string currency, string status, string dateFilter);
        BaseResponseDTO<WinLossDetailsResponse> GetProfitLossDetails(long parentAgentId, long directUserId, string product, DateTime fromDate, DateTime toDate, int page, int pageSiz, string currency, string status, string dateFilter);
        BaseResponseDTO<GGRResponseDTO> GetGGRV2(long parentAgentId, long directUserId, WinLossAccountRequestExtendedDTO request);
        BaseResponseDTO<GGRDetailsResponse> GetGGRDetailsV2(long agentId, long playerId, WinLossAccountRequestExtendedDTO request);
    }
}
