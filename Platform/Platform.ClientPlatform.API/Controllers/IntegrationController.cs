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

        [ServiceFilter(typeof(IPFilter))]
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
        {
            var result = await _platformRepository.GetBetHistory(ApiKey, ClientId, LanguageCode, OperatorToken, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [ServiceFilter(typeof(IPFilter))]
        [HttpGet("v1/settlement/history")]
        public async Task<IActionResult> GetSettlementHistory([FromQuery] SettlementHistoryRequest request)
        {
            var result = await _platformRepository.GetSettlementHistory(ApiKey, ClientId, LanguageCode, request);

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
            var result = await _platformRepository.CashoutBet(ApiKey, ClientId, OperatorToken, request);

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
