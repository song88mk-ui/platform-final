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
        {
            _operatorRepository = operatorRepository;
        }

        [HttpPost("operator/v1/authentication/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.IpAddress))
                request.IpAddress = GetRemoteIpAddress().Trim();
            if (string.IsNullOrEmpty(request.Country))
                request.Country = GetIpCountry().Trim();
            if (string.IsNullOrEmpty(request.UserAgent))
                request.UserAgent = HttpContext.Request.Headers["user-agent"].FirstOrDefault() ?? "";

            var result = await _operatorRepository.Login(new LoginRequestDTO
            {
                UserName = request.UserName,
                Password = request.Password,
                UserAgent = request.UserAgent,
                IpAddress = request.IpAddress,
                Country = request.Country
            });

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

        #region Utilities
        private string GetIpCountry()
        {
            return HttpContext.Request.Headers["X-Original-User-Country"].FirstOrDefault() ?? "";
        }
        private string GetRemoteIpAddress()
        {
            var ip = HttpContext.Request.Headers["X-Original-User-IP"].FirstOrDefault();

            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return ip.Split(':').FirstOrDefault().Split(",").FirstOrDefault();
        }
        #endregion
    }
}
