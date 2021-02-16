using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IClientRepository
    {
        Task<BaseResponseDTO<StatusCode>> InsertClient(ClientRequestDTO request);
        Task<BaseResponseDTO<StatusCode>> UpdateClient(ClientRequestDTO request);
    }
}
