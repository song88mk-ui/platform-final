using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.ClientPlatform.API.ActionFilters;
using Platform.ClientPlatform.API.Models;
using Platform.ClientPlatform.API.Repository;
using Platform.ClientPlatform.API.Telemetry;
using Platform.Shared.Models;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : IntegrationControllerBase
    {
        private readonly IPlatformRepository _platformRepository;
        private readonly ActivitySource _activitySource;

        public IntegrationController(IPlatformRepository platformRepository, Instrumentation instrumentation)
        {
            _platformRepository = platformRepository;
        }

        [HttpGet("v1/sportsbook/topfixtures")]
        public async Task<IActionResult> GetTopFixtures()
        {
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);
            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);

            var result = await _platformRepository.GetClientTopFixtures(ApiKey, ClientId, CountryCode, LanguageCode);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/activefixtures")]
        public async Task<IActionResult> GetActiveFixtures([FromQuery] ActiveFixturesFilterRequestDTO request)
        {
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);
            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);
            activity?.SetTag("request", JsonSerializer.Serialize(request));

            var result = await _platformRepository.GetClientActiveFixtures(ApiKey, ClientId, CountryCode, LanguageCode, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/activefixtures/details")]
        public async Task<IActionResult> GetFixtureDetails([FromQuery] string fixtureId)
        {
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);
            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);
            activity?.SetTag("fixture.id", fixtureId);

            var result = await _platformRepository.GetActiveFixtureDetails(ApiKey, ClientId, fixtureId, CountryCode, LanguageCode);

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
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);
            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);
            activity?.SetTag("operator.token", OperatorToken);
            activity?.SetTag("request", JsonSerializer.Serialize(request));

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
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);
            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);
            activity?.SetTag("ip.address", IPAddress);
            activity?.SetTag("request", JsonSerializer.Serialize(request));

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
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);
            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);
            activity?.SetTag("operator.token", OperatorToken);
            activity?.SetTag("ip.address", IPAddress);

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
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);

            var isSSR = HttpContext.Request.Headers["x-is-ssr"].FirstOrDefault();

            if (isSSR == null)
            {
                IPAddress = request.IPAddress;
            }

            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);
            activity?.SetTag("operator.token", OperatorToken);
            activity?.SetTag("ip.address", IPAddress);
            activity?.SetTag("is.ssr", isSSR);
            activity?.SetTag("request", JsonSerializer.Serialize(request));

            var result = await _platformRepository.CashoutBet(ApiKey, ClientId, OperatorToken, IPAddress, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/ccf")]
        public async Task<IActionResult> GetCCF([FromQuery] string userId)
        {
            using var activity = _activitySource.StartActivity(HttpContext.Request.Path);
            activity?.SetTag("client.id", ClientId);
            activity?.SetTag("country.code", CountryCode);
            activity?.SetTag("language.code", LanguageCode);
            activity?.SetTag("user.id", userId);
            activity?.SetTag("ip.address", IPAddress);

            var result = await _platformRepository.GetCustomerConfidenceFactor(ApiKey, ClientId, userId, IPAddress);

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
