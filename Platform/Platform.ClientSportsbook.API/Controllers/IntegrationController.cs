using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.ClientSportsbook.API.Repository;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.ClientSportsbook.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : IntegrationControllerBase
    {
        private readonly ISportsbookRepository _sportsbookRepository;
        public IntegrationController(ISportsbookRepository platformRepository)
        {
            _sportsbookRepository = platformRepository;
        }

        [HttpPost("v1/sportsbook/activefixtures")]
        public async Task<IActionResult> GetActiveFixtures([FromBody] ActiveFixturesFilterRequestDTO request)
        {
            var result = await _sportsbookRepository.GetClientActiveFixtures(ClientKeyFilter);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v1/sportsbook/activefixtures/details")]
        public async Task<IActionResult> GetActiveFixtureDeatils([FromQuery] string fixtureId)
        {
            var result = await _sportsbookRepository.GetActiveFixtureDetails(ClientKeyFilter, fixtureId);

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
