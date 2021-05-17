using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
{
    public interface IRabbitMQRepository
    {
        Task<BaseResponseDTO<MessageResponseDTO>> Settlebet(MessageRequestDTO request);
    }
}
