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
            //test
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

        [HttpPost("v2/sportsbook/allleagues")]
        public async Task<IActionResult> GetAllLeagues([FromBody] TopLeaguesRequest2DTO request)
        {
            var result = await _sportsbookRepository.GetAllLeagues2(request);

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

        [HttpPost("v1/sportsbook/topfixtures/fixtureId")]
        public async Task<IActionResult> GetTopFixtureIds([FromBody] TopFixturesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTopFixtureIds(request);

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

        [HttpPost("v1/sportsbook/userfavorites/get/all")]
        public async Task<IActionResult> GetUserFavorites2([FromBody] UserFavoriteRequestDTO request)
        {
            var result = await _sportsbookRepository.GetUserFavoritesAll(request);

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

        [HttpPost("v1/sportsbook/userfavorites/remove")]
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

        [HttpPost("v2/sportsbook/dailyfixtures")]
        public async Task<IActionResult> GetDailyFixtures([FromBody] TWDailyFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetDailyFixtures(request);

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

        [HttpPost("v2/sportsbook/topleaguefixtures")]
        public async Task<IActionResult> GetFixtures([FromBody] TWTopLeagueFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTopLeagueFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/segments/fixtures")]
        public async Task<IActionResult> GetSegmentFixtures([FromBody] TWSegmentFixtureRequestDTO request)
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

        [HttpPost("v2/sportsbook/fixtures")]
        public async Task<IActionResult> GetFixtures([FromBody] TWLeagueFixtureRequestDTO request)
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

        [HttpPost("v2/sportsbook/fixtures/details/limited")]
        public async Task<IActionResult> GetFixtureDetailsLimited([FromBody] TWFixtureDetailsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetFixtureDetailsLimited(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/fixtures/markets/selections/details")]
        public async Task<IActionResult> GetSelectionDetails([FromBody] TWFixtureMarketSelectionDetailsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetFixtureMarketSelectionDetails(request);

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

        [HttpPost("v1/sportsbook/search")]
        public async Task<IActionResult> GetSearchResult([FromBody] TWSearchRequestDTO request)
        {
            var result = await _sportsbookRepository.GetSearchResults(request);

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

        [HttpPost("v1/sportsbook/manualsettlement/won")]
        public async Task<IActionResult> ManualSettlementWon([FromBody] BetSettlementRequestDTO request)
        {
            foreach (InternalSelectionSettlementMessage row in request.Selections)
            {
                row.SettlementStatus = Shared.Enums.SettlementStatus.Won;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/manualsettlement/lost")]
        public async Task<IActionResult> ManualSettlementLost([FromBody] BetSettlementRequestDTO request)
        {
            foreach (InternalSelectionSettlementMessage row in request.Selections)
            {
                row.SettlementStatus = Shared.Enums.SettlementStatus.Lost;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("v1/sportsbook/manualsettlement/halfwon")]
        public async Task<IActionResult> ManualSettlementHalfWon([FromBody] BetSettlementRequestDTO request)
        {
            foreach (InternalSelectionSettlementMessage row in request.Selections)
            {
                row.SettlementStatus = Shared.Enums.SettlementStatus.HalfWon;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/manualsettlement/halflost")]
        public async Task<IActionResult> ManualSettlementHalfLost([FromBody] BetSettlementRequestDTO request)
        {
            foreach (InternalSelectionSettlementMessage row in request.Selections)
            {
                row.SettlementStatus = Shared.Enums.SettlementStatus.HalfLost;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        //[HttpPost("v1/sportsbook/manualsettlement/cancellation")]
        //public async Task<IActionResult> ManualCancellation([FromBody] BetCancellationRequestDTO request)
        //{
        //    foreach (InternalMarketCancellationMessage row in request.Markets)
        //    {
        //        row.SettlementStatus = Shared.Enums.SettlementStatus.Cancelled;
        //    }

        //    var result = await _sportsbookRepository.ManualCancellation(request);

        //    switch (result.StatusCode)
        //    {
        //        case Shared.Enums.StatusCode.Success:
        //            return Ok(result);
        //        default:
        //            return BadRequest(result);
        //    }
        //}

        [HttpPost("v1/sportsbook/manualsettlement/void")]
        public async Task<IActionResult> ManualSettlementVoid([FromBody] BetSettlementRequestDTO request)
        {
            foreach (InternalSelectionSettlementMessage row in request.Selections)
            {
                row.SettlementStatus = Shared.Enums.SettlementStatus.Void;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        /// <summary>
        /// Get list of all top fixtures per client ids that are included in the request.
        /// </summary>
        [HttpPost("v1/sportsbook/client/topfixtures")]
        public async Task<IActionResult> GetClientTopFixtures([FromBody] ClientTopFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientTopFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        /// <summary>
        /// Get list of sports along with the following: Segments, Leagues, and Fixtures. Doesn't include virtual sports and outrights. 
        /// </summary>
        [HttpPost("v1/sportsbook/sports/details")]
        public async Task<IActionResult> GetSportstreeDetails([FromBody] BackofficeSportsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetSportstreeDetails(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        /// <summary>
        /// Update top fixtures of a client.
        /// </summary>
        [HttpPost("v1/sportsbook/client/topfixtures/post")]
        public async Task<IActionResult> PostClientTopFixtures([FromBody] PostClientTopFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.PostClientTopFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        /// <summary>
        /// Get top leagues of a client ids.
        /// </summary>
        [HttpPost("v1/sportsbook/client/topleagues")]
        public async Task<IActionResult> GetClientTopLeagues([FromBody] ClientTopLeaguesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientIdsTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        /// <summary>
        /// Get list of sports, excluding virtual sport
        /// </summary>
        [HttpPost("v1/sportsbook/client/sports")]
        public async Task<IActionResult> GetClientSports([FromBody] SportsLookupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientSportsLookup();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        /// <summary>
        /// Add or update top leagues of a client.
        /// </summary>
        [HttpPost("v1/sportsbook/client/topleagues/post")]
        public async Task<IActionResult> AddorUpdateTopLeagues([FromBody] ClientTopLeague request)
        {
            var result = await _sportsbookRepository.AddOrUpdateTopLeagues(request);

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