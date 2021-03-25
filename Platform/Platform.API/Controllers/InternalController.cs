using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Models;
using Platform.API.Repository;
using Platform.Shared.Models;
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
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/topfixtures/all")]
        public async Task<IActionResult> GetAllClientTopFixtures([FromBody] AllClientTopFixtureRequest request)
        {
            var result = await _platformRepository.GetClientTopFixturesByClientIds(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/client/topfixtures/{clientRefId}")]
        public async Task<IActionResult> GetClientTopFixturesByClientId(int clientRefId)
        {
            var result = await _platformRepository.GetClientTopFixtures(clientRefId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/client/topfixtures/sportstree")]
        public async Task<IActionResult> GetSportstreeByClientId([FromBody] SportsRequest request)
        {
            var result = await _platformRepository.GetSportstreeDetails(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("client/topfixtures/post")]
        public async Task<IActionResult> PostClientTopFixtures([FromBody] PostClientTopFixtureRequest request)
        {
            var result = await _platformRepository.PostClientTopFixtures(request);

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