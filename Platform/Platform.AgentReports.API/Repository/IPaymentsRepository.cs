using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public interface IPaymentsRepository
    {
        Task<BaseResponseDTO<PaymentTransferListResponse>> GetTransfers(PaymentTransferListRequest request);
        Task<BaseResponseDTO<PaymentTransferResponse>> Transfer(PaymentTransferRequest request);
        Task<BaseResponseDTO<long>> GGRSettlement(GGRSettlementRequest request);

    }
}
