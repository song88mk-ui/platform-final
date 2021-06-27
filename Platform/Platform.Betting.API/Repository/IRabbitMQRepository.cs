using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IRabbitMQRepository
    {
        BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request);
    }
}
