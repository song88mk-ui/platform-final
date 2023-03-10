using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Operator.API.Models;
using Platform.Operator.API.Repository;
using Platform.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Operator.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class OperatorController : Controller
    {
        private readonly IOperatorRepository _operatorRepository;
        public OperatorController(IOperatorRepository operatorRepository)
        {//
            _operatorRepository = operatorRepository;
        }

        [HttpPost("operator/v1/authentication/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var result = await _operatorRepository.Login(new LoginRequestDTO { UserName = request.UserName, Password = request.Password });

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result.StatusId);
            }
        }
        [HttpPost("operator/v1/authentication/logout")]
        public async Task<IActionResult> Logout()
        {
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _operatorRepository.Logout(operatorToken);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result.StatusId);
            }
        }

        [HttpPost("operator/v1/authentication")]
        public async Task<IActionResult> Authenticate()
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var isAgent = HttpContext.Request.Headers["x-is-agent"].FirstOrDefault();

            var result = await _operatorRepository.Authenticate(new AuthRequestDTO { ClientId = clientId, OperatorToken = operatorToken, IsAgent = isAgent });

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result.StatusId);
            }
        }

        [HttpPost("operator/v1/bet/placement")]
        public async Task<IActionResult> BetPlacement([FromBody] OperatorBetRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var isAgent = HttpContext.Request.Headers["x-is-agent"].FirstOrDefault();

            var result = await _operatorRepository.BetPlacement(request, clientId, operatorToken, isAgent);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("operator/v1/bet/settlement")]
        public async Task<IActionResult> BetSettlement([FromBody] OperatorBetSettlementRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _operatorRepository.BetSettlement(request, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("operator/v1/bet/full/cashout")]
        public async Task<IActionResult> CashOutBet([FromBody] OperatorBetSettlementRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var isAgent = HttpContext.Request.Headers["x-is-agent"].FirstOrDefault();

            var result = await _operatorRepository.CashOutBet(request, clientId, operatorToken, isAgent);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("operator/v1/bet/cancellation")]
        public async Task<IActionResult> BetCancellation([FromBody] OperatorBetCancellationRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _operatorRepository.BetCancellation(request, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
