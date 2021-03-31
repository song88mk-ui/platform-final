using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Client.API.Repository
{
    public interface IMTSCCFRepository
    {
        Task<BaseResponseDTO<CustomerConfidenceFactorResponseDTO>> GetCustomerConfidenceFactor(string authToken, string userId);
    }
}
