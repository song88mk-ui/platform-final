using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Websocket.API2.Handlers;

namespace Platform.Websocket.API2.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class SportsbookController : ControllerBase
    {
        [HttpGet("v1/heartbeat")]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpGet("v1/connections")]
        public IActionResult GetConnections()
        {
            //
            var result = new
            {
                Connections = UserHandler.ConnectedIds.ToList(),
                Count = UserHandler.ConnectedIds.Count
            };

            return Ok(result);
        }
    }
}
