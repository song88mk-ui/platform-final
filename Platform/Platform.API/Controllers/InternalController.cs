using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Models;
using Platform.API.Repository;
using Platform.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.API.Controllers
{
    [ApiController, ApiExplorerSettings(IgnoreApi = true), EnableCors("AllowAllOrigins"), ServiceFilter(typeof(InternalAPIKeyFilter))]
    public class InternalController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public InternalController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        [HttpPost("v1/client")]
        public async Task<IActionResult> AddOrUpdateClient([FromBody] ClientRequestDTO request)
        {
            var result = await _platformRepository.AddOrUpdateClient(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/status")]
        public IActionResult ProcessStatusUpdate([FromBody] ClientStatusRequestDTO request)
        {
            var response = _platformRepository.ProcessStatusUpdate(request);
            return Ok(response);
        }

        /// <summary>
        /// Get list of all top fixtures per client ids that are included in the request.
        /// </summary>
        [HttpPost("v1/client/topfixtures/list")]
        public async Task<IActionResult> GetClientTopFixtures([FromBody] ClientTopFixtureListRequest clientRefIds)
        {
            var result = await _platformRepository.GetClientTopFixtures(clientRefIds.ClientRefIds);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v2/client/topfixtures")]
        public async Task<IActionResult> GetClientTopFixtures2([FromQuery] string clientCodes)
        {
            var result = await _platformRepository.GetClientTopFixtures2(clientCodes);

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
        /// Get list of sports along with the following: Segments, Leagues, and Fixtures. Doesn't include virtual sports and outrights 
        /// </summary>
        [HttpGet("v1/client/topfixtures/sportstree")]
        public async Task<IActionResult> GetSportstree([FromQuery] int clientRefId)
        {
            var result = await _platformRepository.GetSportstreeDetails(clientRefId);

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
        [HttpPost("v1/client/topfixtures")]
        public async Task<IActionResult> PostClientTopFixtures([FromBody] ClientTopFixtureRequest request)
        {
            var result = await _platformRepository.PostClientTopFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/topfixtures/copy")]
        public async Task<IActionResult> CopyClientTopFixtures([FromBody] CopyClientTopFixtureRequest request)
        {
            var result = await _platformRepository.CopyClientTopFixtures(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        // Temporary Fix to provide backward compatibility for Old BO
        [HttpGet("v1/client/topleagues")]
        public async Task<IActionResult> TempGetClientTopLeagues([FromQuery] BackOfficeTopLeaguesRequestDTO request)
        {
            var result = await _platformRepository.GetClientTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/topleagues/list")]
        public async Task<IActionResult> GetClientTopLeagues([FromBody] BackOfficeTopLeaguesRequestDTO request)
        {
            var result = await _platformRepository.GetClientTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/topleagues")]
        public async Task<IActionResult> AddOrUpdateTopLeagues([FromBody] BackOfficeClientTopLeague request)
        {
            var result = await _platformRepository.AddOrUpdateTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/topleagues/copy")]
        public async Task<IActionResult> CopyClientTopLeagues([FromBody] BackOfficeCopyClientTopLeague request)
        {
            var result = await _platformRepository.CopyClientTopLeagues(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client/sports")]
        public async Task<IActionResult> GetClientSports([FromQuery] BackofficeClientSportsRequestDTO request)
        {
            var result = await _platformRepository.GetClientSportsLookup(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client/allleagues")]
        public async Task<IActionResult> GetClientAllLeagues([FromQuery] BackofficeClientAllLeaguesRequestDTO request2)
        {
            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var request = new TopLeaguesRequest
            {
                SportId = request2.SportId
            };

            var result = await _platformRepository.GetAllLeagues(string.Empty, request2.ClientIdRef.ToString(), languageCode, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v2/client/allleagues")]
        public async Task<IActionResult> GetClientAllLeagues2([FromQuery] BackofficeClientAllLeaguesRequest2DTO request2)
        {
            var request = new TopLeaguesRequest2
            {
                SportId = request2.SportId,
                LanguageCode = "EN",
                LeagueId = request2.LeagueId,
                SegmentId = request2.SegmentId
            };

            var result = await _platformRepository.GetAllLeagues2(string.Empty, request2.ClientIdRef.ToString(), request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client/exchangerate")]
        public async Task<IActionResult> GetExchangeRate([FromQuery] string currencyCode)
        {
            var result = await _platformRepository.GetCurrentExchangeRate(currencyCode);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client/exchangerates")]
        public async Task<IActionResult> GetExchangeRates()
        {
            var result = await _platformRepository.GetCurrentExchangeRates();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/config/client")]
        public async Task<IActionResult> ProcessBackOfficeClientBetConfig([FromBody] BackOfficeBetConfigPayloadDTO payload)
        {
            var result = await _platformRepository.ProcessBetConfig(payload);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/cancel")]
        public async Task<IActionResult> CancelBets([FromBody] BetModifyRequestDTO request)
        {
            var result = await _platformRepository.CancelBets(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/manualsettlement")]
        public async Task<IActionResult> ManualSettlement([FromBody] ManualBetSettlementRequest request)
        {
            var result = await _platformRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/details")]
        public async Task<IActionResult> GetManualBetDetails([FromBody] ManualBetDetailsRequest request)
        {
            var result = await _platformRepository.GetManualBetDetails(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/resendsettlement")]
        public async Task<IActionResult> ResendSettlement([FromBody] ResendSettlementRequestDTO request)
        {
            var result = await _platformRepository.ResendSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/resendsettlement/details")]
        public async Task<IActionResult> GetResendSettlementBetDetails([FromBody] ResendSettlementBetDetailRequestDTO request)
        {
            var result = await _platformRepository.GetResendSettlementBetDetails(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/parlaybonus")]
        public async Task<IActionResult> PostClientParlayBonus([FromBody] ParlayBonusRequestDTO request)
        {
            var result = await _platformRepository.AddOrUpdateParlayBonus(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client/parlaybonus")]
        public async Task<IActionResult> GetClientParlayBonus([FromQuery] GetParlayBonusRequestDTO request)
        {
            var result = await _platformRepository.GetClientParlayBonus(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client/sportgroups")]
        public async Task<IActionResult> GetSportGroups()
        {
            var result = await _platformRepository.GetSportGroups();

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
            var result = await _platformRepository.DeleteCachePreload(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client")]
        public async Task<IActionResult> GetClient([FromQuery] string clientId)
        {
            var result = await _platformRepository.GetClientInformation(clientId);

            return ReturnResult(result);
        }

        [HttpPost("v1/translation/recovery")]
        public async Task<IActionResult> TranslationRecovery([FromBody] TranslationRecoveryRequest request)
        {
            var result = await _platformRepository.TranslationRecovery(request);

            return ReturnResult(result);
        }


        [HttpPost("v1/client/announcements")]
        public async Task<IActionResult> GetAnnouncements([FromBody] BackofficeAnnouncementRequestDTO request)
        {
            var result = await _platformRepository.GetAnnouncements(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                case Shared.Enums.StatusCode.Maintenance:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

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
    }
}