using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Client.API.ActionFilters;
using Platform.Client.API.Repository;
using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Client.API.Controllers
{
    [ApiController, ApiExplorerSettings(IgnoreApi = true), EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : ControllerBase
    {
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly TelemetryClient _telemetryClient;

        public IntegrationController(ISportsbookRepository sportsbookRepository, TelemetryClient telemetryClient)
        {
            _sportsbookRepository = sportsbookRepository;
            _telemetryClient = telemetryClient;
        }

        [HttpGet("v1/sportsbook/activefixtures")]
        public async Task<IActionResult> GetActiveFixtures()
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? LanguageCode.EN : Enum.Parse<LanguageCode>(HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper());

            var result = await _sportsbookRepository.GetActiveFixtures(apiKey, clientId, ipCountryCode, languageCode);

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-language-code", languageCode.ToString() },
                                { "requestParameters", JsonSerializer.Serialize("") },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
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

        [HttpGet("v1/sportsbook/activefixtures/details")]
        public async Task<IActionResult> GetFixtureDetails([FromQuery] string fixtureId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? LanguageCode.EN : Enum.Parse<LanguageCode>(HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper());

            var result = await _sportsbookRepository.GetActiveFixtureDetails(apiKey, clientId, fixtureId, ipCountryCode, languageCode);

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-language-code", languageCode.ToString() },
                                { "requestParameters", JsonSerializer.Serialize(fixtureId) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/activefixtures/details", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/activefixtures/count")]
        public async Task<IActionResult> GetActiveFixtureCount()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? LanguageCode.EN : Enum.Parse<LanguageCode>(HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper());

            var result = await _sportsbookRepository.GetActiveFixtureCount(apiKey, clientId, languageCode);

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-language-code", languageCode.ToString() },
                                { "requestParameters", JsonSerializer.Serialize("") },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/activefixtures/count", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/lookup/sports")]
        public async Task<IActionResult> GetSportsLookup()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? LanguageCode.EN : Enum.Parse<LanguageCode>(HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper());

            var result = await _sportsbookRepository.GetSportsLookup(apiKey, clientId, languageCode);

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-language-code", languageCode.ToString() },
                                { "requestParameters", JsonSerializer.Serialize("") },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/lookup/sports", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/lookup/segments")]
        public async Task<IActionResult> GetSegmentsLookup()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? LanguageCode.EN : Enum.Parse<LanguageCode>(HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper());

            var result = await _sportsbookRepository.GetSegmentsLookup(apiKey, clientId, languageCode);

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-language-code", languageCode.ToString() },
                                { "requestParameters", JsonSerializer.Serialize("") },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/lookup/segments", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/lookup/leagues")]
        public async Task<IActionResult> GetLeaguesLookup()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? LanguageCode.EN : Enum.Parse<LanguageCode>(HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper());

            var result = await _sportsbookRepository.GetLeaguesLookup(apiKey, clientId, languageCode);

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-language-code", languageCode.ToString() },
                                { "requestParameters", JsonSerializer.Serialize("") },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/lookup/leagues", properties);

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
            //
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            var result = await _sportsbookRepository.GetCustomerConfidenceFactor(apiKey, clientId, userId);

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "requestParameters", JsonSerializer.Serialize("") },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/ccf", properties);

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