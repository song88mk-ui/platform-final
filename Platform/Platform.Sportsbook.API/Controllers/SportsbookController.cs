using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Models;
using Platform.Sportsbook.API.Repository;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class SportsbookController : ControllerBase
    {
        private readonly ISportsbookRepository _sportsbookRepository;

        public SportsbookController(ISportsbookRepository sportsbookRepository)
        {
            _sportsbookRepository = sportsbookRepository;
        }

        [HttpPost("v1/sportsbook/livestream")]
        public async Task<IActionResult> GetLivestream([FromBody] LivestreamRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLivestream(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/topleagues")]
        public async Task<IActionResult> GetTopLeagues([FromBody] TopLeaguesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/allleagues")]
        public async Task<IActionResult> GetAllLeagues([FromBody] TopLeaguesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetAllLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/topfixtures")]
        public async Task<IActionResult> GetTopFixtures([FromBody] TopFixturesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTopFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/orderedsports")]
        public async Task<IActionResult> GetOrderedSports([FromBody] OrderedSportsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetOrderedSports(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/orderedleagues")]
        public async Task<IActionResult> GetOrderedLeagues([FromBody] OrderedLeaguesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetOrderedLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/markettemplategroups")]
        public async Task<IActionResult> GetMarketTemplateGroups()
        {
            var result = await _sportsbookRepository.GetMarketTemplateGroups();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/betfixture")]
        public async Task<IActionResult> GetBetFixture([FromBody] BetFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetBetFixture(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/userfavorites/get")]
        public async Task<IActionResult> GetUserFavorites([FromBody] UserFavoriteRequestDTO request)
        {
            var result = await _sportsbookRepository.GetUserFavorites(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/userfavorites/insert")]
        public async Task<IActionResult> AddUserFavorites([FromBody] UserFavoriteInsertDeleteRequestDTO request)
        {
            var result = await _sportsbookRepository.AddFavorites(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/userfavorites/delete")]
        public async Task<IActionResult> DeleteUserFavorites([FromBody] UserFavoriteInsertDeleteRequestDTO request)
        {
            var result = await _sportsbookRepository.DeleteFavorites(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/topfixtures")]
        public async Task<IActionResult> GetTopFixturesTW([FromBody] TWRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTopFixturesTW(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/todayfixturecount")]
        public async Task<IActionResult> GetLiveFixtureCount([FromBody] TWFTodayFixtureCountRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTodayFixtureCount(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/livefixturecount")]
        public async Task<IActionResult> GetLiveFixtureCount([FromBody] TWFLiveFixtureCountRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLiveFixtureCount(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/livefixtures")]
        public async Task<IActionResult> GetLiveFixtures([FromBody] TWLiveFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLiveFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/todayfixtures")]
        public async Task<IActionResult> GetTodayFixtures([FromBody] TWTodayFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTodayFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/outrightfixtures")]
        public async Task<IActionResult> GetOutrightFixtures([FromBody] TWOutrightFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetOutrightFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/fixtures")]
        public async Task<IActionResult> GetFixtures([FromBody] TWFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/sports")]
        public async Task<IActionResult> GetSports([FromBody] SportsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetSports(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/fixtures/details")]
        public async Task<IActionResult> GetFixtureDetails([FromBody] TWFixtureDetailsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetFixtureDetails(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("v1/sportsbook/activefixtures")]
        public async Task<IActionResult> GetActiveFixtures([FromBody] ActiveFixturesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetActiveFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }



        [HttpPost("v1/sportsbook/activefixtures/count")]
        public async Task<IActionResult> GetActiveFixtureCount([FromBody] ActiveFixtureCountRequestDTO request)
        {
            var result = await _sportsbookRepository.GetActiveFixtureCount(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/lookup/sports")]
        public async Task<IActionResult> GetSportsLookup([FromBody] SportsLookupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetSportsLookup();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/lookup/segments")]
        public async Task<IActionResult> GetSegmentsLookup([FromBody] SegmentsLookupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetSegmentsLookup();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/lookup/leagues")]
        public async Task<IActionResult> GetLeaguesLookup([FromBody] LeaguesLookupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLeaguesLookup();

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