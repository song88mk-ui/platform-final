using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Operator.API.Models;
using System.Threading.Tasks;

namespace Platform.Operator.API.Controllers
{
    [ApiController, Route("oddsfeed"), EnableCors("AllowAllOrigins")]
    public class OddsFeedController : ControllerBase
    {
        public OddsFeedController()
        {
                
        }

        [HttpPost("v1/bet/placement")]
        public async Task<IActionResult> BetPlacement([FromBody] OddsFeedBetRequest request)
        {
            return Ok();
        }
    }
}