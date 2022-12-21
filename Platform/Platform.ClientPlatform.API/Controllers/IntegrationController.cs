using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.ClientPlatform.API.ActionFilters;
using Platform.ClientPlatform.API.Models;
using Platform.ClientPlatform.API.Repository;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : IntegrationControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public IntegrationController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        [HttpGet("v1/sportsbook/topfixtures")]
        public async Task<IActionResult> GetTopFixtures()
        {
            var result = await _platformRepository.GetClientTopFixtures(ClientId, ApiKey, CountryCode, LanguageCode);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/bet/history")]
        public async Task<IActionResult> GetBetHistory([FromQuery] BetHistoryRequest request)
        {//
            var result = await _platformRepository.GetBetHistory(ApiKey, ClientId, LanguageCode, OperatorToken, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/bet/history/settled")]
        public async Task<IActionResult> GetBetHistorySettled([FromQuery] BetHistorySettledRequest request)
        {
            var result = await _platformRepository.GetBetHistorySettled(ApiKey, ClientId, LanguageCode, IPAddress, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/bet/history/running")]
        public async Task<IActionResult> GetBetHistoryRunning()
        {
            var result = await _platformRepository.GetBetHistoryRunning(ApiKey, ClientId, OperatorToken, IPAddress);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/cashout")]
        public async Task<IActionResult> CashoutBet([FromBody] CashoutBetRequest request)
        {
            var result = await _platformRepository.CashoutBet(ApiKey, ClientId, OperatorToken, IPAddress, request);

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
