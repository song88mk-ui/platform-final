using Platform.Shared.Models;

namespace Platform.Oddin.API.Repository
{
    public interface IRabbitMQRepository
    {
        BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request);
    }
}
