using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Client.API.ActionFilters;
using Platform.Client.API.Repository;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Client.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : ControllerBase
    {
        private readonly ISportsbookRepository _sportsbookRepository;

        public IntegrationController(ISportsbookRepository sportsbookRepository)
        {
            _sportsbookRepository = sportsbookRepository;
        }

        [HttpGet("v1/sportsbook/activefixtures")]
        public async Task<IActionResult> GetTopFixtures()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _sportsbookRepository.GetActiveFixtures(apiKey, clientId, LanguageCode.EN);

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

            var result = await _sportsbookRepository.GetSportsLookup(apiKey, clientId, LanguageCode.EN);

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

            var result = await _sportsbookRepository.GetSegmentsLookup(apiKey, clientId, LanguageCode.EN);

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

            var result = await _sportsbookRepository.GetLeaguesLookup(apiKey, clientId, LanguageCode.EN);

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