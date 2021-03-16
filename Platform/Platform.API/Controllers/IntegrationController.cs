using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Models;
using Platform.API.Repository;
using Platform.Shared.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
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

        [HttpPost("v2/bet/placement")]
        public async Task<IActionResult> PlaceBet([FromBody] BetRequest request)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.PlaceBet(apiKey, clientId, operatorToken, request);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetBetConfig(apiKey, clientId, request);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetBetHistory(apiKey, clientId, operatorToken, request);

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

        [HttpGet("v1/sportsbook/livestream")]
        public async Task<IActionResult> GetLivestream([FromQuery] LivestreamRequest request)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetLivestream(apiKey, clientId, operatorToken, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/topleagues")]
        public async Task<IActionResult> GetTopLeagues([FromQuery] TempTopLeaguesRequest request2)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var request = new TopLeaguesRequest
            {
                SportId = request2.SportId,
                LanguageCode = "EN"
            };

            var result = await _platformRepository.GetTopLeagues(apiKey, clientId, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports/allleagues")]
        public async Task<IActionResult> GetAllLeagues([FromQuery] TempTopLeaguesRequest request2)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var request = new TopLeaguesRequest
            {
                SportId = request2.SportId,
                LanguageCode = "EN"
            };

            var result = await _platformRepository.GetAllLeagues(apiKey, clientId, request);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var request = new TopFixturesRequest
            {
                LanguageCode = "EN"
            };

            var result = await _platformRepository.GetTopFixtureIds(apiKey, clientId, request);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetMarketTemplateGroups(apiKey, clientId);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetUserFavorites2(apiKey, clientId, operatorToken);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetUserFavorites(apiKey, clientId, operatorToken);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetTopFixtures2(apiKey, clientId, new TopFixturesRequest { LanguageCode = LanguageCode.EN.ToString() });

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/livefixturecount")]
        public async Task<IActionResult> GetLiveFixtureCount()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetLiveFixtureCount(apiKey, clientId, new FixtureCountRequest { LanguageCode = LanguageCode.EN.ToString() });

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/todayfixturecount")]
        public async Task<IActionResult> GetTodayFixtureCount([FromQuery] int minuteUTCOffset)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetTodayFixtureCount(apiKey, clientId, new TodayFixtureCountRequest { LanguageCode = LanguageCode.EN.ToString(), MinuteUTCOffset = minuteUTCOffset });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetLiveFixtures(apiKey, clientId, new FixtureRequest { LanguageCode = LanguageCode.EN.ToString(), SportId = sportId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetTodayFixtures(apiKey, clientId, new TodayFixtureRequest { LanguageCode = LanguageCode.EN.ToString(), SportId = sportId, MinuteUTCOffset = minuteUTCOffset });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetDailyFixtures(apiKey, clientId, new DailyFixtureRequest { LanguageCode = LanguageCode.EN.ToString(), SportId = sportId, DateUTC = dateUTC, MinuteUTCOffset = minuteUTCOffset });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetOutrightFixtures(apiKey, clientId, new OutrightFixtureRequest { LanguageCode = LanguageCode.EN.ToString(), SportId = sportId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetTopLeagueFixtures(apiKey, clientId, new TopLeagueFixtureRequest { LanguageCode = LanguageCode.EN.ToString(), SportId = sportId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetSegmentFixtures(apiKey, clientId, new SegmentFixtureRequest { LanguageCode = LanguageCode.EN.ToString(), SportId = sportId, SegmentId = segmentId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetFixtures(apiKey, clientId, new LeagueFixtureRequest { LanguageCode = LanguageCode.EN.ToString(), SportId = sportId, SegmentId = segmentId, LeagueId = leagueId });

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/search")]
        public async Task<IActionResult> GetSearchResults([FromQuery] string searchText, string languageCode)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetSearchResults(apiKey, clientId, new SearchResultRequest { LanguageCode = languageCode, SearchText = searchText});

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/sports")]
        public async Task<IActionResult> GetSports()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetSports(apiKey, clientId, new SportRequest { LanguageCode = LanguageCode.EN.ToString() });

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
            //force deploy
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.GetFixtureDetails(apiKey, clientId, new FixtureDetailsRequest { LanguageCode = LanguageCode.EN.ToString(), FixtureId = fixtureId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.AddUserFavorites(apiKey, clientId, operatorToken, request);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.DeleteUserFavorites(apiKey, clientId, operatorToken, request);

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