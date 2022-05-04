using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Utility;
using Platform.WebSocket.API.Data;
using Platform.WebSocket.API.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.WebSocket.API.Repository
{
    public class WebSocketRepository : Hub<IPlatformEvent>, IWebSocketRepository
    {
        private readonly IServiceProvider _provider;
        private PlatformContext _platformContext;

        public WebSocketRepository(IServiceProvider provider)
        {
            _provider = provider;
        }
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var ipAddress = Context.GetHttpContext().Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? string.Empty;
            var ipCountry = Context.GetHttpContext().Request.Headers["CF-IPCountry"].FirstOrDefault() ?? string.Empty;
            var userAgent = Context.GetHttpContext().Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;

            var server = Environment.MachineName.ToLower();

            using var scope = _provider.CreateScope();
            _platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

            await _platformContext.WebSocket.AddAsync(new Data.WebSocket
            {
                ConnectionId = connectionId,
                IPAddress = ipAddress,
                IPCountry = ipCountry,
                Server = server,
                UserAgent = userAgent,
                ClientCode = string.Empty,
                RequestURL = string.Empty,
                ConnectedTimestamp = DateTime.UtcNow,
                DisconnectedTimestamp = null
            });

            await _platformContext.SaveChangesAsync();

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;

            using var scope = _provider.CreateScope();
            _platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

            var test = Context.GetHttpContext().Request.Query;

            var connection = await _platformContext.WebSocket.FindAsync(connectionId);

            if (connection != null)
            {
                connection.DisconnectedTimestamp = DateTime.UtcNow;

                _platformContext.WebSocket.Update(connection);

                await _platformContext.SaveChangesAsync();
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task SubscribeToEndpoint(EndpointRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"{client.Item2}");
                //await Groups.AddToGroupAsync(Context.ConnectionId, $"{client.Item2}:{request.Endpoint.ToLower()}");

                //using var scope = _provider.CreateScope();

                //_platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

                //var webSocket = await _platformContext.WebSocket.FindAsync(Context.ConnectionId);

                //if (webSocket != null)
                //{
                //    webSocket.ClientCode = client.Item2.ToUpper();
                //    webSocket.RequestURL = string.IsNullOrEmpty(request.RequestURL) ? string.Empty : request.RequestURL;

                //    _platformContext.WebSocket.Update(webSocket);

                //    await _platformContext.SaveChangesAsync();
                //}
            }
        }
        public async Task SubscribeToBetslip(BetslipRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                var isLong = long.TryParse(request.FixtureId, out long fixtureId);

                if (isLong)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetslipoddschange:{client.Item2}:{fixtureId}");
            }
        }
        public async Task UnsubscribeToBetslip(BetslipRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                var isLong = long.TryParse(request.FixtureId, out long fixtureId);

                if (isLong)
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onbetslipoddschange:{client.Item2}:{fixtureId}");
            }
        }
        public async Task PublishOddsChange(string clientCode, OutgoingOddsChange message)
        {
            await Clients.Group($"onbetslipoddschange:{clientCode}:{message.FiId}").OnBetslipOddsChange(message);

            await Clients.Groups(new List<string>
            {
                $"{clientCode}"
            }.AsReadOnly()).OnOddsChange(message);
        }
        public async Task PublishBetStopFixture(string clientCode, OutgoingBetStopFixture message)
        {
            await Clients.Group($"onbetslipbetstopfixture:{clientCode}:{message.FiId}").OnBetslipBetStopFixture(message);

            await Clients.Groups(new List<string>
            {
                $"{clientCode}"
            }.AsReadOnly()).OnBetStopFixture(message);
        }
        public async Task PublishBetStopGlobal(string clientCode, OutgoingBetStopGlobal message)
        {
            await Clients.Group($"{clientCode}").OnBetStopGlobal(message);
        }
        private async Task<Tuple<bool, string>> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid)
            {
                using var scope = _provider.CreateScope();
                _platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

                var client = await _platformContext.Client.FindAsync(guid);

                if (client != null)
                    return new Tuple<bool, string>(true, client.ClientCode.ToLower());
            }

            return new Tuple<bool, string>(false, string.Empty);
        }
    }
    public class EndpointRequest
    {
        public string ClientId { get; set; }
        public string RequestURL { get; set; }
    }

    public class BetslipRequest
    {
        public string ClientId { get; set; }
        public string FixtureId { get; set; }
    }
}


