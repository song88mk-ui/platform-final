using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Data.Sportsbook.Entities;
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
        private readonly IMessageBroker _messageBroker;
        private readonly SportsbookContext _sportsbookContext;
        private IMongoCollection<Fixture> fixtureCollection;
        private IMongoCollection<Market> marketCollection;
        private IMongoCollection<Selection> selectionCollection;
        private readonly HttpClient _httpClient;

        public SportsbookRepository(SportsbookContext sportsbookContext, HttpClient httpClient, IMessageBroker messageBroker)
        {
            var client = new MongoClient("mongodb+srv://lino:fcisong88@production.n6svy.mongodb.net/sportsbook?retryWrites=true&w=0");
            _db = client.GetDatabase("Sportsbook");

            fixtureCollection = _db.GetCollection<Fixture>("Fixture");
            marketCollection = _db.GetCollection<Market>("Market");
            selectionCollection = _db.GetCollection<Selection>("Selection");

            _sportsbookContext = sportsbookContext;
            _httpClient = httpClient;
            _messageBroker = messageBroker;
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
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.SpId == request.SportId && c.FiTyId == 0 && c.FiStId < 2);
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

            var sortOrder = await _sportsbookContext.TierLeagueOrder.Where(c => c.SportId == request.SportId).ToListAsync();

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

                finalResult.Add(item);
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
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiTyId == 0 && c.FiStId < 1);
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

            var sortOrder = await _sportsbookContext.TierLeagueOrder.ToListAsync();

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
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.SpId == request.SportId && c.FiStId < 2);
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

            var sortOrder = await _sportsbookContext.TierLeagueOrder.Where(c => c.SportId == request.SportId).ToListAsync();

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
                Prematch = s.Where(c => c.FixtureStatusId == (int)FixtureStatus.Prematch && c.MarketTemplates != "0").Select(p => new MarketTemplateGroup
                {
                    MarketTemplateGroupId = p.Id,
                    MarketTemplateGroupName = p.Name,
                    SortOrder = p.SortOrder,
                    MarketTemplateIds = p.MarketTemplates.Split(',').Select(ss => ss.ToString()).ToList()
                }).OrderBy(o => o.MarketTemplateGroupId).ToList(),
                Live = s.Where(c => c.FixtureStatusId == (int)FixtureStatus.Live && c.MarketTemplates != "0").Select(l => new MarketTemplateGroup
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
        private async Task<Tuple<List<Fixture>, List<Market>, List<Selection>>> GetBetFixtures(BetFixtureRequestDTO request)
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

            //var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BetStop == "betstop");
            //var bettingStatusTask = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(fixtures, markets, selections);

            return new Tuple<List<Fixture>, List<Market>, List<Selection>>(fixtures.Result, markets.Result, selections.Result);
        }
        public async Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request)
        {
            var result = await GetBetFixtures(request);

            if (result.Item3.Count == 0)
            {
                return new BaseResponseDTO<BetFixtureResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetFixtureResponseDTO()
                };
            }

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
                        CorrelationId = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.CoId).FirstOrDefault()
                    }).ToList()
                }
            };
        }

        private Temp GetBetSelectionStatus(long selectionId, BetFixtureRequestDTO request, Tuple<List<Fixture>, List<Market>, List<Selection>> result)
        {
            return new Temp();
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
    }
}
