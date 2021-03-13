using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Data.Sportsbook.SQLEntities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using Platform.Sportsbook.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly IMongoDatabase _db;
        private readonly IMemCache _memCache;
        private readonly IMessageBroker _messageBroker;
        private readonly SportsbookContext _sportsbookContext;
        private IMongoCollection<Fixture> fixtureCollection;
        private IMongoCollection<Market> marketCollection;
        private IMongoCollection<Selection> selectionCollection;
        private IMongoCollection<BettingStatus> bettingStatusCollection;
        private readonly HttpClient _httpClient;

        public SportsbookRepository(SportsbookContext sportsbookContext, HttpClient httpClient, IMessageBroker messageBroker, IMemCache memCache)
        {
            var client = new MongoClient("mongodb+srv://lino:fcisong88@production.n6svy.mongodb.net/sportsbook?retryWrites=true&w=0");
            _db = client.GetDatabase("Sportsbook").WithReadPreference(new ReadPreference(ReadPreferenceMode.SecondaryPreferred));

            fixtureCollection = _db.GetCollection<Fixture>("Fixture");
            marketCollection = _db.GetCollection<Market>("Market");
            selectionCollection = _db.GetCollection<Selection>("Selection");
            bettingStatusCollection = _db.GetCollection<BettingStatus>("BettingStatus");

            _sportsbookContext = sportsbookContext;
            _httpClient = httpClient;
            _messageBroker = messageBroker;
            _memCache = memCache;
            _messageBroker.Connect();
            _httpClient.BaseAddress = new Uri("https://av-api.betradar.com");
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer C9SdxJm170K61OY152");
        }
        public async Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(LivestreamRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.PrFiId == request.ProviderFixtureId);
            var fixture = await fixtureCollection.Find(fixtureFilter).FirstOrDefaultAsync();

            if (fixture == null)
            {
                return new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = StatusCode.Fixture_Has_No_Livestream,
                    Message = StatusCode.Fixture_Has_No_Livestream.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(fixture.LiId) || string.IsNullOrWhiteSpace(fixture.LiId))
            {
                return new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = StatusCode.Fixture_Has_No_Livestream,
                    Message = StatusCode.Fixture_Has_No_Livestream.ToString().Replace('_', ' ')
                };
            }

            _httpClient.DefaultRequestHeaders.Remove("X-Real-IP");
            _httpClient.DefaultRequestHeaders.Add("X-Real-IP", request.IPAddress);

            var streamType = "dash-manifest";

            if (request.IsMobile)
                streamType = "hls-manifest";

            try
            {
                var response = await _httpClient.GetAsync($"/v1/streams/{fixture.LiId}/{streamType}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<LivestreamResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<LivestreamResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new LivestreamResponseDTO
                        {
                            LiveStreamURL = result.Url
                        }
                    };
                }
                else
                {
                    var result = JsonSerializer.Deserialize<LivestreamResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<LivestreamResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{result.Error.Code} : {result.Error.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(TopLeaguesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.SpId == request.SportId && c.FiTyId == 0);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TopLeaguesResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempTopLeague
            {
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                SortOrder = sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();

            var finalResult = new List<TopLeague>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.SegmentId == item.SegmentId && c.LeagueId == item.LeagueId)) continue;
                if (finalResult.Count == 15) break;

                finalResult.Add(new TopLeague
                {
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LiveCount = fixtures.Where(c => c.SegId == item.SegmentId && c.LeId == item.LeagueId && c.FiStId == (int)FixtureStatus.Live).Count()
                });
            }

            return new BaseResponseDTO<TopLeaguesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TopLeaguesResponseDTO
                {
                    TopLeagues = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(TopLeaguesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.SpId == request.SportId && c.FiTyId == 0);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TopLeaguesResponseDTO()
                };
            }

            var result = fixtures.Select(s => new TopLeague
            {
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                LiveCount = fixtures.Where(c => c.SegId == s.SegId && c.LeId == s.LeId && c.FiStId == (int)FixtureStatus.Live).Count()
            }).OrderBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();

            var finalResult = new List<TopLeague>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.SegmentId == item.SegmentId && c.LeagueId == item.LeagueId)) continue;

                finalResult.Add(new TopLeague
                {
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LiveCount = fixtures.Where(c => c.SegId == item.SegmentId && c.LeId == item.LeagueId && c.FiStId == (int)FixtureStatus.Live).Count()
                });
            }

            return new BaseResponseDTO<TopLeaguesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TopLeaguesResponseDTO
                {
                    TopLeagues = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(TopFixturesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 1 && c.FiTyId == 0);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TopFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TopFixturesResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder();

            var result = fixtures.Select(s => new TempTopFixture
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                FixtureStartDate = s.FiStDa,
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var finalResult = new List<TopFixture>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.FixtureId == item.FixtureId)) continue;
                if (finalResult.Count == 10) break;

                finalResult.Add(item);
            }

            return new BaseResponseDTO<TopFixturesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TopFixturesResponseDTO
                {
                    TopFixtures = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(TopFixturesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 1 && c.FiTyId == 0);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TopFixturesIdsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TopFixturesIdsResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder();

            var result = fixtures.Select(s => new TempTopFixture
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                FixtureStartDate = s.FiStDa,
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var finalResult = new List<string>();

            foreach (var item in result)
            {
                if (finalResult.Contains(item.FixtureId)) continue;
                if (finalResult.Count == 10) break;

                finalResult.Add(item.FixtureId);
            }

            return new BaseResponseDTO<TopFixturesIdsResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TopFixturesIdsResponseDTO
                {
                    TopFixtures = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<SportsResponseDTO>> GetSports(SportsRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<SportsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SportsResponseDTO()
                };
            }

            var sportIds = fixtures.Select(s => s.SpId).Distinct().ToList();

            var leagueSortOrder = _sportsbookContext.TierLeagueOrder.Where(c => sportIds.Contains(c.SportId)).ToList();
            var sportSortOrder = _sportsbookContext.ClientSportOrder.Where(c => c.ClientId == request.ClientId).ToList();

            var result = fixtures.Select(s => new TempOrderedLeague
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                SortOrder = leagueSortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : leagueSortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).ToList();

            var sportResult = fixtures.Select(s => new TempOrderedSport
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SortOrder = sportSortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sportSortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SportName).ToList();

            var finalResult = new List<OrderedSport>();

            foreach (var item in sportResult)
            {
                if (finalResult.Any(c => c.SportId == item.SportId)) continue;

                finalResult.Add(item);
            }

            var final = new List<SportDTO>();

            foreach (var sport in finalResult)
            {
                final.Add(new SportDTO
                {
                    SportId = sport.SportId,
                    SportName = sport.SportName,
                    Segments = result.Where(c => c.SportId == sport.SportId).GroupBy(gse => gse.SegmentId).Select(s => new SegmentDTO
                    {
                        SegmentId = s.Key,
                        SegmentName = s.Select(se => se.SegmentName).FirstOrDefault(),
                        SortOrder = s.Select(se => se.SortOrder).FirstOrDefault(),
                        Leagues = s.GroupBy(gle => gle.LeagueId).Select(l => new LeagueDTO
                        {
                            LeagueId = l.Key,
                            LeagueName = l.Select(le => le.LeagueName).FirstOrDefault(),
                            SortOrder = l.Select(le => le.SortOrder).FirstOrDefault()
                        }).OrderBy(o => o.SortOrder).ThenBy(o => o.LeagueName).ToList()
                    }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ToList()
                });
            }

            return new BaseResponseDTO<SportsResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SportsResponseDTO
                {
                    Sports = final
                }
            };
        }
        public async Task<BaseResponseDTO<OrderedSportsResponseDTO>> GetOrderedSports(OrderedSportsRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<OrderedSportsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new OrderedSportsResponseDTO()
                };
            }

            var sortOrder = await _sportsbookContext.ClientSportOrder.Where(c => c.ClientId == request.ClientId).ToListAsync();

            var result = fixtures.Select(s => new TempOrderedSport
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SportName).ToList();

            var finalResult = new List<OrderedSport>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.SportId == item.SportId)) continue;

                finalResult.Add(item);
            }

            return new BaseResponseDTO<OrderedSportsResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new OrderedSportsResponseDTO
                {
                    Sports = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<OrderedLeaguesResponseDTO>> GetOrderedLeagues(OrderedLeaguesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<OrderedLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new OrderedLeaguesResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempOrderedLeague
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SportName).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();

            var finalResult = new List<OrderedLeague>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.SportId == item.SportId && c.SegmentId == item.SegmentId && c.LeagueId == item.LeagueId)) continue;

                finalResult.Add(item);
            }

            return new BaseResponseDTO<OrderedLeaguesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new OrderedLeaguesResponseDTO
                {
                    Leagues = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups()
        {
            var initial = await _sportsbookContext.MarketTemplateGroup.ToListAsync();

            var prematch = initial.Where(c => c.FixtureStatusId == 0 && c.MarketTemplates != "0").ToList();
            var live = initial.Where(c => c.FixtureStatusId == 1 && c.MarketTemplates != "0").ToList();

            var sportGroup = initial.GroupBy(g => g.SportId).Select(s => new SportMarketTemplateGroup
            {
                SportId = s.Key,
                Prematch = s.Where(c => c.FixtureStatusId == (int)FixtureStatus.Prematch && c.MarketTemplates != "0").Select(p => new Shared.Models.MarketTemplateGroup
                {
                    MarketTemplateGroupId = p.Id,
                    MarketTemplateGroupName = p.Name,
                    SortOrder = p.SortOrder,
                    MarketTemplateIds = p.MarketTemplates.Split(',').Select(ss => ss.ToString()).ToList()
                }).OrderBy(o => o.MarketTemplateGroupId).ToList(),
                Live = s.Where(c => c.FixtureStatusId == (int)FixtureStatus.Live && c.MarketTemplates != "0").Select(l => new Shared.Models.MarketTemplateGroup
                {
                    MarketTemplateGroupId = l.Id,
                    MarketTemplateGroupName = l.Name,
                    SortOrder = l.SortOrder,
                    MarketTemplateIds = l.MarketTemplates.Split(',').Select(ss => ss.ToString()).ToList()
                }).OrderBy(o => o.MarketTemplateGroupId).ToList()
            }).OrderBy(o => o.SportId).ToList();

            return new BaseResponseDTO<MarketTemplateGroupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new MarketTemplateGroupResponseDTO
                {
                    Sports = sportGroup
                }
            };
        }
        private async Task<Tuple<List<Fixture>, List<Market>, List<Selection>, BettingStatus>> GetBetFixtures(BetFixtureRequestDTO request)
        {
            var fixtureIds = request.Selections.Select(s => s.FixtureId).ToList();
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => fixtureIds.Contains(c.FiId));
            var fixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var marketIds = request.Selections.Select(s => s.MarketId).ToList();
            var marketFilter = Builders<Market>.Filter.Where(c => marketIds.Contains(c.MaId));
            var markets = marketCollection.Find(marketFilter).ToListAsync();

            var selectionIds = request.Selections.Select(s => s.SelectionId).ToList();
            var selectionFilter = Builders<Selection>.Filter.Where(c => selectionIds.Contains(c.SeId));
            var selections = selectionCollection.Find(selectionFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var bettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(fixtures, markets, selections, bettingStatus);

            return new Tuple<List<Fixture>, List<Market>, List<Selection>, BettingStatus>(fixtures.Result, markets.Result, selections.Result, bettingStatus.Result);
        }
        public async Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request)
        {
            var result = await GetBetFixtures(request);

            return new BaseResponseDTO<BetFixtureResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetFixtureResponseDTO
                {
                    Selections = request.Selections.Select(s => new BetFixtureSelectionResponseDTO
                    {
                        SportId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SpId).FirstOrDefault(),
                        SportName = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SpNa).FirstOrDefault(),
                        SegmentId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SegId).FirstOrDefault(),
                        SegmentName = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SegNa).FirstOrDefault(),
                        LeagueId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.LeId).FirstOrDefault(),
                        LeagueName = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.LeNa).FirstOrDefault(),
                        FixtureId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.FiId).FirstOrDefault(),
                        FixtureName = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.FiNa).FirstOrDefault(),
                        FixtureType = (FixtureType)result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.FiTyId).FirstOrDefault(),
                        MarketTemplateId = result.Item2.Where(c => c.MaId == s.MarketId).Select(ss => ss.MaTeId).FirstOrDefault(),
                        MarketId = result.Item2.Where(c => c.MaId == s.MarketId).Select(ss => ss.MaId).FirstOrDefault(),
                        MarketName = result.Item2.Where(c => c.MaId == s.MarketId).Select(ss => ss.MaNa).FirstOrDefault(),
                        Line = result.Item2.Where(c => c.MaId == s.MarketId).Select(ss => ss.Li).FirstOrDefault(),
                        SelectionTemplateId = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.SeTeId).FirstOrDefault(),
                        SelectionId = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.SeId).FirstOrDefault(),
                        SelectionName = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.SeNa).FirstOrDefault(),
                        Odds = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.Od).FirstOrDefault(),
                        FixtureStatus = (FixtureStatus)result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.FiStId).FirstOrDefault(),
                        FixtureStartDate = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.FiStDa).FirstOrDefault(),
                        Score = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.Sc).FirstOrDefault(),
                        FixtureSpecifier = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.FiSp).FirstOrDefault(),
                        SelectionSpecifier = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.SeSp).FirstOrDefault(),
                        CorrelationId = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.CoId).FirstOrDefault(),
                        BetSelectionState = GetBetSelectionStatus(request.OddsChangeType, s, result)
                    }).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(UserFavoriteRequestDTO request)
        {
            var result = new List<FixtureDTO>();
            var favorites = await _sportsbookContext.UserFavorite.Where(c => c.UserId == $"{request.ClientCode}-{request.UserId}").ToListAsync();

            if (favorites.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var leagues = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.League).Select(s => Convert.ToInt32(s.Id)).Distinct().ToList();

            if (leagues.Count > 0)
            {
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && leagues.Contains(c.LeId));
                var leagueResult = await fixtureCollection.Find(leagueFilter).ToListAsync();

                if (leagueResult.Count > 0)
                {
                    var fixtureIds = leagueResult.Select(s => Convert.ToInt64(s.FiId)).ToList();
                    var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

                    var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
                    var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

                    var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
                    var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

                    var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
                    var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

                    foreach (var item in leagueResult)
                    {
                        if (result.Any(c => c.FixtureId == item.FiId.ToString())) continue;

                        result.Add(new FixtureDTO
                        {
                            SportId = item.SpId,
                            SportName = item.SpNa,
                            SegmentId = item.SegId,
                            SegmentName = item.SegNa,
                            LeagueId = item.LeId,
                            LeagueName = item.LeNa,
                            ProviderFixtureId = item.PrFiId,
                            FixtureId = item.FiId.ToString(),
                            FixtureName = item.FiNa,
                            HomeName = item.FiTyId == (int)FixtureType.Match ? item.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                            AwayName = item.FiTyId == (int)FixtureType.Match ? item.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                            FixtureStartDate = item.FiStDa,
                            IsLive = item.IsOfLi,
                            IsLiveStream = string.IsNullOrEmpty(item.LiId) ? false : true,
                            FixtureStatusId = item.FiStId,
                            FixtureStatusName = item.FiStNa,
                            MarketCount = marketCount.Result.Where(c => c.FiId == item.FiId).Count(),
                            SMSCode = string.Empty,
                            StatisticsURL = $"https://s5.sir.sportradar.com/igaholding2{item.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                            Score = item.Sc,
                            CurrentPeriod = CurrentPeriodHelper.ToShortName(item.CuPe),
                            CurrentMinute = item.CuMi.ToString(),
                            Markets = GetMarkets(item.SpId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList())
                        });
                    }
                }
            }

            var fixtures = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtures.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && fixtures.Contains(c.FiId));
                var fixtureResult = await fixtureCollection.Find(fixtureFilter).ToListAsync();

                if (fixtureResult.Count > 0)
                {
                    var fixtureIds = fixtureResult.Select(s => Convert.ToInt64(s.FiId)).ToList();
                    var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

                    var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
                    var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

                    var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
                    var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

                    var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
                    var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

                    foreach (var item in fixtureResult)
                    {
                        if (result.Any(c => c.FixtureId == item.FiId.ToString())) continue;

                        result.Add(new FixtureDTO
                        {
                            SportId = item.SpId,
                            SportName = item.SpNa,
                            SegmentId = item.SegId,
                            SegmentName = item.SegNa,
                            LeagueId = item.LeId,
                            LeagueName = item.LeNa,
                            ProviderFixtureId = item.PrFiId,
                            FixtureId = item.FiId.ToString(),
                            FixtureName = item.FiNa,
                            HomeName = item.FiTyId == (int)FixtureType.Match ? item.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                            AwayName = item.FiTyId == (int)FixtureType.Match ? item.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                            FixtureStartDate = item.FiStDa,
                            IsLive = item.IsOfLi,
                            IsLiveStream = string.IsNullOrEmpty(item.LiId) ? false : true,
                            FixtureStatusId = item.FiStId,
                            FixtureStatusName = item.FiStNa,
                            MarketCount = marketCount.Result.Where(c => c.FiId == item.FiId).Count(),
                            SMSCode = string.Empty,
                            StatisticsURL = $"https://s5.sir.sportradar.com/igaholding2{item.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                            Score = item.Sc,
                            CurrentPeriod = CurrentPeriodHelper.ToShortName(item.CuPe),
                            CurrentMinute = item.CuMi.ToString(),
                            Markets = GetMarkets(item.SpId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList())
                        });
                    }
                }
            }

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(UserFavoriteRequestDTO request)
        {
            var result = new List<UserFavoriteDTO>();
            var favorites = await _sportsbookContext.UserFavorite.Where(c => c.UserId == $"{request.ClientCode}-{request.UserId}").ToListAsync();

            if (favorites.Count == 0)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new UserFavoriteResponseDTO()
                };
            }

            var leagueIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.League).Select(s => Convert.ToInt32(s.Id)).Distinct().ToList();

            if (leagueIds.Count > 0)
            {
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && leagueIds.Contains(c.LeId));
                var leagueResult = await fixtureCollection.Find(leagueFilter).ToListAsync();

                foreach (var item in leagueResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.LeId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = item.SpNa,
                        HierarchyId = (int)UserFavoriteHierarchy.League,
                        HierarchyName = UserFavoriteHierarchy.League.ToString(),
                        Id = item.LeId.ToString(),
                        Name = item.LeNa
                    });
                }
            }

            var fixtureIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtureIds.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && fixtureIds.Contains(c.FiId));
                var fixtureResult = await fixtureCollection.Find(fixtureFilter).ToListAsync();

                foreach (var item in fixtureResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.FiId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = item.SpNa,
                        HierarchyId = (int)UserFavoriteHierarchy.Fixture,
                        HierarchyName = UserFavoriteHierarchy.Fixture.ToString(),
                        Id = item.FiId.ToString(),
                        Name = item.FiNa
                    });
                }
            }

            return new BaseResponseDTO<UserFavoriteResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new UserFavoriteResponseDTO
                {
                    Favorites = result.OrderBy(o => o.Name).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddFavorites(UserFavoriteInsertDeleteRequestDTO request)
        {
            var result = new List<UserFavoriteDTO>();

            var favorite = await _sportsbookContext.UserFavorite.FindAsync($"{request.ClientCode}-{request.UserId}", request.Id);

            if (favorite == null)
            {
                _sportsbookContext.UserFavorite.Add(new UserFavorite
                {
                    UserId = $"{request.ClientCode}-{request.UserId}",
                    Id = request.Id,
                    HierarchyId = (int)request.UserFavoriteHierarchy
                });

                _sportsbookContext.SaveChanges();
            }

            var favorites = await _sportsbookContext.UserFavorite.Where(c => c.UserId == $"{request.ClientCode}-{request.UserId}").ToListAsync();

            if (favorites.Count == 0)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new UserFavoriteResponseDTO()
                };
            }

            var leagueIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.League).Select(s => Convert.ToInt32(s.Id)).Distinct().ToList();

            if (leagueIds.Count > 0)
            {
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && leagueIds.Contains(c.LeId));
                var leagueResult = await fixtureCollection.Find(leagueFilter).ToListAsync();

                foreach (var item in leagueResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.LeId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = item.SpNa,
                        HierarchyId = (int)UserFavoriteHierarchy.League,
                        HierarchyName = UserFavoriteHierarchy.League.ToString(),
                        Id = item.LeId.ToString(),
                        Name = item.LeNa
                    });
                }
            }

            var fixtureIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtureIds.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && fixtureIds.Contains(c.FiId));
                var fixtureResult = await fixtureCollection.Find(fixtureFilter).ToListAsync();

                foreach (var item in fixtureResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.FiId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = item.SpNa,
                        HierarchyId = (int)UserFavoriteHierarchy.Fixture,
                        HierarchyName = UserFavoriteHierarchy.Fixture.ToString(),
                        Id = item.FiId.ToString(),
                        Name = item.FiNa
                    });
                }
            }

            return new BaseResponseDTO<UserFavoriteResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new UserFavoriteResponseDTO
                {
                    Favorites = result.OrderBy(o => o.Name).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteFavorites(UserFavoriteInsertDeleteRequestDTO request)
        {
            var result = new List<UserFavoriteDTO>();

            var favorite = await _sportsbookContext.UserFavorite.FindAsync($"{request.ClientCode}-{request.UserId}", request.Id);

            if (favorite != null)
            {
                _sportsbookContext.UserFavorite.Remove(favorite);
                _sportsbookContext.SaveChanges();
            }

            var favorites = await _sportsbookContext.UserFavorite.Where(c => c.UserId == $"{request.ClientCode}-{request.UserId}").ToListAsync();

            if (favorites.Count == 0)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new UserFavoriteResponseDTO()
                };
            }

            var leagueIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.League).Select(s => Convert.ToInt32(s.Id)).Distinct().ToList();

            if (leagueIds.Count > 0)
            {
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && leagueIds.Contains(c.LeId));
                var leagueResult = await fixtureCollection.Find(leagueFilter).ToListAsync();

                foreach (var item in leagueResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.LeId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = item.SpNa,
                        HierarchyId = (int)UserFavoriteHierarchy.League,
                        HierarchyName = UserFavoriteHierarchy.League.ToString(),
                        Id = item.LeId.ToString(),
                        Name = item.LeNa
                    });
                }
            }

            var fixtureIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtureIds.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && fixtureIds.Contains(c.FiId));
                var fixtureResult = await fixtureCollection.Find(fixtureFilter).ToListAsync();

                foreach (var item in fixtureResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.FiId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = item.SpNa,
                        HierarchyId = (int)UserFavoriteHierarchy.Fixture,
                        HierarchyName = UserFavoriteHierarchy.Fixture.ToString(),
                        Id = item.FiId.ToString(),
                        Name = item.FiNa
                    });
                }
            }

            return new BaseResponseDTO<UserFavoriteResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new UserFavoriteResponseDTO
                {
                    Favorites = result.OrderBy(o => o.Name).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopFixturesTW(TWRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == 0 && c.FiTyId == (int)FixtureType.Match);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var initialResult = new List<TempFixtureDTO>();

            var limit = 10;

            if (request.ClientId == Guid.Parse("5093617A-9367-EB11-89FB-00224815B68B"))
            {
                initialResult.AddRange(result.Where(c => c.SportId == 21).ToList());
                limit = initialResult.Count + 10;
            }

            foreach (var item in result)
            {
                if (initialResult.Any(c => c.FixtureId == item.FixtureId && c.SportId != 21)) continue;
                if (initialResult.Count == limit) break;

                initialResult.Add(item);
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();
            //var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            foreach (var item in initialResult)
            {
                var currentMarkets = GetMarkets(item.SportId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList());

                if (currentMarkets.Count == 0) continue;

                finalResult.Add(new FixtureDTO
                {
                    SportId = item.SportId,
                    SportName = item.SportName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    FixtureId = item.FixtureId,
                    ProviderFixtureId = item.ProviderFixtureId,
                    FixtureName = item.FixtureName,
                    HomeName = item.HomeName,
                    AwayName = item.AwayName,
                    FixtureStartDate = item.FixtureStartDate,
                    IsLive = item.IsLive,
                    IsLiveStream = item.IsLiveStream,
                    FixtureStatusId = item.FixtureStatusId,
                    FixtureStatusName = item.FixtureStatusName,
                    MarketCount = marketCount.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = item.StatisticsURL,
                    Score = item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    Markets = currentMarkets
                });

                if (finalResult.Count == 10) break;
            }

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(TWFLiveFixtureCountRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Live && c.FiTyId == (int)FixtureType.Match);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWFixtureCountResponseDTO()
                };
            }

            var sortOrder = await _sportsbookContext.ClientSportOrder.Where(c => c.ClientId == request.ClientId).ToListAsync();

            var result = new List<FixtureCount>();

            foreach (var item in sortOrder.OrderBy(o => o.SortOrder).ToList())
            {
                var count = fixtures.Where(c => c.SpId == item.SportId).Count();

                if (count == 0) continue;

                result.Add(new FixtureCount
                {
                    SportId = item.SportId,
                    SportName = item.SportName.Trim(),
                    Count = count
                });
            }

            return new BaseResponseDTO<TWFixtureCountResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWFixtureCountResponseDTO
                {
                    Sports = result
                }
            };
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(TWFTodayFixtureCountRequestDTO request)
        {
            var dateTimeRange = GetDateTimeRange(DateTime.UtcNow, request.MinuteUTCOffset);
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWFixtureCountResponseDTO()
                };
            }

            var sortOrder = await _sportsbookContext.ClientSportOrder.Where(c => c.ClientId == request.ClientId).ToListAsync();

            var result = new List<FixtureCount>();

            foreach (var item in sortOrder.OrderBy(o => o.SortOrder).ToList())
            {
                var count = fixtures.Where(c => c.SpId == item.SportId).Count();

                if (count == 0) continue;

                result.Add(new FixtureCount
                {
                    SportId = item.SportId,
                    SportName = item.SportName.Trim(),
                    Count = count
                });
            }

            return new BaseResponseDTO<TWFixtureCountResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWFixtureCountResponseDTO
                {
                    Sports = result
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(TWLiveFixtureRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Live && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var initialResult = new List<TempFixtureDTO>();

            foreach (var item in result)
            {
                if (initialResult.Any(c => c.FixtureId == item.FixtureId)) continue;

                initialResult.Add(item);
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            foreach (var item in initialResult)
            {
                finalResult.Add(new FixtureDTO
                {
                    SportId = item.SportId,
                    SportName = item.SportName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    FixtureId = item.FixtureId,
                    ProviderFixtureId = item.ProviderFixtureId,
                    FixtureName = item.FixtureName,
                    HomeName = item.HomeName,
                    AwayName = item.AwayName,
                    FixtureStartDate = item.FixtureStartDate,
                    IsLive = item.IsLive,
                    IsLiveStream = item.IsLiveStream,
                    FixtureStatusId = item.FixtureStatusId,
                    FixtureStatusName = item.FixtureStatusName,
                    MarketCount = marketCount.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = item.StatisticsURL,
                    Score = item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    Markets = GetMarkets(item.SportId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList())
                });
            }

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(TWTodayFixtureRequestDTO request)
        {
            var dateTimeRange = GetDateTimeRange(DateTime.UtcNow, request.MinuteUTCOffset);
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var initialResult = new List<TempFixtureDTO>();

            foreach (var item in result)
            {
                if (initialResult.Any(c => c.FixtureId == item.FixtureId)) continue;

                initialResult.Add(item);
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            foreach (var item in initialResult)
            {
                finalResult.Add(new FixtureDTO
                {
                    SportId = item.SportId,
                    SportName = item.SportName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    FixtureId = item.FixtureId,
                    ProviderFixtureId = item.ProviderFixtureId,
                    FixtureName = item.FixtureName,
                    HomeName = item.HomeName,
                    AwayName = item.AwayName,
                    FixtureStartDate = item.FixtureStartDate,
                    IsLive = item.IsLive,
                    IsLiveStream = item.IsLiveStream,
                    FixtureStatusId = item.FixtureStatusId,
                    FixtureStatusName = item.FixtureStatusName,
                    MarketCount = marketCount.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = item.StatisticsURL,
                    Score = item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    Markets = GetMarkets(item.SportId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList())
                });
            }

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(TWDailyFixtureRequestDTO request)
        {
            var dateTimeRange = GetDateTimeRange(request.DateUTC, request.MinuteUTCOffset);
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var initialResult = new List<TempFixtureDTO>();

            foreach (var item in result)
            {
                if (initialResult.Any(c => c.FixtureId == item.FixtureId)) continue;

                initialResult.Add(item);
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            foreach (var item in initialResult)
            {
                finalResult.Add(new FixtureDTO
                {
                    SportId = item.SportId,
                    SportName = item.SportName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    FixtureId = item.FixtureId,
                    ProviderFixtureId = item.ProviderFixtureId,
                    FixtureName = item.FixtureName,
                    HomeName = item.HomeName,
                    AwayName = item.AwayName,
                    FixtureStartDate = item.FixtureStartDate,
                    IsLive = item.IsLive,
                    IsLiveStream = item.IsLiveStream,
                    FixtureStatusId = item.FixtureStatusId,
                    FixtureStatusName = item.FixtureStatusName,
                    MarketCount = marketCount.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = item.StatisticsURL,
                    Score = item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    Markets = GetMarkets(item.SportId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList())
                });
            }

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(TWOutrightFixtureRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Outright && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var initialResult = new List<TempFixtureDTO>();

            foreach (var item in result)
            {
                if (initialResult.Any(c => c.FixtureId == item.FixtureId)) continue;

                initialResult.Add(item);
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            foreach (var item in initialResult)
            {
                finalResult.Add(new FixtureDTO
                {
                    SportId = item.SportId,
                    SportName = item.SportName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    FixtureId = item.FixtureId,
                    ProviderFixtureId = item.ProviderFixtureId,
                    FixtureName = item.FixtureName,
                    HomeName = item.HomeName,
                    AwayName = item.AwayName,
                    FixtureStartDate = item.FixtureStartDate,
                    IsLive = item.IsLive,
                    IsLiveStream = item.IsLiveStream,
                    FixtureStatusId = item.FixtureStatusId,
                    FixtureStatusName = item.FixtureStatusName,
                    MarketCount = markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = item.StatisticsURL,
                    Score = item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    Markets = GetOutrightMarkets(markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList())
                });
            }

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(TWTopLeagueFixtureRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var resultInitial = fixtures.Select(s => new TempTopLeague
            {
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                SortOrder = sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();

            var finalResult = new List<TopLeague>();

            foreach (var item in resultInitial)
            {
                if (finalResult.Any(c => c.SegmentId == item.SegmentId && c.LeagueId == item.LeagueId)) continue;
                if (finalResult.Count == 15) break;

                finalResult.Add(new TopLeague
                {
                    LeagueId = item.LeagueId,
                    LeagueName = item.LeagueName,
                    SegmentId = item.SegmentId,
                    SegmentName = item.SegmentName,
                    LiveCount = fixtures.Where(c => c.FiStId == (int)FixtureStatus.Live && c.SegId == item.SegmentId && c.LeId == item.LeagueId).Count()
                });
            }

            var leagueIds = finalResult.Select(s => s.LeagueId).Distinct().ToList();

            var fixtureFilter2 = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.SpId == request.SportId && leagueIds.Contains(c.LeId));
            var fixtures2 = await fixtureCollection.Find(fixtureFilter2).ToListAsync();

            var fixtureIds = fixtures2.Select(s => Convert.ToInt64(s.FiId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var result = fixtures2.Select(s => new FixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = marketCount.Result.Where(c => c.FiId == s.FiId).Count(),
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                Markets = GetMarkets(s.SpId, markets.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList())
            }).OrderBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWSegmentFixtureRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.SegId == request.SegmentId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var fixtureIds = fixtures.Select(s => Convert.ToInt64(s.FiId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var result = fixtures.Select(s => new FixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = marketCount.Result.Where(c => c.FiId == s.FiId).Count(),
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                Markets = GetMarkets(s.SpId, markets.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList())
            }).OrderBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWFixtureRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var fixtureIds = fixtures.Select(s => Convert.ToInt64(s.FiId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var result = fixtures.Select(s => new FixtureDTO
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                HomeName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                AwayName = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = string.IsNullOrEmpty(s.LiId) ? false : true,
                FixtureStatusId = s.FiStId,
                FixtureStatusName = s.FiStNa,
                MarketCount = marketCount.Result.Where(c => c.FiId == s.FiId).Count(),
                SMSCode = string.Empty,
                StatisticsURL = string.IsNullOrEmpty(s.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{s.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                Markets = GetMarkets(s.SpId, markets.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList())
            }).OrderBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        public async Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(TWSearchRequestDTO request)
        {
            var result = new List<SearchResponse>();

            var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.LeNa.ToLower().Contains(request.SearchText.ToLower()));
            var leagues = fixtureCollection.Find(leagueFilter).ToListAsync();

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiNa.ToLower().Contains(request.SearchText.ToLower()));
            var fixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            await Task.WhenAll(leagues, fixtures);

            var finalLeagues = leagues.Result;
            var finalFixtures = fixtures.Result;

            if (finalLeagues.Count == 0 && finalFixtures.Count == 0)
            {
                return new BaseResponseDTO<TWSearchResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWSearchResponseDTO()
                };
            }

            var leagueListStartsWith = new List<SearchResponse>();
            var leagueListNotStartsWith = new List<SearchResponse>();

            var fixtureListStartsWith = new List<SearchResponse>();
            var fixtureListNotStartsWith = new List<SearchResponse>();

            var sortOrder = GetTierLeagueOrder();

            foreach (var league in finalLeagues)
            {
                if (leagueListStartsWith.Any(c => c.SportId == league.SpId && c.SegmentId == league.SegId && c.LeagueId == league.LeId)) continue;
                if (leagueListNotStartsWith.Any(c => c.SportId == league.SpId && c.SegmentId == league.SegId && c.LeagueId == league.LeId)) continue;

                if (league.LeNa.ToLower().StartsWith(request.SearchText.ToLower()))
                {
                    leagueListStartsWith.Add(new SearchResponse
                    {
                        SportId = league.SpId,
                        SportName = league.SpNa,
                        SegmentId = league.SegId,
                        SegmentName = league.SegNa,
                        LeagueId = league.LeId,
                        LeagueName = league.LeNa,
                        FixtureId = string.Empty,
                        FixtureName = string.Empty
                    });
                }
                else
                {
                    if (league.LeNa.ToLower().Contains(request.SearchText.ToLower()))
                    {
                        leagueListNotStartsWith.Add(new SearchResponse
                        {
                            SportId = league.SpId,
                            SportName = league.SpNa,
                            SegmentId = league.SegId,
                            SegmentName = league.SegNa,
                            LeagueId = league.LeId,
                            LeagueName = league.LeNa,
                            FixtureId = string.Empty,
                            FixtureName = string.Empty
                        });
                    }
                }
            }

            foreach (var fixture in finalFixtures)
            {
                if (fixtureListStartsWith.Any(c => c.FixtureId == fixture.FiId.ToString())) continue;
                if (fixtureListNotStartsWith.Any(c => c.FixtureId == fixture.FiId.ToString())) continue;

                if (fixture.FiNa.Split(" vs. ").First().ToLower().StartsWith(request.SearchText.ToLower()) || fixture.FiNa.Split(" vs. ").Last().ToLower().StartsWith(request.SearchText.ToLower()))
                {
                    fixtureListStartsWith.Add(new SearchResponse
                    {
                        SportId = fixture.SpId,
                        SportName = fixture.SpNa,
                        SegmentId = fixture.SegId,
                        SegmentName = fixture.SegNa,
                        LeagueId = fixture.LeId,
                        LeagueName = fixture.LeNa,
                        FixtureId = fixture.FiId.ToString(),
                        FixtureName = fixture.FiNa
                    });
                }
                else
                {
                    if (fixture.FiNa.ToLower().Contains(request.SearchText.ToLower()))
                    {
                        fixtureListNotStartsWith.Add(new SearchResponse
                        {
                            SportId = fixture.SpId,
                            SportName = fixture.SpNa,
                            SegmentId = fixture.SegId,
                            SegmentName = fixture.SegNa,
                            LeagueId = fixture.LeId,
                            LeagueName = fixture.LeNa,
                            FixtureId = fixture.FiId.ToString(),
                            FixtureName = fixture.FiNa
                        });
                    }
                }
            }

            var startsWith = new List<SearchResponse>();
            var notStartsWith = new List<SearchResponse>();

            startsWith.AddRange(leagueListStartsWith);
            startsWith.AddRange(fixtureListStartsWith);
            notStartsWith.AddRange(leagueListNotStartsWith);
            notStartsWith.AddRange(fixtureListNotStartsWith);

            var final1 = startsWith.Select(s => new TempSearchResponse
            {
                SportId = s.SportId,
                SportName = s.SportName,
                SegmentId = s.SegmentId,
                SegmentName = s.SegmentName,
                LeagueId = s.LeagueId,
                LeagueName = s.LeagueName,
                FixtureId = s.FixtureId,
                FixtureName = s.FixtureName,
                SortOrder = sortOrder.Where(c => c.SportId == s.SportId && c.SegmentId == s.SegmentId && c.LeagueId == s.LeagueId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SportId && c.SegmentId == s.SegmentId && c.LeagueId == s.LeagueId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ToList();

            var final2 = notStartsWith.Select(s => new TempSearchResponse
            {
                SportId = s.SportId,
                SportName = s.SportName,
                SegmentId = s.SegmentId,
                SegmentName = s.SegmentName,
                LeagueId = s.LeagueId,
                LeagueName = s.LeagueName,
                FixtureId = s.FixtureId,
                FixtureName = s.FixtureName,
                SortOrder = sortOrder.Where(c => c.SportId == s.SportId && c.SegmentId == s.SegmentId && c.LeagueId == s.LeagueId).Select(s => s.SortOrder).FirstOrDefault()
                == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SportId && c.SegmentId == s.SegmentId && c.LeagueId == s.LeagueId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ToList();

            if (final1.Count > 0)
            {
                foreach (var item in final1)
                {
                    result.Add(new SearchResponse
                    {
                        SportId = item.SportId,
                        SportName = item.SportName,
                        SegmentId = item.SegmentId,
                        SegmentName = item.SegmentName,
                        LeagueId = item.LeagueId,
                        LeagueName = item.LeagueName,
                        FixtureId = item.FixtureId,
                        FixtureName = item.FixtureName
                    });
                }
            }

            if (final2.Count > 0)
            {
                foreach (var item in final2)
                {
                    result.Add(new SearchResponse
                    {
                        SportId = item.SportId,
                        SportName = item.SportName,
                        SegmentId = item.SegmentId,
                        SegmentName = item.SegmentName,
                        LeagueId = item.LeagueId,
                        LeagueName = item.LeagueName,
                        FixtureId = item.FixtureId,
                        FixtureName = item.FixtureName
                    });
                }
            }

            return new BaseResponseDTO<TWSearchResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWSearchResponseDTO
                {
                    SearchResult = result
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(TWFixtureDetailsRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiId == Convert.ToInt64(request.FixtureId));
            var fixture = await fixtureCollection.Find(fixtureFilter).FirstOrDefaultAsync();

            if (fixture == null)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var fixtureIds = new List<long> { fixture.FiId };

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            var initial = await _sportsbookContext.MarketTemplateGroup.Where(c => c.SportId == fixture.SpId).ToListAsync();

            var prematch = initial.Where(c => c.FixtureStatusId == 0 && c.MarketTemplates != "0").ToList();
            var live = initial.Where(c => c.FixtureStatusId == 1 && c.MarketTemplates != "0").ToList();

            var marketTemplateGroups = new SportMarketTemplateGroup
            {
                SportId = fixture.SpId,
                Prematch = prematch.Select(p => new Shared.Models.MarketTemplateGroup
                {
                    MarketTemplateGroupId = p.Id,
                    MarketTemplateGroupName = p.Name,
                    SortOrder = p.SortOrder,
                    MarketTemplateIds = p.MarketTemplates.Split(',').Select(ss => ss.ToString()).ToList()
                }).OrderBy(o => o.MarketTemplateGroupId).ToList(),
                Live = live.Select(l => new Shared.Models.MarketTemplateGroup
                {
                    MarketTemplateGroupId = l.Id,
                    MarketTemplateGroupName = l.Name,
                    SortOrder = l.SortOrder,
                    MarketTemplateIds = l.MarketTemplates.Split(',').Select(ss => ss.ToString()).ToList()
                }).OrderBy(o => o.MarketTemplateGroupId).ToList()
            };

            var result = new List<FixtureDTO>
            {
                new FixtureDTO
                {
                    SportId = fixture.SpId,
                    SportName = fixture.SpNa,
                    SegmentId = fixture.SegId,
                    SegmentName = fixture.SegNa,
                    LeagueId = fixture.LeId,
                    LeagueName = fixture.LeNa,
                    ProviderFixtureId = fixture.PrFiId,
                    FixtureId = fixture.FiId.ToString(),
                    FixtureName = fixture.FiNa,
                    HomeName = fixture.FiTyId == (int)FixtureType.Match ? fixture.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                    AwayName = fixture.FiTyId == (int)FixtureType.Match ? fixture.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                    FixtureStartDate = fixture.FiStDa,
                    IsLive = fixture.IsOfLi,
                    IsLiveStream = string.IsNullOrEmpty(fixture.LiId) ? false : true,
                    FixtureStatusId = fixture.FiStId,
                    FixtureStatusName = fixture.FiStNa,
                    MarketCount = markets.Result.Where(c => c.FiId == fixture.FiId).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = string.IsNullOrEmpty(fixture.StUr) ? string.Empty : $"https://s5.sir.sportradar.com/igaholding2{fixture.StUr}".Replace("{languageCode}", request.LanguageCode.ToString().ToLower()),
                    Score = fixture.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(fixture.CuPe),
                    CurrentMinute = fixture.CuMi.ToString(),
                    MarketTemplateGroups = marketTemplateGroups,
                    Markets = GetMarkets(markets.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList())
                }
            };

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        private List<MarketDTO> GetMarkets(int sportId, List<MarketFilter> markets, List<SelectionFilter> selections)
        {
            var marketTemplates = GetMainMarketTemplates(sportId);

            return markets.Where(c => marketTemplates.Contains(c.MaTeId)).Select(m => new MarketDTO
            {
                MarketTemplateId = m.MaTeId,
                MarketId = m.MaId.ToString(),
                MarketName = m.MaNa,
                Line = m.Li,
                IsCashout = false,
                MarketStatusId = m.MaStId,
                MarketStatusName = m.MaStNa,
                Selections = selections.Where(c => c.MaId == m.MaId).Select(s => new SelectionDTO
                {
                    SelectionTemplateId = s.SeTeId,
                    SelectionId = s.SeId.ToString(),
                    SelectionName = s.SeNa,
                    DecimalOdds = s.Od.ToString(),
                    IsActive = s.IsAc,
                    CorrelationId = s.CoId
                }).OrderBy(o => o.SelectionTemplateId).ToList()
            }).OrderBy(o => o.MarketTemplateId).ThenBy(o => o.MarketName).ToList();
        }
        private List<MarketDTO> GetMarkets(List<MarketFilter> markets, List<SelectionFilter> selections)
        {
            return markets.Select(m => new MarketDTO
            {
                MarketTemplateId = m.MaTeId,
                MarketId = m.MaId.ToString(),
                MarketName = m.MaNa,
                Line = m.Li,
                IsCashout = false,
                MarketStatusId = m.MaStId,
                MarketStatusName = m.MaStNa,
                Selections = selections.Where(c => c.MaId == m.MaId).Select(s => new SelectionDTO
                {
                    SelectionTemplateId = s.SeTeId,
                    SelectionId = s.SeId.ToString(),
                    SelectionName = s.SeNa,
                    DecimalOdds = s.Od.ToString(),
                    IsActive = s.IsAc,
                    CorrelationId = s.CoId,
                }).OrderBy(o => o.SelectionTemplateId).ToList()
            }).OrderBy(o => o.MarketTemplateId).ThenBy(o => o.MarketName).ToList();
        }
        private List<MarketDTO> GetOutrightMarkets(List<MarketFilter> markets, List<SelectionFilter> selections)
        {
            return markets.Select(m => new MarketDTO
            {
                MarketTemplateId = m.MaTeId,
                MarketId = m.MaId.ToString(),
                MarketName = m.MaNa,
                Line = m.Li,
                IsCashout = false,
                MarketStatusId = m.MaStId,
                MarketStatusName = m.MaStNa,
                Selections = selections.Where(c => c.MaId == m.MaId).Select(s => new SelectionDTO
                {
                    SelectionTemplateId = s.SeNa.ToLower() == "yes" ? "1" : s.SeNa.ToLower() == "no" ? "2" : s.SeTeId,
                    SelectionId = s.SeId.ToString(),
                    SelectionName = s.SeNa,
                    DecimalOdds = s.Od.ToString(),
                    IsActive = s.IsAc,
                    CorrelationId = s.CoId,
                }).OrderBy(o => o.SelectionTemplateId).ToList()
            }).OrderBy(o => o.MarketTemplateId).ThenBy(o => o.MarketName).ToList();
        }
        private BetSelectionState GetBetSelectionStatus(OddsChangeType oddsChangeType, BetFixtureSelectionRequestDTO current, Tuple<List<Fixture>, List<Market>, List<Selection>, BettingStatus> result)
        {
            var fixture = result.Item1.Where(c => c.FiId == current.FixtureId).FirstOrDefault();
            var market = result.Item2.Where(c => c.MaId == current.MarketId).FirstOrDefault();
            var selection = result.Item3.Where(c => c.SeId == current.SelectionId).FirstOrDefault();
            var bettingStatus = result.Item4;

            if (bettingStatus != null && bettingStatus.IsBeSo == true)
            {
                return new BetSelectionState
                {
                    SelectionId = current.SelectionId.ToString(),
                    Odds = current.Odds,
                    StatusCode = StatusCode.Betstop_On_Global,
                    Message = StatusCode.Betstop_On_Global.ToString().Replace('_', ' ')
                };
            }

            if (fixture == null || market == null || selection == null)
            {
                return new BetSelectionState
                {
                    SelectionId = current.SelectionId.ToString(),
                    Odds = current.Odds,
                    StatusCode = StatusCode.Market_Is_Inactive,
                    Message = StatusCode.Market_Is_Inactive.ToString().Replace('_', ' ')
                };
            }

            if (selection.FiId != current.FixtureId || selection.MaId != current.MarketId || selection.SeId != current.SelectionId || selection.SpId != current.SportId)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    Odds = selection.Od,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Is_Inactive,
                    Message = StatusCode.Market_Is_Inactive.ToString().Replace('_', ' '),
                };
            }

            if (fixture.IsSu == true && market.MaStId != (int)MarketStatus.Active)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    Odds = selection.Od,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Is_Inactive,
                    Message = StatusCode.Market_Is_Inactive.ToString().Replace('_', ' '),
                };
            }

            if (fixture.IsSu)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    Odds = selection.Od,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Betstop_On_Fixture,
                    Message = StatusCode.Betstop_On_Fixture.ToString().Replace('_', ' '),
                };
            }

            if (market.MaStId == (int)MarketStatus.Suspended)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    Odds = selection.Od,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Is_Suspended,
                    Message = StatusCode.Market_Is_Suspended.ToString().Replace('_', ' ')
                };
            }

            if (market.MaStId != (int)MarketStatus.Active)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    Odds = selection.Od,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Is_Inactive,
                    Message = StatusCode.Market_Is_Inactive.ToString().Replace('_', ' ')
                };
            }

            if (selection.IsAc == false)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    Odds = selection.Od,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Odds_Is_Inactive,
                    Message = StatusCode.Odds_Is_Inactive.ToString().Replace('_', ' ')
                };
            }

            switch (oddsChangeType)
            {
                case OddsChangeType.None:
                    if (current.Odds != selection.Od)
                    {
                        return new BetSelectionState
                        {
                            SelectionId = selection.SeId.ToString(),
                            Odds = selection.Od,
                            CorrelationId = selection.CoId,
                            StatusCode = StatusCode.Odds_Is_Stale,
                            Message = StatusCode.Odds_Is_Stale.ToString().Replace('_', ' ')
                        };
                    }
                    break;
                case OddsChangeType.Any:
                    break;
                case OddsChangeType.Higher:
                    if (current.Odds > selection.Od)
                    {
                        return new BetSelectionState
                        {
                            SelectionId = selection.SeId.ToString(),
                            Odds = selection.Od,
                            CorrelationId = selection.CoId,
                            StatusCode = StatusCode.Odds_Is_Stale,
                            Message = StatusCode.Odds_Is_Stale.ToString().Replace('_', ' ')
                        };
                    }
                    break;
                default:
                    break;
            }

            return new BetSelectionState
            {
                SelectionId = selection.SeId.ToString(),
                Odds = selection.Od,
                CorrelationId = selection.CoId,
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }
        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(ActiveFixturesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<ActiveFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ActiveFixturesResponseDTO()
                };
            }

            var result = fixtures.OrderBy(o => o.FiStDa).Select(s => new ActiveFixture
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                FixtureType = s.FiTyNa,
                FixtureId = s.FiId.ToString(),
                FixtureName = s.FiNa,
                Home = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                Away = s.FiTyId == (int)FixtureType.Match ? s.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                FixtureStatus = s.FiStNa,
                FixtureStartDate = s.FiStDa
            }).ToList();

            return new BaseResponseDTO<ActiveFixturesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ActiveFixturesResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        public async Task<BaseResponseDTO<ActiveFixtureCountResponseDTO>> GetActiveFixtureCount(ActiveFixtureCountRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<ActiveFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ActiveFixtureCountResponseDTO()
                };
            }

            var result = fixtures.GroupBy(g => g.SpId).Select(s => new ActiveFixtureCount
            {
                SportId = s.Key,
                SportName = s.Select(ss => ss.SpNa).FirstOrDefault(),
                PrematchCount = s.Where(c => c.FiTyId == (int)FixtureStatus.Prematch).Count(),
                LiveCount = s.Where(c => c.FiTyId == (int)FixtureStatus.Live).Count(),
            }).OrderBy(o => o.SportId).ToList();

            return new BaseResponseDTO<ActiveFixtureCountResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ActiveFixtureCountResponseDTO
                {
                    Sports = result
                }
            };
        }
        public async Task<BaseResponseDTO<SportsLookupResponseDTO>> GetSportsLookup()
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 12);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<SportsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SportsLookupResponseDTO()
                };
            }

            var result = fixtures.Select(s => new SportLookup
            {
                SportId = s.SpId,
                SportName = s.SpNa
            }).OrderBy(o => o.SportId).ToList();

            var finalResult = new List<SportLookup>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.SportId == item.SportId)) continue;

                finalResult.Add(item);
            }

            return new BaseResponseDTO<SportsLookupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SportsLookupResponseDTO
                {
                    Sports = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<SegmentsLookupResponseDTO>> GetSegmentsLookup()
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 12);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<SegmentsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SegmentsLookupResponseDTO()
                };
            }

            var result = fixtures.Select(s => new SegmentLookup
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa
            }).OrderBy(o => o.SegmentId).ToList();

            var finalResult = new List<SegmentLookup>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.SegmentId == item.SegmentId)) continue;

                finalResult.Add(item);
            }

            return new BaseResponseDTO<SegmentsLookupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SegmentsLookupResponseDTO
                {
                    Segments = finalResult
                }
            };
        }
        public async Task<BaseResponseDTO<LeaguesLookupResponseDTO>> GetLeaguesLookup()
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<LeaguesLookupResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new LeaguesLookupResponseDTO()
                };
            }

            var result = fixtures.Select(s => new LeagueLookup
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa
            }).OrderBy(o => o.LeagueId).ToList();

            var finalResult = new List<LeagueLookup>();

            foreach (var item in result)
            {
                if (finalResult.Any(c => c.LeagueId == item.LeagueId)) continue;

                finalResult.Add(item);
            }

            return new BaseResponseDTO<LeaguesLookupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new LeaguesLookupResponseDTO
                {
                    Leagues = finalResult
                }
            };
        }
        private List<string> GetMainMarketTemplates(int sportId)
        {
            switch (sportId)
            {
                case 1: //soccer
                    return new List<string> { "1", "16", "18" };
                case 2: //basketball
                    return new List<string> { "219", "223", "225" };
                case 3: //baseball
                    return new List<string> { "251", "256", "258" };
                case 4: //ice hockey
                    return new List<string> { "406", "410", "412" };
                case 5: //tennis
                    return new List<string> { "186", "187", "189" };
                case 6: //handball
                    return new List<string> { "1", "16", "18" };
                case 21: //cricket
                    return new List<string> { "1", "340" };
                case 16: //American Football
                    return new List<string> { "219", "223", "225" };
                case 13: //Aussie Rules
                    return new List<string> { "186", "16", "18" };
                case 31: //Badminton
                    return new List<string> { "186", "237", "238" };
                case 118: //Call of Duty
                    return new List<string> { "186" };
                case 22: //Darts
                    return new List<string> { "186", "188", "314" };
                case 109: //eSport CS GO
                    return new List<string> { "186", "327", "328" };
                case 111: //eSport Dota 2
                    return new List<string> { "186", "327", "328" };
                case 134: //ESport King of Glory
                    return new List<string> { "186" };
                case 110: //eSport LOL
                    return new List<string> { "186", "327", "328" };
                case 137: //eSoccer
                    return new List<string> { "1", "16", "18" };
                case 153: //eBasketball
                    return new List<string> { "219", "223", "225" };
                case 113: //Hearthstone
                    return new List<string> { "186" };
                case 121: //Overwatch
                    return new List<string> { "186" };
                case 128: //Rocket League
                    return new List<string> { "186" };
                case 12: //Rugby
                    return new List<string> { "1", "16", "18" };
                case 19: //Snooker
                    return new List<string> { "1", "186" };
                case 112: //StarCraft
                    return new List<string> { "186", "327", "328" };
                case 20: //Table Tennis
                    return new List<string> { "186", "327", "328" };
                case 1003: //Virtual Baseball
                    return new List<string> { "251", "256", "258" };
                case 1002: //Virtual Basketball
                    return new List<string> { "219", "223", "225" };
                case 1058: //Virtual Dog Racing
                    return new List<string> { "866" };
                case 1055: //Virtual Horse Racing
                    return new List<string> { "866" };
                case 1001: //Virtual Soccer
                    return new List<string> { "1", "16", "18" };
                case 1005: //Virtual Tennis
                    return new List<string> { "186", "187", "189" };
                case 23: //Volleyball
                    return new List<string> { "186", "237", "238" };
                default:
                    return new List<string>();
            }
        }
        private Tuple<DateTime, DateTime> GetDateTimeRange(DateTime utcDate, int minuteUTCOffset)
        {
            var hours = minuteUTCOffset / 60;
            var remainderMinutes = minuteUTCOffset % 60;

            if (hours >= 0)
            {
                var utc = utcDate.Date;
                var startTime = utc.AddHours(hours * -1).AddMinutes(remainderMinutes * -1).AddMilliseconds(-1);
                var endTime = utc.AddHours(hours).Date.AddDays(1).AddHours(hours * -1).AddMinutes(remainderMinutes * -1);

                return new Tuple<DateTime, DateTime>(startTime, endTime);
            }
            else
            {
                var utc = utcDate.Date;
                var startTime = utc.AddHours(hours * -1).AddMinutes(remainderMinutes * -1).AddMilliseconds(-1);
                var endTime = startTime.AddDays(1);

                return new Tuple<DateTime, DateTime>(startTime, endTime);
            }
        }
        private List<TierLeagueOrder> GetTierLeagueOrder()
        {
            return _memCache.GetCache($"tlo", () => _sportsbookContext.TierLeagueOrder.ToList(), 24);
        }
        public async Task<BaseResponseDTO<StatusCode>> ManualSettlement(BetSettlementRequestDTO request)
        {
            var digit = Convert.ToString(request.FixtureId);
            var lastDigit = digit.Substring(digit.Length - 1);
            var selections = new List<InternalSelectionSettlementMessagePayload>();

            foreach (var selection in request.Selections)
            {
                selections.Add(new InternalSelectionSettlementMessagePayload
                {
                    MarketId = long.Parse(selection.MarketId),
                    SelectionId = long.Parse(selection.SelectionId),
                    SettlementStatus = selection.SettlementStatus
                });
            }

            if (request.Selections.Count > 0)
            {
                var payload = new BetSettlementRequestPayloadDTO
                {
                    FixtureId = long.Parse(request.FixtureId),
                    Selections = selections
                };

                _messageBroker.Publish(payload, "OnBetSettlement", $"r.sb.adapter.{lastDigit}.onbetsettlement", Guid.NewGuid().ToString());
            }

            return new BaseResponseDTO<StatusCode>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }
        public async Task<BaseResponseDTO<StatusCode>> ManualCancellation(BetCancellationRequestDTO request)
        {
            var digit = Convert.ToString(request.FixtureId);
            var lastDigit = digit.Substring(digit.Length - 1);

            var markets = new List<InternalMarketCancellationMessagePayload>();

            foreach (var market in request.Markets)
            {
                markets.Add(new InternalMarketCancellationMessagePayload
                {
                    MarketId = long.Parse(market.MarketId),
                    Reason = market.Reason,
                    SettlementStatus = SettlementStatus.Cancelled
                });
            }

            if (request.Markets.Count > 0)
            {
                var payload = new BetCancellationRequestPayloadDTO
                {
                    FixtureId = long.Parse(request.FixtureId),
                    Markets = markets
                };

                _messageBroker.Publish(payload, "OnBetCancellation", $"r.sb.adapter.{lastDigit}.onbetcancellation", Guid.NewGuid().ToString());
            }

            return new BaseResponseDTO<StatusCode>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }
    }

    public static class CurrentPeriodHelper
    {
        public static string ToShortName(int currentPeriod)
        {
            var result = string.Empty;

            switch (currentPeriod)
            {
                case 1:
                    result = "1P"; //First Period
                    break;
                case 2:
                    result = "2P"; //Second Period
                    break;
                case 3:
                    result = "3P"; //Third Period
                    break;
                case 4:
                    result = "4P"; //Fourth Period
                    break;
                case 5:
                    result = "5P"; //Fifth Period
                    break;
                case 6:
                    result = "1H"; //FirstHalf
                    break;
                case 7:
                    result = "2H"; //SecondHalf
                    break;
                case 8:
                    result = "1S"; //FirstSet
                    break;
                case 9:
                    result = "2S"; //SecondSet
                    break;
                case 10:
                    result = "3S"; //ThirdSet
                    break;
                case 11:
                    result = "4S"; //FourthSet
                    break;
                case 12:
                    result = "5S"; //FifthSet
                    break;
                case 13:
                    result = "1Q"; //FirstQuarter
                    break;
                case 14:
                    result = "2Q"; //SecondQuarter
                    break;
                case 15:
                    result = "3Q"; //ThirdQuarter
                    break;
                case 16:
                    result = "4Q"; //FourthQuarter
                    break;
                case 17:
                    result = "Golden set"; //Golden set
                    break;
                case 20:
                    result = "Started"; //Started
                    break;
                case 21:
                    result = "In progress"; //In progress
                    break;
                case 22:
                    result = "About to start"; //About to start
                    break;
                case 30:
                    result = "Break"; //Break
                    break;
                case 31:
                    result = "Halftime"; //Halftime
                    break;
                case 32:
                    result = "Awaiting extra time"; //Awaiting extra time
                    break;
                case 33:
                    result = "Extra time halftime"; //Extra time halftime
                    break;
                case 34:
                case 35:
                case 36:
                    result = "Awaiting penalties"; //Awaiting penalties
                    break;
                case 37:
                    result = "Awaiting golden set"; //Awaiting golden set
                    break;
                case 40:
                    result = "Overtime"; //Overtime
                    break;
                case 41:
                    result = "1E"; //1st extra
                    break;
                case 42:
                    result = "2E"; //2nd extra
                    break;
                case 50:
                case 51:
                case 52:
                    result = "Penalties"; //Penalties
                    break;
                case 60:
                    result = "Postponed"; //Postponed
                    break;
                case 61:
                    result = "Start delayed"; //Start delayed
                    break;
                case 70:
                    result = "Cancelled";
                    break;
                case 71:
                    result = "G1"; //Game 1
                    break;
                case 72:
                    result = "G2"; //Game 2
                    break;
                case 73:
                    result = "G3"; //Game 3
                    break;
                case 74:
                    result = "G4"; //Game 4
                    break;
                case 75:
                    result = "G5"; //Game 5
                    break;
                case 76:
                    result = "G6"; //Game 6
                    break;
                case 77:
                    result = "G7"; //Game 7
                    break;
                case 80:
                    result = "Interrupted";
                    break;
                case 81:
                    result = "Suspended";
                    break;
                case 90:
                    result = "Abandoned";
                    break;
                case 91:
                    result = "Walkover";
                    break;
                case 92:
                    result = "Retired";
                    break;
                case 93:
                    result = "Walkover, player 1 won";
                    break;
                case 94:
                    result = "Walkover, player 2 won";
                    break;
                case 95:
                    result = "Player 1 retired, player 2 won";
                    break;
                case 96:
                    result = "Player 2 retired, player 1 won";
                    break;
                case 97:
                    result = "Player 1 defaulted, player 2 won";
                    break;
                case 98:
                    result = "Player 2 defaulted, player 1 won";
                    break;
                case 99:
                    result = "Only Result";
                    break;
                case 100:
                    result = "Ended";
                    break;
                case 110:
                    result = "Awaiting extra time";
                    break;
                case 111:
                    result = "Awaiting over time";
                    break;
                case 120:
                    result = "Awaiting Penalties";
                    break;
                case 130:
                    result = "After golden set";
                    break;
                case 141:
                    result = "1st map";
                    break;
                case 142:
                    result = "2nd map";
                    break;
                case 143:
                    result = "3rd map";
                    break;
                case 144:
                    result = "4th map";
                    break;
                case 145:
                    result = "5th map";
                    break;
                case 146:
                    result = "6th map";
                    break;
                case 147:
                    result = "7th map";
                    break;
                case 151:
                    result = "1st Game";
                    break;
                case 152:
                    result = "2nd Game";
                    break;
                case 153:
                    result = "3rd Game";
                    break;
                case 154:
                    result = "4th Game";
                    break;
                case 155:
                    result = "5th Game";
                    break;
                case 161:
                    result = "1st end";
                    break;
                case 162:
                    result = "2nd end";
                    break;
                case 163:
                    result = "3rd end";
                    break;
                case 164:
                    result = "4th end";
                    break;
                case 165:
                    result = "5th end";
                    break;
                case 166:
                    result = "6th end";
                    break;
                case 167:
                    result = "7th end";
                    break;
                case 168:
                    result = "8th end";
                    break;
                case 169:
                    result = "9th end";
                    break;
                case 170:
                    result = "10th end";
                    break;
                case 171:
                    result = "Extra end";
                    break;
                case 301:
                    result = "First break";
                    break;
                case 302:
                    result = "Second break";
                    break;
                case 303:
                    result = "Third break";
                    break;
                case 304:
                    result = "Fourth break";
                    break;
                case 305:
                    result = "Fifth break";
                    break;
                case 306:
                    result = "Sixth break";
                    break;
                case 401:
                    result = "1st inning top";
                    break;
                case 402:
                    result = "1st inning bottom";
                    break;
                case 403:
                    result = "2nd inning top";
                    break;
                case 404:
                    result = "2nd inning bottom";
                    break;
                case 405:
                    result = "3rd inning top";
                    break;
                case 406:
                    result = "3rd inning bottom";
                    break;
                case 407:
                    result = "4th inning top";
                    break;
                case 408:
                    result = "4th inning bottom";
                    break;
                case 409:
                    result = "5th inning top";
                    break;
                case 410:
                    result = "5th inning bottom";
                    break;
                case 411:
                    result = "6th inning top";
                    break;
                case 412:
                    result = "6th inning bottom";
                    break;
                case 413:
                    result = "7th inning top";
                    break;
                case 414:
                    result = "7th inning bottom";
                    break;
                case 415:
                    result = "8th inning top";
                    break;
                case 416:
                    result = "8th inning bottom";
                    break;
                case 417:
                    result = "9th inning top";
                    break;
                case 418:
                    result = "9th inning bottom";
                    break;
                case 419:
                    result = "Extra inning top";
                    break;
                case 420:
                    result = "Extra inning bottom";
                    break;
                case 421:
                    result = "Break top1-bottom1";
                    break;
                case 422:
                    result = "Break top2-bottom1";
                    break;
                case 423:
                    result = "Break top2-bottom2";
                    break;
                case 424:
                    result = "Break top3-bottom2";
                    break;
                case 425:
                    result = "Break top3-bottom3";
                    break;
                case 426:
                    result = "Break top4-bottom3";
                    break;
                case 427:
                    result = "Break top4-bottom4";
                    break;
                case 428:
                    result = "Break top5-bottom4";
                    break;
                case 429:
                    result = "Break top5-bottom5";
                    break;
                case 430:
                    result = "Break top6-bottom5";
                    break;
                case 431:
                    result = "Break top6-bottom6";
                    break;
                case 432:
                    result = "Break top7-bottom6";
                    break;
                case 433:
                    result = "Break top7-bottom7";
                    break;
                case 434:
                    result = "Break top8-bottom7";
                    break;
                case 435:
                    result = "Break top8-bottom8";
                    break;
                case 436:
                    result = "Break top9-bottom8";
                    break;
                case 437:
                    result = "Break top9-bottom9";
                    break;
                case 438:
                    result = "Break topEI-bottom9";
                    break;
                case 439:
                    result = "Break topEI-bottomEI";
                    break;
                case 440:
                    result = "Sudden death";
                    break;
                case 441:
                    result = "6th set";
                    break;
                case 442:
                    result = "7th set";
                    break;
                case 443:
                    result = "Awaiting sudden death";
                    break;
                case 444:
                    result = "After sudden death";
                    break;
                case 445:
                    result = "Break";
                    break;
                case 501:
                    result = "First innings, home team";
                    break;
                case 502:
                    result = "First innings, away team";
                    break;
                case 503:
                    result = "Second innings, home team";
                    break;
                case 504:
                    result = "Second innings, away team";
                    break;
                case 505:
                    result = "Awaiting super over";
                    break;
                case 506:
                    result = "Super over, home team";
                    break;
                case 507:
                    result = "Super over, away team";
                    break;
                case 508:
                    result = "After super over";
                    break;
                case 509:
                    result = "Innings break";
                    break;
                case 510:
                    result = "Super over break";
                    break;
                case 511:
                    result = "Lunch break";
                    break;
                case 512:
                    result = "Tea break";
                    break;
                case 513:
                    result = "Stumps";
                    break;
                case 514:
                    result = "8th set";
                    break;
                case 515:
                    result = "9th set";
                    break;
                case 516:
                    result = "10th set";
                    break;
                case 517:
                    result = "11th set";
                    break;
                case 518:
                    result = "12th set";
                    break;
                case 519:
                    result = "13th set";
                    break;
                case 520:
                    result = "Third innings, home team";
                    break;
                case 521:
                    result = "Third innings, away team";
                    break;
                case 522:
                    result = "Fourth innings, home team";
                    break;
                case 523:
                    result = "Fourth innings, away team";
                    break;
                case 524:
                    result = "Dinner Break";
                    break;
                case 525:
                    result = "Drinks";
                    break;
                case 526:
                    result = "Super over";
                    break;
                case 531:
                    result = "1st inning";
                    break;
                case 532:
                    result = "2nd inning";
                    break;
                case 533:
                    result = "3rd inning";
                    break;
                case 534:
                    result = "4th inning";
                    break;
                case 535:
                    result = "5th inning";
                    break;
                case 536:
                    result = "6th inning";
                    break;
                case 537:
                    result = "7th inning";
                    break;
                case 538:
                    result = "8th inning";
                    break;
                case 539:
                    result = "9th inning";
                    break;
                default:
                    result = "None";
                    break;
            }

            return result;
        }
    }
}
