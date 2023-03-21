using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.AgentReports.API.ActionFilters;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Repository;
using Platform.Shared.Enums;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentsRepository _paymentsRepository;

        public PaymentController(IPaymentsRepository paymentsRepository)
        {
            _paymentsRepository = paymentsRepository;
        }

        [HttpPost("v1/payment/transfer/list")]
        public async Task<IActionResult> GetTransfers([FromBody] PaymentTransferListRequest request)
        {
            request.UserTypeId = request.UserTypeId == 10 ? (int)UserType.Agent : (int)UserType.Player;

            var result = await _paymentsRepository.GetTransfers(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/payment/transfer")]
        public async Task<IActionResult> Transfer([FromBody] PaymentTransferRequest request)
        {
            var result = await _paymentsRepository.Transfer(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
