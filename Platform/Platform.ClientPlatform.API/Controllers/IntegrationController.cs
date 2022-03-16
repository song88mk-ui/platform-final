using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.ClientPlatform.API.ActionFilters;
using Platform.ClientPlatform.API.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class IntegrationController : ControllerBase
    {
        private readonly ISportsbookRepository _sportsbookRepository;

        public IntegrationController(ISportsbookRepository sportsbookRepository)
        {
            _sportsbookRepository = sportsbookRepository;
        }

        [HttpGet("v1/sportsbook/topfixtures")]
        public async Task<IActionResult> GetTopFixtures2()
        {
            var apiKey = HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var languageCode = string.IsNullOrEmpty(HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();

            var result = await _sportsbookRepository.GetTopFixtures(clientId);

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
