using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Platform.ClientPlatform.API.Controllers
{
    [ApiController]
    public class ProbeController : ControllerBase
    {
        [HttpGet("v1/probe")]
        public IActionResult GetProbe()
        {
            return Ok();
        }
    }
}
