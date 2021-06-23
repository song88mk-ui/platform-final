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

        [HttpPost("v1/bet/maxstake")]
        public async Task<IActionResult> GetMaxStake([FromBody] BetRequest request)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetMaxStake(apiKey, clientId, operatorToken, request);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetBetHistory(apiKey, clientId, languageCode, operatorToken, request);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.CashoutBet(apiKey, clientId, operatorToken, request);

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
        public async Task<IActionResult> GetTopLeagues([FromQuery] TopLeaguesRequest request)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopLeagues(apiKey, clientId, languageCode, request);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetAllLeagues(apiKey, clientId, languageCode, request);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopFixtureIds(apiKey, clientId, languageCode);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetUserFavorites2(apiKey, clientId, languageCode, operatorToken);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetUserFavorites(apiKey, clientId, languageCode, operatorToken);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopFixtures(apiKey, clientId, languageCode);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetLiveFixtureCount(apiKey, clientId, languageCode);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTodayFixtureCount(apiKey, clientId, languageCode, new TodayFixtureCountRequest { MinuteUTCOffset = minuteUTCOffset });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetLiveFixtures(apiKey, clientId, languageCode, new FixtureRequest { SportId = sportId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetLiveFixturesByLeague(apiKey, clientId, languageCode, new FixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTodayFixtures(apiKey, clientId, languageCode, new TodayFixtureRequest { SportId = sportId, MinuteUTCOffset = minuteUTCOffset });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTodayFixturesByLeague(apiKey, clientId, languageCode, new TodayFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId, MinuteUTCOffset = minuteUTCOffset });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetUpcomingFixtures(apiKey, clientId, languageCode, new UpcomingFixtureRequest { SportId = sportId, MinuteUTCOffset = minuteUTCOffset });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetDailyFixtures(apiKey, clientId, languageCode, new DailyFixtureRequest { SportId = sportId, DateUTC = dateUTC, MinuteUTCOffset = minuteUTCOffset });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetDailyFixturesByLeague(apiKey, clientId, languageCode, new DailyFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId, DateUTC = dateUTC, MinuteUTCOffset = minuteUTCOffset });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetOutrightFixtures(apiKey, clientId, languageCode, new OutrightFixtureRequest { SportId = sportId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetOutrightFixturesByLeague(apiKey, clientId, languageCode, new OutrightFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetTopLeagueFixtures(apiKey, clientId, languageCode, new TopLeagueFixtureRequest { SportId = sportId });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSegmentFixtures(apiKey, clientId, languageCode, new SegmentFixtureRequest { SportId = sportId, SegmentId = segmentId });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetFixtures(apiKey, clientId, languageCode, new LeagueFixtureRequest { SportId = sportId, SegmentId = segmentId, LeagueId = leagueId });

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/sportsbook/search")]
        public async Task<IActionResult> GetSearchResults([FromQuery] string searchText)
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSearchResults(apiKey, clientId, languageCode, new SearchResultRequest { SearchText = searchText});

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSports(apiKey, clientId, languageCode);

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetFixtureDetails(apiKey, clientId, languageCode, new FixtureDetailsRequest { FixtureId = fixtureId });

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
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.GetSelectionDetails(apiKey, clientId, languageCode, new FixtureMarketSelectionDetailsRequest { FixtureId = fixtureId, MarketId = marketId, SelectionId = selectionId });

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.AddUserFavorites(apiKey, clientId, languageCode, operatorToken, request);

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
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _platformRepository.DeleteUserFavorites(apiKey, clientId, languageCode, operatorToken, request);

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