using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.ClientPlatform.API.ActionFilters;
using Platform.ClientPlatform.API.Repository;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : IntegrationControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public IntegrationController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        [HttpGet("v1/sportsbook/topfixtures")]
        public async Task<IActionResult> GetTopFixtures2()
        {
            var result = await _platformRepository.GetClientTopFixtures(ClientId, ApiKey, CountryCode, LanguageCode);

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
