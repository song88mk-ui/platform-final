using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.SignalR;
using Platform.Shared.Utility;
using Platform.Websocket.API2.Handlers;
using Platform.WebSocket.API2.Data;
using Platform.WebSocket.API2.Messages;
using System.Text.Json;

namespace Platform.WebSocket.API2.Repository
{
    public class WebSocketRepository : Hub<IPlatformEvent>, IWebSocketRepository
    {
        private readonly IServiceProvider _provider;
        private PlatformContext _platformContext;
        private readonly IMemCache _memCache;
        private readonly TelemetryClient _telemetryClient;



        public WebSocketRepository(IServiceProvider provider, IMemCache memCache, TelemetryClient telemetryClient)
        {
            _provider = provider;
            _memCache = memCache;
            _telemetryClient = telemetryClient;
        }
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var ipAddress = Context.GetHttpContext().Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? string.Empty;
            var ipCountry = Context.GetHttpContext().Request.Headers["CF-IPCountry"].FirstOrDefault() ?? string.Empty;
            var userAgent = Context.GetHttpContext().Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;

            var server = Environment.MachineName.ToLower();

            var properties = new Dictionary<string, string>
                            {
                                { "ConnectionId", connectionId },
                                { "IPAddress", ipAddress },
                                { "IPCountry", ipCountry },
                                { "Server", server },
                                { "UserAgent", userAgent },
                                { "ConnectedTimestamp", DateTime.UtcNow.ToString() }
                            };

            _telemetryClient.TrackEvent("e:OnConnectedAsync", properties);

            UserHandler.ConnectedIds.Add(connectionId);

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;

            var properties = new Dictionary<string, string>
                            {
                                { "ConnectionId", connectionId },
                                { "DisconnectedTimestamp", DateTime.UtcNow.ToString() }
                            };

            _telemetryClient.TrackEvent("e:OnDisconnectedAsync", properties);

            UserHandler.ConnectedIds.Remove(connectionId);

