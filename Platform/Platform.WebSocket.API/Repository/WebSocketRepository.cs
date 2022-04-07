using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Enums;
using Platform.WebSocket.API.Data;
using Platform.WebSocket.API.Messages;
using System;
using System.Linq;
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
            var ipAddress = Context.GetHttpContext().Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? string.Empty;
            var ipCountry = Context.GetHttpContext().Request.Headers["CF-IPCountry"].FirstOrDefault() ?? string.Empty;
            var userAgent = Context.GetHttpContext().Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;
            var connectionId = Context.ConnectionId;

            using var scope = _provider.CreateScope();
            _platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

            var test = Context.GetHttpContext().Request.Query;

            await _platformContext.WebSocket.AddAsync(new Data.WebSocket
            {
                ConnectionId = connectionId,
                IPAddress = ipAddress,
                IPCountry = ipCountry,
                UserAgent = userAgent
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
                _platformContext.WebSocket.Remove(connection);

            await _platformContext.SaveChangesAsync();

            await base.OnDisconnectedAsync(exception);
        }
        public async Task PublishOddsChange(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, IncomingOddsChange message)
        {
            var payload = new OutgoingOddsChange
            {
                SpId = message.SportId,
                SpNa = message.SportName,
                SegId = message.SegmentId,
                SegNa = message.SegmentName,
                LeId = message.LeagueId,
                LeNa = message.LeagueName,
                FiId = message.FixtureId.ToString(),
                PrFiId = message.ProviderFixtureId,
                FiNa = message.FixtureName,
                HoNa = GetHomeName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                AwNa = GetAwayName(message.FixtureName, (FixtureType)message.FixtureTypeId),
                FiStDa = message.FixtureStartDate,
                IsOfLi = message.IsOfferedLive,
                FiStId = message.FixtureStatusId,
                FiStNa = message.FixtureStatusName,
                Sc = message.Score,
                CuPe = message.CurrentPeriod,
                CuMi = message.CurrentMinute,
                SoOr = message.SortOrder,
                SpNaTr = message.SportTranslations.Select(f => new OutgoingSportTranslation 
                {
                    LaCo = f.LanguageCode,
                    SpNa = f.SportName
                }).ToList(),
                SegNaTr = message.SegmentTranslations.Select(f => new OutgoingSegmentTranslation
                {
                    LaCo = f.LanguageCode,
                    SegNa = f.SegmentName
                }).ToList(),
                LeNaTr = message.LeagueTranslations.Select(f => new OutgoingLeagueTranslation
                {
                    LaCo = f.LanguageCode,
                    LeNa = f.LeagueName
                }).ToList(),
                FiNaTr = message.FixtureTranslations.Select(f => new OutgoingFixtureTranslation
                {
                    LaCo = f.LanguageCode,
                    FiNa = f.FixtureName,
                    HoNa = GetHomeName(f.FixtureName, (FixtureType)message.FixtureTypeId),
                    AwNa = GetAwayName(f.FixtureName, (FixtureType)message.FixtureTypeId),
                }).ToList(),
                Ma = message.Markets.Select(m => new OutgoingMarket 
                {
                    MaTeId = m.MarketTemplateId,
                    MaId = m.MarketId.ToString(),
                    MaNa = m.MarketName,
                    Li = m.Line,
                    MaStId = (int)m.MarketStatus,
                    MaStNa = m.MarketStatus.ToString(),
                    SoOr = m.SortOrder,
                    MaNaTr = m.MarketTranslations.Select(mt => new OutgoingMarketTranslation 
                    { 
                        LaCo = mt.LanguageCode,
                        MaNa = mt.MarketName
                    }).ToList(),
                    Se = m.Selections.Select(s => new OutgoingSelection
                    {
                        SeTeId = s.SelectionTemplateId,
                        SeId = s.SelectionId.ToString(),
                        SeNa = s.SelectionName,
                        DeOd = s.Odds,
                        IsAc = s.IsActive,
                        SoOr = s.SortOrder,
                        SeNaTr = s.SelectionTranslations.Select(ss => new OutgoingSelectionTranslation 
                        { 
                            LaCo = ss.LanguageCode,
                            SeNa = ss.SelectionName
                        }).ToList()
                    }).OrderBy(o => o.SoOr).ToList()
                }).OrderBy(o => o.SoOr).ToList()
            };

            await hubContext.Clients.All.OnOddsChange(payload);
        }
        public async Task PublishBetStopFixture(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, IncomingBetStopFixture onBetStopFixture)
        {
            var payload = new OutgoingBetStopFixture
            {
                SpId = onBetStopFixture.SportId,
                FiId = onBetStopFixture.FixtureId.ToString(),
                IsBeSt = onBetStopFixture.IsBetStop,
                MaTeGr = onBetStopFixture.MarketTemplateGroups,
            };

            await hubContext.Clients.All.OnBetStopFixture(payload);
        }
        public async Task PublishBetStopGlobal(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, IncomingBetStopGlobal onBetStopGlobal)
        {
            var payload = new OutgoingBetStopGlobal
            {
                IsBeSt = onBetStopGlobal.IsBetStop
            };

            await hubContext.Clients.All.OnBetStopGlobal(payload);
        }
        private static string GetHomeName(string name, FixtureType fixtureType)
        {
            return fixtureType == FixtureType.Match ? name.Split(" vs. ").First().Trim() : string.Empty;
        }
        private static string GetAwayName(string name, FixtureType fixtureType)
        {
            return fixtureType == FixtureType.Match ? name.Split(" vs. ").Last().Trim() : string.Empty;
        }
    }
}
