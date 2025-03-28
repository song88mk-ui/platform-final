using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Operator.API.Models;
using Platform.Shared.Data.Platform;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Operator.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class OddsFeedController : ControllerBase
    {
        private readonly PlatformContext _platformContext;

        public OddsFeedController(PlatformContext platformContext)
        {
            _platformContext = platformContext;
        }

        [HttpPost("bet/status/placement")]
        public async Task<IActionResult> BetStatusPlacement([FromBody] OddsFeedBetRequest request)
        {
            return Ok(new OddsFeedBetResponse
            {
                StatusId = 1000,
                Message = "Success"
            });
        }

        [HttpPost("bet/status/cancellation")]
        public async Task<IActionResult> BetStatusCancellation([FromBody] OddsFeedBetCancellationRequest request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            Guid.TryParse(clientId, out var finalClientId);

            var res = _platformContext.NegativeTest.Find(finalClientId);

            if (res != null && res.ClientId == finalClientId)
            {
                return BadRequest(new OddsFeedBetResponse
                {
                    StatusId = 1002
                });
            }


            return Ok(new OddsFeedBetResponse
            {
                StatusId = 1000,
                Message = "Success"
            });
        }
    }
}