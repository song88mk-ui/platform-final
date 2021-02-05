using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
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

        [HttpGet("v1/wallet/balance")]
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

        [HttpGet("v1/bet/config")]
        public async Task<IActionResult> GetBetConfig()
        {
            return Ok();
        }

        [HttpGet("v1/bet/history")]
        public async Task<IActionResult> GetBetHistory()
        {
            return Ok();
        }

        [HttpPost("v1/bet/placement")]
        public async Task<IActionResult> PlaceBet()
        {
            return Ok();
        }

        [HttpPost("v1/sportsbook/recovery")]
        public async Task<IActionResult> RecoverOdds()
        {
            return Ok();
        }
    }
}