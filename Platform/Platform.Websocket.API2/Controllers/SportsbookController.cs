using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Websocket.API2.Handlers;

namespace Platform.Websocket.API2.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class SportsbookController : ControllerBase
    {
        private readonly ClientPresenceTracker _presenceTracker;

        public SportsbookController(ClientPresenceTracker presenceTracker)
        {
            _presenceTracker = presenceTracker;
        }

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

        // Client codes with at least one connected subscriber. Polled by
        // OE.Websocket.Service to skip odds fan-out for clients with no audience (GM-720).
        [HttpGet("v1/activeclients")]
        public IActionResult GetActiveClients()
        {
            var clients = _presenceTracker.GetActiveClientCodes();

            return Ok(new
            {
                Clients = clients,
                Count = clients.Count
            });
        }
    }
}
