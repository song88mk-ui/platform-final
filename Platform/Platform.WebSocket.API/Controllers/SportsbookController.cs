using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Platform.WebSocket.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class SportsbookController : ControllerBase
    {
        [HttpGet("v1/heartbeat")]
        public IActionResult Get()
        {//
            return Ok();
        }
    }
}