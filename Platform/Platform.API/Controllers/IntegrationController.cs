using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Models;
using Platform.API.Repository;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;
        private readonly TelemetryClient _telemetryClient;

        public IntegrationController(IPlatformRepository platformRepository, TelemetryClient telemetryClient)
        {
            //
            _platformRepository = platformRepository;
            _telemetryClient = telemetryClient;
        }

        [HttpPost("v1/authentication/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var userAgent = HttpContext.Request.Headers["user-agent"].FirstOrDefault();
            var headers = new Dictionary<string, string>()
            {
                { "X-Original-User-IP", ipAddress ?? ""},
                { "X-Original-User-Agent", userAgent ?? ""},
                { "X-Original-User-Country", HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault() ?? ""}
            };

            var result = await _platformRepository.Login(clientId, request, headers);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "user-agent", userAgent },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/authentication/login", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v1/authentication/logout")]
        public async Task<IActionResult> Logout()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var userAgent = HttpContext.Request.Headers["user-agent"].FirstOrDefault();

            var result = await _platformRepository.Logout(clientId, operatorToken);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "user-agent", userAgent },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/authentication/logout", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/wallet/balance")]
        public async Task<IActionResult> GetWalletBalance()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetWalletBalance(clientId, operatorToken);

            var parameters = new
            {
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/wallet/balance", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/bet/placement")]
        public async Task<IActionResult> PlaceBet([FromBody] BetRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var userAgent = HttpContext.Request.Headers["user-agent"].FirstOrDefault();

            request.IPAddress = String.IsNullOrEmpty(request.IPAddress) ? ipAddress : request.IPAddress;

            var result = await _platformRepository.PlaceBet(apiKey, clientId, operatorToken, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "user-agent", userAgent },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v2/bet/placement", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/maxstake")]
        public async Task<IActionResult> GetMaxStake([FromBody] BetRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            request.IPAddress = String.IsNullOrEmpty(request.IPAddress) ? ipAddress : request.IPAddress;

            var result = await _platformRepository.GetMaxStake(apiKey, clientId, operatorToken, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/bet/maxstake", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/bet/config")]
        public async Task<IActionResult> GetBetConfig([FromQuery] BetConfigRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetBetConfig(apiKey, clientId, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/bet/config", properties);

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
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetBetHistory(apiKey, clientId, languageCode, operatorToken, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/bet/history", properties);

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
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetBetHistorySettled(apiKey, clientId, languageCode, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/bet/history/settled", properties);

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
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetBetHistoryRunning(clientId, languageCode, operatorToken);

            var parameters = new
            {
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/bet/history/running", properties);

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
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            request.IPAddress = String.IsNullOrEmpty(request.IPAddress) ? ipAddress : request.IPAddress;

            var result = await _platformRepository.CashoutBet(apiKey, clientId, operatorToken, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/bet/cashout", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/livestream")]
        public async Task<IActionResult> GetLivestream([FromQuery] LivestreamRequest request)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            request.IPAddress = String.IsNullOrEmpty(request.IPAddress) ? ipAddress : request.IPAddress;

            var result = await _platformRepository.GetLivestream(apiKey, clientId, operatorToken, ipCountryCode, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/livestream", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/topleagues")]
        public async Task<IActionResult> GetTopLeagues([FromQuery] TopLeaguesRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopLeagues(apiKey, clientId, languageCode, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/topleagues", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/allleagues")]
        public async Task<IActionResult> GetAllLeagues([FromQuery] TopLeaguesRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetAllLeagues(apiKey, clientId, languageCode, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/allleagues", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v2/sportsbook/topfixtures/fixtureId")]
        public async Task<IActionResult> GetTopFixtures()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopFixtureIds(apiKey, clientId, languageCode);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v2/sportsbook/topfixtures/fixtureId", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/markettemplategroups")]
        public async Task<IActionResult> GetMarketTemplateGroups()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetMarketTemplateGroups(apiKey, clientId);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/markettemplategroups", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/selectormarkettemplates")]
        public async Task<IActionResult> GetSelectorMarketTemplates()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetSelectorMarketTemplates(apiKey, clientId);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/markettemplategroups", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/marketsortorder")]
        public async Task<IActionResult> GetMarketSortOrder()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetMarketSortOrder(clientId);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/marketsortorder", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/fixturesortorder")]
        public async Task<IActionResult> GetFixtureSortOrder()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetFixtureSortOrder(clientId);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/fixturesortorder", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/userfavorites/fixtures")]
        public async Task<IActionResult> GetUserFavorites2()
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetUserFavorites2(apiKey, clientId, languageCode, operatorToken, ipCountryCode);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/userfavorites/fixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/userfavorites")]
        public async Task<IActionResult> GetUserFavorites()
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetUserFavorites(apiKey, clientId, languageCode, operatorToken, ipCountryCode);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/userfavorites|GET", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v2/sportsbook/topfixtures")]
        public async Task<IActionResult> GetTopFixtures2()
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopFixtures(apiKey, clientId, languageCode, ipCountryCode);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v2/sportsbook/topfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v2/sportsbook/activefixtures")]
        public async Task<IActionResult> GetActiveFixtures()
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetActiveFixtures(apiKey, clientId, languageCode, ipCountryCode);

            //var parameters = new { };

            //var properties = new Dictionary<string, string>
            //                {
            //                    { "ipCountryCode", ipCountryCode },
            //                    { "ipAddress", ipAddress },
            //                    { "x-api-key", apiKey },
            //                    { "x-client-id", clientId },
            //                    { "x-operator-token", string.Empty },
            //                    { "requestParameters", JsonSerializer.Serialize(parameters) },
            //                    { "statusCode", result.StatusCode.ToString() },
            //                    { "message", result.Message.ToString() }
            //                };

            //_telemetryClient.TrackEvent("e:v2/sportsbook/activefixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/livefixturecount")]
        public async Task<IActionResult> GetLiveFixtureCount([FromQuery] SportGroup sportGroupId)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetLiveFixtureCount(apiKey, clientId, languageCode, sportGroupId);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/livefixturecount", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/todayfixturecount")]
        public async Task<IActionResult> GetTodayFixtureCount([FromQuery] int minuteUTCOffset, SportGroup sportGroupId)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTodayFixtureCount(apiKey, clientId, languageCode, new TodayFixtureCountRequest { MinuteUTCOffset = minuteUTCOffset }, sportGroupId);

            var parameters = new
            {
                minuteUTCOffset
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/todayfixturecount", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/livefixtures")]
        public async Task<IActionResult> GetLiveFixtures([FromQuery] int sportId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetLiveFixtures(apiKey, clientId, languageCode, new FixtureRequest { SportId = sportId }, ipCountryCode);

            var parameters = new
            {
                sportId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/livefixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/segments/leagues/livefixtures")]
        public async Task<IActionResult> GetLiveFixturesByLeague([FromQuery] int sportId, int segmentId, int leagueId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetLiveFixturesByLeague(apiKey, clientId, languageCode, new FixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId }, ipCountryCode);

            var parameters = new
            {
                sportId,
                segmentId,
                leagueId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/segments/leagues/livefixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/todayfixtures")]
        public async Task<IActionResult> GetTodayFixtures([FromQuery] int sportId, int minuteUTCOffset)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTodayFixtures(apiKey, clientId, languageCode, new TodayFixtureRequest { SportId = sportId, MinuteUTCOffset = minuteUTCOffset }, ipCountryCode);

            var parameters = new
            {
                sportId,
                minuteUTCOffset
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/todayfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/segments/leagues/todayfixtures")]
        public async Task<IActionResult> GetTodayFixturesByLeague([FromQuery] int sportId, int segmentId, int leagueId, int minuteUTCOffset)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTodayFixturesByLeague(apiKey, clientId, languageCode, new TodayFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId, MinuteUTCOffset = minuteUTCOffset }, ipCountryCode);

            var parameters = new
            {
                sportId,
                segmentId,
                leagueId,
                minuteUTCOffset
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/segments/leagues/todayfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/upcomingfixtures")]
        public async Task<IActionResult> GetUpcomingFixtures([FromQuery] int sportId, int minuteUTCOffset)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetUpcomingFixtures(apiKey, clientId, languageCode, new UpcomingFixtureRequest { SportId = sportId, MinuteUTCOffset = minuteUTCOffset }, ipCountryCode);

            var parameters = new
            {
                sportId,
                minuteUTCOffset
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/upcomingfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/segments/leagues/upcomingfixtures")]
        public async Task<IActionResult> GetUpcomingFixturesByLeague([FromQuery] int sportId, int segmentId, int leagueId, int minuteUTCOffset)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetUpcomingFixturesByLeague(apiKey, clientId, languageCode, new UpcomingFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId, MinuteUTCOffset = minuteUTCOffset }, ipCountryCode);

            var parameters = new
            {
                sportId,
                segmentId,
                leagueId,
                minuteUTCOffset
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/segments/leagues/upcomingfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/dailyfixtures")]
        public async Task<IActionResult> GetDailyFixtures([FromQuery] int sportId, DateTime dateUTC, int minuteUTCOffset)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetDailyFixtures(apiKey, clientId, languageCode, new DailyFixtureRequest { SportId = sportId, DateUTC = dateUTC, MinuteUTCOffset = minuteUTCOffset }, ipCountryCode);

            var parameters = new
            {
                sportId,
                dateUTC,
                minuteUTCOffset
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/dailyfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/segments/leagues/dailyfixtures")]
        public async Task<IActionResult> GetDailyFixturesByLeague([FromQuery] int sportId, int segmentId, int leagueId, DateTime dateUTC, int minuteUTCOffset)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetDailyFixturesByLeague(apiKey, clientId, languageCode, new DailyFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId, DateUTC = dateUTC, MinuteUTCOffset = minuteUTCOffset }, ipCountryCode);

            var parameters = new
            {
                sportId,
                segmentId,
                leagueId,
                dateUTC,
                minuteUTCOffset
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/segments/leagues/dailyfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/outrightfixtures")]
        public async Task<IActionResult> GetOutrightFixtures([FromQuery] int sportId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetOutrightFixtures(apiKey, clientId, languageCode, new OutrightFixtureRequest { SportId = sportId }, ipCountryCode);

            var parameters = new
            {
                sportId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/outrightfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/segments/leagues/outrightfixtures")]
        public async Task<IActionResult> GetOutrightFixturesByLeague([FromQuery] int sportId, int segmentId, int leagueId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetOutrightFixturesByLeague(apiKey, clientId, languageCode, new OutrightFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId }, ipCountryCode);

            var parameters = new
            {
                sportId,
                segmentId,
                leagueId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/segments/leagues/outrightfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/topleaguefixtures")]
        public async Task<IActionResult> GetFixtures([FromQuery] int sportId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopLeagueFixtures(apiKey, clientId, languageCode, new TopLeagueFixtureRequest { SportId = sportId }, ipCountryCode);

            var parameters = new
            {
                sportId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/topleaguefixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/segments/fixtures")]
        public async Task<IActionResult> GetFixtures([FromQuery] int sportId, int segmentId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSegmentFixtures(apiKey, clientId, languageCode, new SegmentFixtureRequest { SportId = sportId, SegmentId = segmentId }, ipCountryCode);

            var parameters = new
            {
                sportId,
                segmentId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/segments/fixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/segments/leagues/fixtures")]
        public async Task<IActionResult> GetFixtures([FromQuery] int sportId, int segmentId, int leagueId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetFixtures(apiKey, clientId, languageCode, new LeagueFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId }, ipCountryCode);

            var parameters = new
            {
                sportId,
                segmentId,
                leagueId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports/segments/leagues/fixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/search")]
        public async Task<IActionResult> GetSearchResults([FromQuery] string searchText, SportGroup sportGroupId)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSearchResults(apiKey, clientId, languageCode, new SearchResultRequest { SearchText = searchText }, sportGroupId);

            var parameters = new
            {
                searchText
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/search", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports")]
        public async Task<IActionResult> GetSports([FromQuery] SportGroup sportGroup)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSports(apiKey, clientId, languageCode, sportGroup);

            var parameters = new
            {
                sportGroup
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/sports", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/fixturedetails")]
        public async Task<IActionResult> GetFixtureDetails([FromQuery] string fixtureId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetFixtureDetails(apiKey, clientId, languageCode, new FixtureDetailsRequest { FixtureId = fixtureId }, ipCountryCode);

            var parameters = new
            {
                fixtureId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/fixturedetails", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/selectiondetails")]
        public async Task<IActionResult> GetSelectionDetails([FromQuery] string fixtureId, string marketId, string selectionId)
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSelectionDetails(apiKey, clientId, languageCode, new FixtureMarketSelectionDetailsRequest { FixtureId = fixtureId, MarketId = marketId, SelectionId = selectionId }, ipCountryCode);

            var parameters = new
            {
                fixtureId,
                marketId,
                selectionId
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/selectiondetails", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/userfavorites")]
        public async Task<IActionResult> AddUserFavorites([FromQuery] InsertDeleteUserFavoritesRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.AddUserFavorites(apiKey, clientId, languageCode, operatorToken, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/userfavorites|POST", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpDelete("v1/sportsbook/userfavorites")]
        public async Task<IActionResult> DeleteUserFavorites([FromQuery] InsertDeleteUserFavoritesRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.DeleteUserFavorites(apiKey, clientId, languageCode, operatorToken, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", operatorToken },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/userfavorites|DELETE", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpGet("v1/sportsbook/boostedfixtures")]
        public async Task<IActionResult> GetBoostedFixtures()
        {
            var ipCountryCode = HttpContext.Request.Headers["x-country-code"].FirstOrDefault();
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetBoostedFixtures(apiKey, clientId, languageCode, ipCountryCode);

            var parameters = new
            {
            };

            var properties = new Dictionary<string, string>
                            {
                                { "ipCountryCode", ipCountryCode },
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/boostedfixtures", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/betbuilder/markettemplates")]
        public async Task<IActionResult> GetBetBuilderMarketTemplates()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetBetBuilderMarketTemplates(apiKey, clientId);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "x-operator-token", string.Empty },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/betbuilder/markettemplates", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("v1/sportsbook/betbuilder/selections/odds")]
        public async Task<IActionResult> GetBetBuilderSelectionsOdds([FromBody] BetBuilderSelectionsOddsRequest request)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetBetBuilderSelectionsOdds(clientId, languageCode, request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/betbuilder/selections/odds", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/client/markettemplateplayerprops")]
        public async Task<IActionResult> GetMarketTemplatePlayerProps([FromQuery] int sportId)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetMarketTemplatePlayerProps(sportId);

            var parameters = new { };

            var properties = new Dictionary<string, string>
                            {
                                { "ipAddress", ipAddress },
                                { "x-api-key", apiKey },
                                { "x-client-id", clientId },
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/sportsbook/client/markettemplateplayerprops", properties);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpGet("v1/casino")]
        public async Task<IActionResult> GetCasinoURL([FromQuery] bool isMobile = false)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = HttpContext.Request.Headers["x-language-code"].FirstOrDefault();

            var result = await _platformRepository.GetCasinoURL(clientId, operatorToken, languageCode, isMobile);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/livecasino")]
        public async Task<IActionResult> GetLiveCasinoURL([FromQuery] bool isMobile = false)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = HttpContext.Request.Headers["x-language-code"].FirstOrDefault();

            var result = await _platformRepository.GetLiveCasinoURL(clientId, operatorToken, languageCode, isMobile);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/virtualgames")]
        public async Task<IActionResult> GetVirtualGamesURL([FromQuery] bool isMobile = false)
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var languageCode = HttpContext.Request.Headers["x-language-code"].FirstOrDefault();

            var result = await _platformRepository.GetVirtualGames(clientId, operatorToken, languageCode, isMobile);

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