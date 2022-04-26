using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Enums;
using Platform.Shared.Utility;
using Platform.WebSocket.API.Data;
using Platform.WebSocket.API.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.WebSocket.API.Repository
{
    public class WebSocketRepository : Hub<IPlatformEvent>, IWebSocketRepository
    {
        private readonly IServiceProvider _provider;
        private PlatformContext _platformContext;
        private IMessageBroker _messageBroker;

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
                await Groups.AddToGroupAsync(Context.ConnectionId, $"{client.Item2}:{request.Endpoint.ToLower()}");

                using var scope = _provider.CreateScope();

                _messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();

                _messageBroker.Publish("test", "test", "q.websocket.test", "");
            }

        }
        public async Task PublishOddsChange(IHubContext<WebSocketRepository, IPlatformEvent> hubContext, IncomingOddsChange message)
        {
            if (message.SportId > 1000 || message.FixtureTypeId == (int)FixtureType.Outright)
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
            else
            {
                if (message.ClientCodes == null || message.ClientCodes.Contains("TSO") == false)
                    return;

                var markets = new List<OutgoingMarket>();

                foreach (var market in message.Markets)
                {
                    var selections = new List<OutgoingSelection>();
                    var clientOdds = new List<ClientOdds>();
                    var margin = 0m;

                    if (market.Selections.Count > 0)
                    {
                        var activeSelections = market.Selections.Where(c => c.IsActive == true && c.Odds > 0m).ToList();

                        margin = (Math.Round(activeSelections.Sum(s => 100 / s.Odds)) - 100) / 100;

                        clientOdds = GetClientOdds(activeSelections.Count, margin, market.Selections, market.ClientCodeMargin, "TSO");
                    }

                    foreach (var selection in market.Selections)
                    {
                        var odds = clientOdds.Where(c => c.ClientCode == "TSO" && c.SelectionId == selection.SelectionId).Select(ss => ss.Odds).FirstOrDefault();

                        selections.Add(new OutgoingSelection
                        {
                            SeTeId = selection.SelectionTemplateId,
                            SeId = selection.SelectionId.ToString(),
                            SeNa = selection.SelectionName,
                            DeOd = odds,
                            IsAc = selection.IsActive,
                            SoOr = selection.SortOrder,
                            SeNaTr = selection.SelectionTranslations.Select(ss => new OutgoingSelectionTranslation
                            {
                                LaCo = ss.LanguageCode,
                                SeNa = ss.SelectionName
                            }).ToList()
                        });
                    }

                    selections = selections.OrderBy(o => o.SoOr).ToList();

                    markets.Add(new OutgoingMarket
                    {
                        MaTeId = market.MarketTemplateId,
                        MaId = market.MarketId.ToString(),
                        MaNa = market.MarketName,
                        Li = market.Line,
                        MaStId = market.ClientCodes.Contains("TSO") ? (int)market.MarketStatus : (int)MarketStatus.Inactive,
                        MaStNa = market.ClientCodes.Contains("TSO") ? market.MarketStatus.ToString() : MarketStatus.Inactive.ToString(),
                        SoOr = market.SortOrder,
                        MaNaTr = market.MarketTranslations.Select(mt => new OutgoingMarketTranslation
                        {
                            LaCo = mt.LanguageCode,
                            MaNa = mt.MarketName
                        }).ToList(),
                        Se = selections
                    });
                }

                markets = markets.OrderBy(o => o.SoOr).ToList();

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
                    Ma = markets
                };

                await hubContext.Clients.All.OnOddsChange(payload);
            }

            //var groups = new List<string>();
            //IReadOnlyList<string> readOnlyGroups = groups.AsReadOnly();

            //await hubContext.Clients.Groups(readOnlyGroups).OnOddsChange(payload);

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
        private async Task<Tuple<bool, string>> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid)
            {
                using var scope = _provider.CreateScope();
                _platformContext = scope.ServiceProvider.GetRequiredService<PlatformContext>();

                var client = await _platformContext.Client.FindAsync(guid);

                if (client != null)
                    return new Tuple<bool, string>(true, client.ClientCode);
            }

            return new Tuple<bool, string>(false, string.Empty);
        }
        private static List<ClientOdds> GetClientOdds(int activeSelectionCount, decimal margin, List<IncomingSelection> selections, string clientMarketConfig, string clientCode)
        {
            if (string.IsNullOrEmpty(clientMarketConfig))
                return new List<ClientOdds>();

            if (!clientMarketConfig.Contains(clientCode) && clientCode.ToUpper() != "OE")
                return new List<ClientOdds>();

            List<Tuple<string, decimal>> clients;

            if (clientCode.ToUpper() == "OE")
                clients = clientMarketConfig.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList();
            else
                clients = clientMarketConfig.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).Where(c => c.Item1 == clientCode).ToList();

            var result = new List<ClientOdds>();

            foreach (var client in clients)
            {
                foreach (var selection in selections)
                {
                    if (selection.IsActive == true && selection.Odds > 0m)
                    {
                        var targetMargin = client.Item2;

                        var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Odds);

                        if (fairOddsInitialDivideBy <= 0)
                        {
                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = GetMasterOddsWithAddedMargin(selection.Odds),
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId
                            });

                            continue;
                        }

                        var fairOdds = (activeSelectionCount * selection.Odds) / fairOddsInitialDivideBy;

                        if (fairOdds <= 0)
                        {
                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = GetMasterOddsWithAddedMargin(selection.Odds),
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId
                            });

                            continue;
                        }

                        var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

                        if (finalOddsInitialDivideBy <= 0)
                        {
                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = GetMasterOddsWithAddedMargin(selection.Odds),
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId
                            });

                            continue;
                        }

                        var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

                        if (odds < 1.00m)
                        {
                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                Odds = GetMasterOddsWithAddedMargin(selection.Odds),
                                SelectionTemplateId = selection.SelectionTemplateId,
                                SelectionId = selection.SelectionId
                            });

                            continue;
                        }

                        if (odds > selection.Odds)
                            odds = GetMasterOddsWithAddedMargin(selection.Odds);

                        result.Add(new ClientOdds
                        {
                            ClientCode = client.Item1,
                            Odds = odds,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            SelectionId = selection.SelectionId
                        });
                    }
                    else
                    {
                        result.Add(new ClientOdds
                        {
                            ClientCode = client.Item1,
                            Odds = selection.Odds,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            SelectionId = selection.SelectionId
                        });
                    }
                }
            }

            return result;
        }

        private static decimal GetMasterOddsWithAddedMargin(decimal odds)
        {
            if (Math.Round(odds, 2) <= 1.01m)
                return odds;

            return Math.Round(100 / ((100 / odds) + 2m), 2);
        }

    }
    public class EndpointRequest
    {
        public string ClientId { get; set; }
        public string Endpoint { get; set; }
    }

    public class ClientOdds
    {
        public string ClientCode { get; set; }
        public decimal Odds { get; set; }
        public string SelectionTemplateId { get; set; }
        public long SelectionId { get; set; }
    }
}


