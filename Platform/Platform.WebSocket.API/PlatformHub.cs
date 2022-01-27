using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Platform.WebSocket.API
{
    public class PlatformHub : Hub<IPlatformEvent>
    {
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            await base.OnDisconnectedAsync(exception);
        }
    }
}
