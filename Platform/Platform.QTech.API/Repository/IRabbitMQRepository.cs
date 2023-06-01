using Platform.Shared.Models;

namespace Platform.QTech.API.Repository
{
    public interface IRabbitMQRepository
    {
        BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request);
    }
}
