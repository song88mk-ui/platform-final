using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Models;
using Platform.API.Repository;

namespace Platform.API.Controllers
{
    [ApiController, ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public IntegrationController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        [HttpPost("v1/wallet/balance")]
        public async Task<IActionResult> GetWalletBalance()
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetWalletBalance(clientId, operatorToken);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/config")]
        public async Task<IActionResult> GetBetConfig()
        {
            return Ok();
        }

        [HttpPost("v1/bet/history")]
        public async Task<IActionResult> GetBetHistory([FromBody] BetHistoryRequest request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetBetHistory(clientId, operatorToken, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/placement")]
        public async Task<IActionResult> PlaceBet([FromBody] BetRequest request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();

            var result = await _platformRepository.PlaceBet(apiKey,clientId, operatorToken, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/recovery")]
        public async Task<IActionResult> RecoverOdds()
        {
            return Ok();
        }
    }
}