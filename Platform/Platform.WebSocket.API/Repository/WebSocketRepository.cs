using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Enums;
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
                if (string.IsNullOrEmpty(request.ChannelKey) == false)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"endpoint:{client.Item2}:{request.ChannelKey}");
                else
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"{client.Item2}");
            }
        }
        public async Task UnsubscribeToEndpoint(EndpointRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"endpoint:{client.Item2}:{request.ChannelKey}");
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
        public async Task SubscribeToBetStatus(BetStatusRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"onbetstatus:{client.Item2}:{request.UserId}");
        }
        public async Task UnsubscribeToBetStatus(BetStatusRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onbetstatus:{client.Item2}:{request.UserId}");
        }
        public async Task SubscribeToCashOutChange(CashOutChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"oncashoutchange:{client.Item2}:{request.UserId}");
        }
        public async Task UnsubscribeToCashOutChange(CashOutChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"oncashoutchange:{client.Item2}:{request.UserId}");
        }
        public async Task SubscribeToWalletChange(WalletChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"onwalletchange:{client.Item2}:{request.UserId}");
        }
        public async Task UnsubscribeToWalletChange(WalletChangeRequest request)
        {
            var client = await GetClient(request.ClientId);

            if (client.Item1 == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"onwalletchange:{client.Item2}:{request.UserId}");
        }
        public async Task PublishOddsChange(string clientCode, OutgoingOddsChange message)
        {
            await Clients.Group($"onbetslipoddschange:{clientCode}:{message.FiId}").OnBetslipOddsChange(message);
            await Clients.Groups(GetGroups(clientCode, message).AsReadOnly()).OnOddsChange(message);
        }
        public async Task PublishOddsChangeDelta(string clientCode, OutgoingOddsChangeDelta message)
        {
            await Clients.Group($"onbetslipoddschange:{clientCode}:{message.FiId}").OnBetslipOddsChange(message);
            await Clients.Groups(GetGroups(clientCode, message).AsReadOnly()).OnOddsChange(message);
        }
        public async Task PublishBetStopFixture(string clientCode, OutgoingBetStopFixture message)
        {
            await Clients.All.OnBetStopFixture(message);
        }
        public async Task PublishBetStopGlobal(string clientCode, OutgoingBetStopGlobal message)
        {
            await Clients.All.OnBetStopGlobal(message);
        }
        public async Task PublishBetStatus(string clientCode, OutgoingBetStatus message)
        {
            await Clients.Group($"{clientCode}:{message.UsId}").OnBetStatus(message);
        }
        public async Task PublishCashOutChange(string clientCode, OutgoingCashOutChange message)
        {
            await Clients.Group($"{clientCode}:{message.UsId}").OnCashOutChange(message);
        }
        public async Task PublishWalletChange(string clientCode, OutgoingWalletChange message)
        {
            await Clients.Group($"{clientCode}:{message.UsId}").OnWalletChange(message);
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
        private static List<string> GetGroups(string clientCode, OutgoingOddsChange message)
        {
            var result = new List<string>
            {
                $"{clientCode}",
                $"endpoint:{clientCode}:fixturedetails:{message.FiId}"
            };

            if (message.FiStId == (int)FixtureStatus.Live)
            {
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:livefixtures");
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:segments:{message.SegId}:leagues:{message.LeId}:livefixtures");
            }

            if (message.FiStId == (int)FixtureStatus.Prematch && 
                message.FiStDa.Month == DateTime.UtcNow.Month && 
                message.FiStDa.Day == DateTime.UtcNow.Day && 
                message.FiStDa.Year == DateTime.UtcNow.Year &&
                message.FiTyId == (int)FixtureType.Match)
            {
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:todayfixtures");
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:dailyfixtures");
            }

            if (message.FiStId == (int)FixtureStatus.Prematch &&
                message.FiTyId == (int)FixtureType.Outright)
            {
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:outrightfixtures");
            }

            result.Add($"endpoint:{clientCode}:sports:{message.SpId}:segments:{message.SegId}:fixtures");
            result.Add($"endpoint:{clientCode}:sports:{message.SpId}:segments:{message.SegId}:leagues:{message.LeId}:fixtures");

            //TODO: topleagues
            //TODO: topfixtures

            return result;
        }
        private static List<string> GetGroups(string clientCode, OutgoingOddsChangeDelta message)
        {
            var result = new List<string>
            {
                $"{clientCode}",
                $"endpoint:{clientCode}:fixturedetails:{message.FiId}"
            };

            if (message.FiStId == (int)FixtureStatus.Live)
            {
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:livefixtures");
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:segments:{message.SegId}:leagues:{message.LeId}:livefixtures");
            }

            if (message.FiStId == (int)FixtureStatus.Prematch &&
                message.FiStDa.Month == DateTime.UtcNow.Month &&
                message.FiStDa.Day == DateTime.UtcNow.Day &&
                message.FiStDa.Year == DateTime.UtcNow.Year &&
                message.FiTyId == (int)FixtureType.Match)
            {
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:todayfixtures");
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:dailyfixtures");
            }

            if (message.FiStId == (int)FixtureStatus.Prematch &&
                message.FiTyId == (int)FixtureType.Outright)
            {
                result.Add($"endpoint:{clientCode}:sports:{message.SpId}:outrightfixtures");
            }

            result.Add($"endpoint:{clientCode}:sports:{message.SpId}:segments:{message.SegId}:fixtures");
            result.Add($"endpoint:{clientCode}:sports:{message.SpId}:segments:{message.SegId}:leagues:{message.LeId}:fixtures");

            //TODO: topleagues
            //TODO: topfixtures

            return result;
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
}


