using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IRabbitMQRepository
    {
        Task<BaseResponseDTO<MessageResponseDTO>> Settlebet(MessageRequestDTO request);
    }
}
