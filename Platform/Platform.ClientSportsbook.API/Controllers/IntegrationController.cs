using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.ClientSportsbook.API.ActionFilters;
using Platform.ClientSportsbook.API.Models;
using Platform.ClientSportsbook.API.Repository;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.ClientSportsbook.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class IntegrationController : IntegrationControllerBase
    {
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly TelemetryClient _telemetryClient;
        public IntegrationController(ISportsbookRepository platformRepository, TelemetryClient telemetryClient)
        {
            _sportsbookRepository = platformRepository;
            _telemetryClient = telemetryClient;
        }

        [HttpGet("v1/sportsbook/activefixtures")]
        public async Task<IActionResult> GetActiveFixtures([FromQuery] ActiveFixtureRequest request = null)
        {
            var parameters = request;

            var result = await _sportsbookRepository.GetClientActiveFixtures(ClientKeyFilter, request);

            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ClientKeyFilter.IpAddress },
                                { "x-api-key",  apiKey},
                                { "x-client-id", ClientKeyFilter.ClientId.ToString() },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/activefixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("v1/sportsbook/activefixtures/details/{fixtureId}")]
        public async Task<IActionResult> GetActiveFixtureDetails([FromRoute] string fixtureId)
        {
            var parameters = new { };

            var result = await _sportsbookRepository.GetActiveFixtureDetails(ClientKeyFilter, fixtureId);

            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ClientKeyFilter.IpAddress },
                                { "x-api-key",  apiKey},
                                { "x-client-id", ClientKeyFilter.ClientId.ToString() },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent($"e:v1/sportsbook/activefixtures/details/{fixtureId}", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v1/sportsbook/market/subscribe")]
        public async Task<IActionResult> SubscribeToMarkets([FromBody] MarketsSubscriptionRequestDTO request)
        {
            var parameters = request;

            var result = await _sportsbookRepository.SubscribeToMarkets(ClientKeyFilter.ClientCode, request);

            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ClientKeyFilter.IpAddress },
                                { "x-api-key",  apiKey},
                                { "x-client-id", ClientKeyFilter.ClientId.ToString() },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/market/subscribe", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v1/sportsbook/market/unsubscribe")]
        public async Task<IActionResult> UnsubscribeToMarkets([FromBody] MarketsSubscriptionRequestDTO request)
        {
            var parameters = request;

            var result = await _sportsbookRepository.UnsubscribeToMarkets(ClientKeyFilter.ClientCode, request);

            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ClientKeyFilter.IpAddress },
                                { "x-api-key",  apiKey},
                                { "x-client-id", ClientKeyFilter.ClientId.ToString() },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/market/unsubscribe", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("v1/sportsbook/market/subscription")]
        public async Task<IActionResult> GetMarketsSubscription()
        {
            var parameters = new { ClientKeyFilter.LanguageCode };

            var result = await _sportsbookRepository.GetMarketsSubscription(ClientKeyFilter);

            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ClientKeyFilter.IpAddress },
                                { "x-api-key",  apiKey},
                                { "x-client-id", ClientKeyFilter.ClientId.ToString() },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/market/subscription", properties);

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
