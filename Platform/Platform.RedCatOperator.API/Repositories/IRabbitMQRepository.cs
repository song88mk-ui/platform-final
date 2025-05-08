using Platform.Shared.Models;

namespace Platform.RedCatOperator.API.Repositories
{
    public interface IRabbitMQRepository
    {
        BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request);
    }
}
