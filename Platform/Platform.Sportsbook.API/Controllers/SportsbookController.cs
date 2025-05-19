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
            //
            _sportsbookRepository = sportsbookRepository;
        }

        [HttpPost("v1/sportsbook/livestream")]
        public async Task<IActionResult> GetLivestream([FromBody] LivestreamRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLivestream(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/selectormarkettemplates")]
        public async Task<IActionResult> GetSelectorMarketTemplates([FromBody] SelectorMarketTemplatesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientSelectorMarketTemplates(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/betselectionprobability")]
        public async Task<IActionResult> GetBetFixture([FromBody] BetSelectionProbabilityRequestDTO request)
        {
            var result = await _sportsbookRepository.GetBetSelectionProbabilities(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/betfixture/translation")]
        public async Task<IActionResult> GetBetFixtureTranslation([FromBody] BetFixtureTranslationRequestDTO request)
        {
            var result = await _sportsbookRepository.GetBetFixtureTranslation(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/sports/segments/leagues/livefixtures")]
        public async Task<IActionResult> GetLiveFixturesByLeague([FromBody] TWLeagueLiveFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLiveFixturesByLeague(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/sports/segments/leagues/todayfixtures")]
        public async Task<IActionResult> GetTodayFixturesByLeague([FromBody] TWLeagueTodayFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTodayFixturesByLeague(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/upcomingfixtures")]
        public async Task<IActionResult> GetUpcomingFixtures([FromBody] TWUpcomingFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetUpcomingFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/sports/segments/leagues/upcomingfixtures")]
        public async Task<IActionResult> GetUpcomingFixturesByLeague([FromBody] TWLeagueUpcomingFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetUpcomingFixturesByLeague(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/sports/segments/leagues/dailyfixtures")]
        public async Task<IActionResult> GetDailyFixturesByLeague([FromBody] TWLeagueDailyFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetDailyFixturesByLeague(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/sports/segments/leagues/outrightfixtures")]
        public async Task<IActionResult> GetOutrightFixturesByLeague([FromBody] TWLeagueOutrightFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.GetOutrightFixturesByLeague(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/marketsortorder")]
        public async Task<IActionResult> GetMarketSortOrder([FromBody] MarketSortOrderRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientMarketSortOrder(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v1/sportsbook/fixturesortorder")]
        public async Task<IActionResult> GetFixtureSortOrder([FromBody] FixtureSortOrderRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientFixtureSortOrder(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/lookup/sports")]
        public async Task<IActionResult> GetSportsLookup([FromBody] SportsLookupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetSportsLookup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/lookup/segments")]
        public async Task<IActionResult> GetSegmentsLookup([FromBody] SegmentsLookupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetSegmentsLookup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/lookup/leagues")]
        public async Task<IActionResult> GetLeaguesLookup([FromBody] LeaguesLookupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLeaguesLookup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                row.SelectionStatus = Shared.Enums.BetStatus.Won;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                row.SelectionStatus = Shared.Enums.BetStatus.Lost;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                row.SelectionStatus = Shared.Enums.BetStatus.HalfWon;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                row.SelectionStatus = Shared.Enums.BetStatus.HalfLost;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
        //        case Shared.Enums.StatusCode.Success: case Shared.Enums.StatusCode.Maintenance:
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
                row.SelectionStatus = Shared.Enums.BetStatus.Void;
            }

            var result = await _sportsbookRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        /// <summary>
        /// Get list of all top fixtures per client ids that are included in the request.
        /// </summary>
        [HttpPost("v2/sportsbook/client/topfixtures")]
        public async Task<IActionResult> GetClientTopFixtures2([FromBody] ClientTopFixtureRequestDTO2 request)
        {
            var result = await _sportsbookRepository.GetClientTopFixtures2(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("v1/sportsbook/client/topfixtures/copy")]
        public async Task<IActionResult> CopyClientTopFixtures([FromBody] CopyClientTopFixtureRequestDTO request)
        {
            var result = await _sportsbookRepository.CopyClientTopFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
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
                case Shared.Enums.StatusCode.Maintenance:
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
            var result = await _sportsbookRepository.GetClientSportsLookup(request.ClientCode);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        /// <summary>
        /// Add or update top leagues of a client.
        /// </summary>
        [HttpPost("v1/sportsbook/client/topleagues/post")]
        public async Task<IActionResult> AddorUpdateTopLeagues([FromBody] ClientTopLeagueDTO request)
        {
            var result = await _sportsbookRepository.AddOrUpdateTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/topleagues/copy")]
        public async Task<IActionResult> CopyClientTopLeagues([FromBody] CopyClientTopLeagueRequestDTO request)
        {
            var result = await _sportsbookRepository.CopyClientTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/sportgroups")]
        public async Task<IActionResult> GetSportGroups()
        {
            var result = await _sportsbookRepository.GetSportGroups();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/boostedfixtures")]
        public async Task<IActionResult> GetBoostedFixtures([FromBody] BoostedFixturesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetBoostedFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/betsettlementscore")]
        public async Task<IActionResult> GetBetSettlementScore([FromBody] BetSettlementScoreRequestDTO request)
        {
            var result = await _sportsbookRepository.GetBetSettlementScore(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/betbuilder/markettemplates")]
        public async Task<IActionResult> GetBetBuilderMarketTemplates()
        {
            var result = await _sportsbookRepository.GetBetBuilderMarketTemplatesDTO();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/betbuilder/fixtures/totalodds")]
        public async Task<IActionResult> GetBetBuilderSelectionsOdds([FromBody] BetBuilderSelectionsOddsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetBetBuilderSelectionsOdds(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/markettemplategroups")]
        public async Task<IActionResult> GetClientMarketTemplateGroups([FromBody] ClientMarketTemplateGroupRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientMarketTemplateGroups(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/markettemplates")]
        public async Task<IActionResult> GetClientMarketTemplates([FromBody] ClientMarketTemplateRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientMarketTemplates(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/markettemplatesorder")]
        public async Task<IActionResult> AddOrUpdateClientMarketTemplatesOrder([FromBody] ClientMarketTemplateRequestDTO request)
        {
            var result = await _sportsbookRepository.AddOrUpdateClientMarketTemplatesOrder(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/add/markettemplategroup")]
        public async Task<IActionResult> AddClientMarketTemplateGroup([FromBody] ClientAddMarketTemplateGroupRequestDTO request)
        {
            var result = await _sportsbookRepository.AddClientMarketTemplateGroup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("v1/sportsbook/client/markettemplategroup/post")]
        public async Task<IActionResult> AddOrUpdateClientMarketTemplateGroup([FromBody] ClientPostMarketTemplateGroupRequestDTO request)
        {
            var result = await _sportsbookRepository.AddOrUpdateClientMarketTemplateGroup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/markettemplategroup/delete")]
        public async Task<IActionResult> DeleteClientMarketTemplateGroup([FromBody] ClientDeleteMarketTemplateGroupRequestDTO request)
        {
            var result = await _sportsbookRepository.DeleteClientMarketTemplateGroup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/markettemplategroup/copy")]
        public async Task<IActionResult> CopyClientMarketTemplateGroup([FromBody] ClientCopyMarketTemplateGroupRequestDTO request)
        {
            var result = await _sportsbookRepository.CopyClientMarketTemplateGroup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/client/markettemplateplayerprops")]
        public async Task<IActionResult> GetMarketTemplatePlayerProps([FromBody] MarketTemplatePlayerPropsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetMarketTemplatePlayerProps(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/cachepreload")]
        public async Task<IActionResult> CachePreload([FromBody] CachePreloadRequestDTO request)
        {
            var result = await _sportsbookRepository.DeleteCachePreload(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/translations")]
        public async Task<IActionResult> GetTranslationByLanguageCode([FromBody] TranslationRequestDTO request)
        {
            var result = await _sportsbookRepository.GetTranslationByLanguageCode(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/highlights")]
        public async Task<IActionResult> GetLeagueHighlights([FromBody] LeagueHighlightsRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLeagueHighlights(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v2/sportsbook/highlights/fixtures")]
        public async Task<IActionResult> GetLeagueHighlightsFixtures([FromBody] LeagueHighlightsFixturesRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLeagueHighlightsFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/sportsbook/betrecommendations")]
        public async Task<IActionResult> GetBetRecommendations([FromBody] BetRecommendationDetailRequestDTO request)
        {
            var result = await _sportsbookRepository.GetBetRecommendationDetails(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/sportsbook/translation/recovery")]
        public async Task<IActionResult> TranslationRecovery([FromBody] TranslationRecoveryRequestDTO request)
        {
            var result = await _sportsbookRepository.TranslationRecovery(request);

            return ReturnResult(result);
        }

        [HttpPost("v1/sportsbook/announcements")]
        public async Task<IActionResult> GetAnnouncements([FromBody] AnnouncementRequestDTO request)
        {
            var result = await _sportsbookRepository.GetAnnouncements(request);

            return ReturnResult(result);
        }

        [HttpPost("v1/sportsbook/client/announcements")]
        public async Task<IActionResult> GetClientAnnouncements([FromBody] BackofficeAnnouncementRequestDTO request)
        {
            var result = await _sportsbookRepository.GetAnnouncements(request);

            return ReturnResult(result);
        }

        [HttpPost("v1/sportsbook/client/announcements/post")]
        public async Task<IActionResult> AddOrUpdateClientAnnouncements([FromBody] AddOrUpdateBackofficeAnnouncementRequestDTO request)
        {
            var result = await _sportsbookRepository.AddOrUpdateAnnouncements(request);

            return ReturnResult(result);
        }

        [HttpPost("v1/sportsbook/client/announcements/delete")]
        public async Task<IActionResult> DeleteClientAnnouncements([FromBody] DeleteBackofficeAnnouncementDTO request)
        {
            var result = await _sportsbookRepository.DeleteAnnouncements(request);

            return ReturnResult(result);
        }
        [HttpGet("v1/sportsbook/client/asian")]
        public async Task<IActionResult> GetAsianClients()
        {
            var result = await _sportsbookRepository.GetClientsWithouMargin();

            return ReturnResult(result);
        }

        #region Utilities

        private IActionResult ReturnResult<T>(BaseResponseDTO<T> result)
        {
            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #endregion
    }
}