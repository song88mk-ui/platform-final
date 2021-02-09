using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.Operator.API.Repository;
using Platform.Shared.Models;

namespace Platform.Operator.API.Controllers
{
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public IntegrationController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        [HttpPost("v1/authentication")]
        public async Task<IActionResult> Authenticate()
        {
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.Authenticate(operatorToken);

            switch (result.StatusId)
            {
                case 1002:
                    return new BadRequestObjectResult(result.StatusId);
                default:
                    return Ok(result);
            }
        }

        [HttpPost("v1/bet/placement")]
        public async Task<IActionResult> BetPlacement([FromBody] OperatorBetRequestDTO request)
        {
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.BetPlacement(operatorToken, request.Stake, request.CurrencyCode.ToString());

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result);
            }
        }
    }
}