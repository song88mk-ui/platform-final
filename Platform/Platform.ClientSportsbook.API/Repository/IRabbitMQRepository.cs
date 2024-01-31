using Platform.Shared.Models;

namespace Platform.ClientSportsbook.API.Repository
{
    public interface IRabbitMQRepository
    {
        BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request);
    }
}
