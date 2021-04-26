using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Models;
using Platform.Shared.Utility;

namespace Platform.RabbitMQ.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : ControllerBase
    {
        private readonly IMessageBroker _messageBroker;

        public IntegrationController(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        [HttpPost("v1/broker/publish")]
        public async Task<IActionResult> PlaceBet([FromBody] MessageRequestDTO request)
        {
            try
            {
                _messageBroker.Publish(request.Payload, request.MessageType, request.RoutingKey, request.CorrelationId);

                return Ok(new BaseResponseDTO<MessageResponseDTO>
                {
                    StatusCode = Shared.Enums.StatusCode.Success,
                    Message = $"{Shared.Enums.StatusCode.Success.ToString()}",
                    Result = new MessageResponseDTO
                    {
                        IsSuccess = true
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new BaseResponseDTO<MessageResponseDTO>
                {
                    StatusCode = Shared.Enums.StatusCode.Error,
                    Message = $"{Shared.Enums.StatusCode.Error.ToString()}",
                    Result = new MessageResponseDTO
                    {
                        IsSuccess = false
                    }
                });
            }
        }
    }
}