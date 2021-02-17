using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Repository;

namespace Platform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(InternalAPIKeyFilter))]
    public class InternalController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public InternalController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        [HttpPost("v1/client")]
        public async Task<IActionResult> AddOrUpdateClient()
        {
            var clientId = HttpContext.Request.Headers["x-api-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetWalletBalance(clientId, operatorToken);

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