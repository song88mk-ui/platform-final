using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Repository;
using Platform.Shared.Models;

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

        [HttpPost("v1/generateuser")]
        public async Task<IActionResult> AddUser([FromBody] GenerateUserRequestDTO request)
        {
            var result = await _platformRepository.AddUser(request);

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