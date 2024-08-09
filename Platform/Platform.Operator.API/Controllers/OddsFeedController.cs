using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class OddsFeedController : ControllerBase
    {
        public OddsFeedController()
        {
                
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
            return Ok(new OddsFeedBetResponse
            {
                StatusId = 1000,
                Message = "Success"
            });
        }
    }
}