using Microsoft.AspNetCore.SignalR;
using Platform.Shared.Utility;
using Platform.Websocket.API2.Handlers;
using Platform.WebSocket.API2.Data;
using Platform.WebSocket.API2.Messages;
using Platform.WebSocket.API2.Telemetry;
using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.WebSocket.API2.Repository
{
    public class WebSocketRepository : Hub<IPlatformEvent>, IWebSocketRepository
    {
        private PlatformContext _platformContext;
        private readonly IServiceProvider _provider;
        private readonly IMemCache _memCache;
        private readonly ILogger<WebSocketRepository> _logger;
        private readonly Counter<long> _eventCounter;
        private readonly ClientPresenceTracker _presenceTracker;

        public WebSocketRepository(IServiceProvider provider, IMemCache memCache, ILogger<WebSocketRepository> logger, Instrumentation instrumentation, ClientPresenceTracker presenceTracker)
        {
            _provider = provider;
            _memCache = memCache;
            _logger = logger;
            _eventCounter = instrumentation.EventCounter;
            _presenceTracker = presenceTracker;
        }

        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        public override async Task OnConnectedAsync()
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "OnConnected"));

            var connectionId = Context.ConnectionId;
            var xff = Context.GetHttpContext().Request.Headers["X-Forwarded-For"].ToString();
            var ipAddress = xff?.Split(',').Select(ip => ip.Trim()).FirstOrDefault() ?? GetClientIp();
            var cf_country = Context.GetHttpContext().Request.Headers["CF-IPCountry"].ToString();
            var ipCountry = Context.GetHttpContext().Request.Headers["CF-IPCountry"].FirstOrDefault() ?? string.Empty;
            var userAgent = Context.GetHttpContext().Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;

            var server = Environment.MachineName.ToLower();

            var properties = new Dictionary<string, string>
                            {
                                { "xff", xff },
                                { "ConnectionId", connectionId },
                                { "IPAddress", ipAddress },
                                { "CF-Country", cf_country },
                                { "IPCountry", ipCountry },
                                { "Server", server },
                                { "UserAgent", userAgent },
                                { "ConnectedTimestamp", DateTime.UtcNow.ToString() }
                            };

            _logger.LogInformation("{event} {@properties}", "OnConnectedAsync", properties);

            UserHandler.ConnectedIds.Add(connectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "OnDisconnected"));

            var connectionId = Context.ConnectionId;

            var properties = new Dictionary<string, string>
                            {
                                { "ConnectionId", connectionId },
                                { "DisconnectedTimestamp", DateTime.UtcNow.ToString() }
                            };

            _logger.LogInformation("{event} {@properties}", "OnDisconnectedAsync", properties);

            UserHandler.ConnectedIds.Remove(connectionId);

            _presenceTracker.TrackDisconnect(connectionId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeToEndpoint(EndpointRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToEndpoint"));

            var connectionId = Context.ConnectionId;

            var properties = new Dictionary<string, string>
                            {
                                { "ConnectionId", connectionId },
                                { "requestParameters", JsonSerializer.Serialize(request ?? new EndpointRequest()) },
                                { "RequestTimestamp", DateTime.UtcNow.ToString() }
                            };

            _logger.LogInformation("{event} {@properties}", "SubscribeToEndpoint", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                if (string.IsNullOrEmpty(request.ChannelKey) == false)
                {
                    _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                    await Groups.AddToGroupAsync(Context.ConnectionId, $"endpoint:{client.Item2}:{request.ChannelKey}");
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetstopglobal:{client.Item2}");
                }
        }

        public async Task UnsubscribeToEndpoint(EndpointRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToEndpoint"));

            var connectionId = Context.ConnectionId;

            var properties = new Dictionary<string, string>
                            {
                                { "ConnectionId", connectionId },
                                { "requestParameters", JsonSerializer.Serialize(request ?? new EndpointRequest()) },
                                { "RequestTimestamp", DateTime.UtcNow.ToString() }
                            };
            _logger.LogInformation("{event} {@properties}", "UnsubscribeToEndpoint", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                if (string.IsNullOrEmpty(request.ChannelKey) == false)
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"endpoint:{client.Item2}:{request.ChannelKey}");
        }

        public async Task SubscribeToBetslip(BetslipRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToBetslip"));

            var properties = new Dictionary<string, string>
            {
                { "requestParameters", JsonSerializer.Serialize(request) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogInformation("{event} {@properties}", "SubscribeToBetslip", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                var isLong = long.TryParse(request.FixtureId, out long fixtureId);

                if (isLong)
                {
                    _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                    await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetslipoddschange:{client.Item2}:{fixtureId}");
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetslipbetstopfixture:{client.Item2}:{fixtureId}");
                }
            }
        }

        public async Task UnsubscribeToBetslip(BetslipRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToBetslip"));

            var properties = new Dictionary<string, string>
            {
                { "requestParameters", JsonSerializer.Serialize(request) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogInformation("{event} {@properties}", "UnsubscribeToBetslip", properties);

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

        public async Task SubscribeToTopMarkets(BetslipRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToTopMarkets"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                var isLong = long.TryParse(request.FixtureId, out long fixtureId);

                if (isLong)
                {
                    _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                    await Groups.AddToGroupAsync(Context.ConnectionId, $"ontopmarketsoddschange:{client.Item2}:{fixtureId}");
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"ontopmarketsbetstopfixture:{client.Item2}:{fixtureId}");
                }
            }
        }

        public async Task UnsubscribeToTopMarkets(BetslipRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToTopMarkets"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                var isLong = long.TryParse(request.FixtureId, out long fixtureId);

                if (isLong)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ontopmarketsoddschange:{client.Item2}:{fixtureId}");
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ontopmarketsbetstopfixture:{client.Item2}:{fixtureId}");
                }
            }
        }

        public async Task SubscribeToBetStatus(BetStatusRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToBetStatus"));

            var properties = new Dictionary<string, string>
            {
                { "requestParameters", JsonSerializer.Serialize(request) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogInformation("{event} {@properties}", "SubscribeToBetStatus", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetstatus:{client.Item2}:{request.UserId.ToLower()}");
            }
        }

        public async Task UnsubscribeToBetStatus(BetStatusRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToBetStatus"));

            var properties = new Dictionary<string, string>
            {
                { "requestParameters", JsonSerializer.Serialize(request) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogInformation("{event} {@properties}", "UnsubscribeToBetStatus", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onbetstatus:{client.Item2}:{request.UserId.ToLower()}");
        }

        public async Task SubscribeToCashOutChange(CashOutChangeRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToCashOutChange"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                await Groups.AddToGroupAsync(Context.ConnectionId, $"oncashoutchange:{client.Item2}:{request.UserId.ToLower()}");
            }
        }

        public async Task UnsubscribeToCashOutChange(CashOutChangeRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToCashOutChange"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"oncashoutchange:{client.Item2}:{request.UserId.ToLower()}");
        }

        public async Task SubscribeToWalletChange(WalletChangeRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToWalletChange"));

            var properties = new Dictionary<string, string>
            {
                { "requestParameters", JsonSerializer.Serialize(request) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogInformation("{event} {@properties}", "SubscribeToWalletChange", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                await Groups.AddToGroupAsync(Context.ConnectionId, $"onwalletchange:{client.Item2}:{request.UserId.ToLower()}");
            }
        }

        public async Task UnsubscribeToWalletChange(WalletChangeRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToWalletChange"));

            var properties = new Dictionary<string, string>
            {
                { "requestParameters", JsonSerializer.Serialize(request) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogInformation("{event} {@properties}", "UnsubscribeToWalletChange", properties);

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onwalletchange:{client.Item2}:{request.UserId.ToLower()}");
        }

        public async Task SubscribeToPayableChange(PayableChangeRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToPayableChange"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                await Groups.AddToGroupAsync(Context.ConnectionId, $"onpayablechange:{client.Item2}:{request.UserId.ToLower()}");
            }
        }

        public async Task UnsubscribeToPayableChange(PayableChangeRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToPayableChange"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onpayablechange:{client.Item2}:{request.UserId.ToLower()}");
        }

        public async Task SubscribeToFavorites(FavoritesRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "SubscribeToFavorites"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
            {
                _presenceTracker.TrackSubscription(Context.ConnectionId, client.Item2);

                await Groups.AddToGroupAsync(Context.ConnectionId, $"onfavorites:{client.Item2}:{request.UserId.ToLower()}");
            }
        }

        public async Task UnsubscribeToFavorites(WalletChangeRequest request)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "UnsubscribeToFavorites"));

            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onfavorites:{client.Item2}:{request.UserId.ToLower()}");
        }

        public async Task PublishOddsChangeForMainMarkets(string clientCode, OutgoingOddsChange message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishOddsChangeForMainMarkets"));

            var properties = new Dictionary<string, string>
            {
                { "clientCode", clientCode },
                { "requestParameters", JsonSerializer.Serialize(message) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogDebug("{event} {@properties}", "PublishOddsChangeForMainMarkets", properties);

            await Clients.Groups(message.Gr).OnOddsChange(message.Pa);
        }

        public async Task PublishOddsChangeForAllMarkets(string clientCode, OutgoingOddsChange message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishOddsChangeForAllMarkets"));

            await Clients.Group($"onbetslipoddschange:{clientCode}:{message.Pa.FiId}").OnBetslipOddsChange(message.Pa);
            await Clients.Group($"ontopmarketsoddschange:{clientCode}:{message.Pa.FiId}").OnTopMarketsOddsChange(message.Pa);
            await Clients.Groups(message.Gr).OnOddsChange(message.Pa);
        }

        public async Task PublishBetStopFixture(string clientCode, OutgoingBetStopFixture message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishBetStopFixture"));

            var properties = new Dictionary<string, string>
            {
                { "clientCode", clientCode },
                { "requestParameters", JsonSerializer.Serialize(message) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogDebug("{event} {@properties}", "PublishBetStopFixture", properties);

            await Clients.Group($"onbetslipbetstopfixture:{clientCode}:{message.Pa.FiId}").OnBetslipBetStopFixture(message.Pa);
            await Clients.Group($"ontopmarketsbetstopfixture:{clientCode}:{message.Pa.FiId}").OnTopMarketsBetStopFixture(message.Pa);
            await Clients.Groups(message.Gr).OnBetStopFixture(message.Pa);
        }

        public async Task PublishBetStopGlobal(string clientCode, OutgoingBetStopGlobal message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishBetStopGlobal"));

            var properties = new Dictionary<string, string>
            {
                { "clientCode", clientCode },
                { "requestParameters", JsonSerializer.Serialize(message) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogDebug("{event} {@properties}", "PublishBetStopGlobal", properties);

            await Clients.Group($"onbetstopglobal:{clientCode}").OnBetStopGlobal(message);
        }

        public async Task PublishBetStatus(string clientCode, OutgoingBetStatus message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishBetStatus"));

            var properties = new Dictionary<string, string>
            {
                { "clientCode", clientCode },
                { "requestParameters", JsonSerializer.Serialize(message) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogDebug("{event} {@properties}", "PublishBetStatus", properties);

            await Clients.Group($"onbetstatus:{clientCode}:{message.UsId.ToLower()}").OnBetStatus(message);
        }

        public async Task PublishCashOutChange(string clientCode, OutgoingCashOutChange message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishCashOutChange"));

            await Clients.Group($"oncashoutchange:{clientCode}:{message.UsId.ToLower()}").OnCashOutChange(message);
        }

        public async Task PublishWalletChange(string clientCode, OutgoingWalletChange message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishWalletChange"));

            var properties = new Dictionary<string, string>
            {
                { "clientCode", clientCode },
                { "requestParameters", JsonSerializer.Serialize(message) },
                { "RequestTimestamp", DateTime.UtcNow.ToString() }
            };

            _logger.LogDebug("{event} {@properties}", "PublishWalletChange", properties);
            _logger.LogInformation("Sending to {target}", $"onwalletchange:{clientCode}:{message.UsId.ToLower()}");

            await Clients.Group($"onwalletchange:{clientCode}:{message.UsId.ToLower()}").OnWalletChange(message);
        }

        public async Task PublishPayableChange(string clientCode, OutgoingPayableChange message)
        {
            if (_eventCounter.Enabled)
                _eventCounter.Add(1, new KeyValuePair<string, object?>("event", "PublishPayableChange"));

            _logger.LogInformation("Sending to {target}", $"onpayablechange:{clientCode}:{message.UsId.ToLower()}");

            await Clients.Group($"onpayablechange:{clientCode}:{message.UsId.ToLower()}").OnPayableChange(message);
        }

        public string? GetClientIp()
        {
            return Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
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
                    {
                        _logger.LogInformation($"GetClient: from cache {cache.Item1}-{cache.Item2}");
                        return cache;

                    }

                    using var scope = _provider.CreateScope();

                    _platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

                    var client = await _platformContext.Client.FindAsync(guid);

                    if (client != null)
                    {
                        _logger.LogInformation($"GetClient: {client.ClientCode}-{client.ClientId}");

                        _memCache.AddOrUpdateCache(key, new Tuple<bool, string>(true, client.ClientCode.ToLower()), 24);
                        return new Tuple<bool, string>(true, client.ClientCode.ToLower());
                    }
                }

                _memCache.AddOrUpdateCache(key, new Tuple<bool, string>(false, string.Empty), DateTimeOffset.UtcNow.AddMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error on GetClient: {message} {@stacktrace}", ex.Message, ex.StackTrace);
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