            await base.OnDisconnectedAsync(exception);
        }
        public async Task SubscribeToEndpoint(EndpointRequest request)
        {
            var connectionId = Context.ConnectionId;

            var properties = new Dictionary<string, string>
                            {
                                { "ConnectionId", connectionId },
                                { "requestParameters", JsonSerializer.Serialize(request ?? new EndpointRequest()) },
                                { "RequestTimestamp", DateTime.UtcNow.ToString() }
                            };

            _telemetryClient.TrackEvent("e:SubscribeToEndpoint", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                if (string.IsNullOrEmpty(request.ChannelKey) == false)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"endpoint:{client.Item2}:{request.ChannelKey}");
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetstopglobal:{client.Item2}");
                }
        }
        public async Task UnsubscribeToEndpoint(EndpointRequest request)
        {
            var connectionId = Context.ConnectionId;

            var properties = new Dictionary<string, string>
                            {
                                { "ConnectionId", connectionId },
                                { "requestParameters", JsonSerializer.Serialize(request ?? new EndpointRequest()) },
                                { "RequestTimestamp", DateTime.UtcNow.ToString() }
                            };

            _telemetryClient.TrackEvent("e:UnsubscribeToEndpoint", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                if (string.IsNullOrEmpty(request.ChannelKey) == false)
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"endpoint:{client.Item2}:{request.ChannelKey}");
        }
        public async Task SubscribeToBetslip(BetslipRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                var isLong = long.TryParse(request.FixtureId, out long fixtureId);

                if (isLong)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetslipoddschange:{client.Item2}:{fixtureId}");
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetslipbetstopfixture:{client.Item2}:{fixtureId}");
                }
            }
        }
        public async Task UnsubscribeToBetslip(BetslipRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                var isLong = long.TryParse(request.FixtureId, out long fixtureId);

                if (isLong)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onbetslipoddschange:{client.Item2}:{fixtureId}");
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onbetslipbetstopfixture:{client.Item2}:{fixtureId}");
                }
            }
        }
        public async Task SubscribeToBetStatus(BetStatusRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetstatus:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task UnsubscribeToBetStatus(BetStatusRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onbetstatus:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task SubscribeToCashOutChange(CashOutChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"oncashoutchange:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task UnsubscribeToCashOutChange(CashOutChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"oncashoutchange:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task SubscribeToWalletChange(WalletChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"onwalletchange:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task UnsubscribeToWalletChange(WalletChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onwalletchange:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task SubscribeToPayableChange(PayableChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"onpayablechange:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task UnsubscribeToPayableChange(PayableChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onpayablechange:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task SubscribeToFavorites(FavoritesRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"onfavorites:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task UnsubscribeToFavorites(WalletChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onfavorites:{client.Item2}:{request.UserId.ToLower()}");
        }
        public async Task PublishOddsChangeForMainMarkets(string clientCode, OutgoingOddsChange message)
        {
            await Clients.Groups(message.Gr).OnOddsChange(message.Pa);
        }
        public async Task PublishOddsChangeForAllMarkets(string clientCode, OutgoingOddsChange message)
        {
            await Clients.Group($"onbetslipoddschange:{clientCode}:{message.Pa.FiId}").OnBetslipOddsChange(message.Pa);
            await Clients.Groups(message.Gr).OnOddsChange(message.Pa);
        }
        public async Task PublishBetStopFixture(string clientCode, OutgoingBetStopFixture message)
        {
            await Clients.Group($"onbetslipbetstopfixture:{clientCode}:{message.Pa.FiId}").OnBetslipBetStopFixture(message.Pa);
            await Clients.Groups(message.Gr).OnBetStopFixture(message.Pa);
        }
        public async Task PublishBetStopGlobal(string clientCode, OutgoingBetStopGlobal message)
        {
            await Clients.Group($"onbetstopglobal:{clientCode}").OnBetStopGlobal(message);
        }
        public async Task PublishBetStatus(string clientCode, OutgoingBetStatus message)
        {
            await Clients.Group($"onbetstatus:{clientCode}:{message.UsId.ToLower()}").OnBetStatus(message);
        }
        public async Task PublishCashOutChange(string clientCode, OutgoingCashOutChange message)
        {
            await Clients.Group($"oncashoutchange:{clientCode}:{message.UsId.ToLower()}").OnCashOutChange(message);
        }
        public async Task PublishWalletChange(string clientCode, OutgoingWalletChange message)
        {
            await Clients.Group($"onwalletchange:{clientCode}:{message.UsId.ToLower()}").OnWalletChange(message);
        }
        public async Task PublishPayableChange(string clientCode, OutgoingPayableChange message)
        {
            await Clients.Group($"onpayablechange:{clientCode}:{message.UsId.ToLower()}").OnPayableChange(message);
        }
        private async Task<Tuple<bool, string>> GetClient(string clientId)
        {
            try
            {
                var key = $"cc:{clientId.ToUpper()}";
                var isGuid = Guid.TryParse(clientId, out Guid guid);

                if (isGuid)
                {
                    var cache = _memCache.GetCache<Tuple<bool, string>>(key);

                    if (cache != null)
                        return cache;

                    using var scope = _provider.CreateScope();

                    _platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

                    var client = await _platformContext.Client.FindAsync(guid);

                    if (client != null)
                    {
                        _memCache.AddOrUpdateCache(key, new Tuple<bool, string>(true, client.ClientCode.ToLower()), 24);
                        return new Tuple<bool, string>(true, client.ClientCode.ToLower());
                    }
                }

                _memCache.AddOrUpdateCache(key, new Tuple<bool, string>(false, string.Empty), DateTimeOffset.UtcNow.AddMinutes(1));
            }
            catch (Exception ex)
            {

            }

            return new Tuple<bool, string>(false, string.Empty);
        }
    }
    public class EndpointRequest
    {
        public string ClientId { get; set; }
        public string ChannelKey { get; set; }
    }

    public class BetslipRequest
    {
        public string ClientId { get; set; }
        public string FixtureId { get; set; }
    }

    public class BetStatusRequest
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
    }

    public class CashOutChangeRequest
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
    }

    public class WalletChangeRequest
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
    }
    public class FavoritesRequest
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
    }
    public class PayableChangeRequest
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
    }
}


