using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
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
        private IMongoCollection<Fixture> fixtureCollection;
        private readonly HttpClient _httpClient;

        public SportsbookRepository(HttpClient httpClient)
        {
            var client = new MongoClient("mongodb+srv://lino:fcisong88@production.n6svy.mongodb.net/sportsbook?retryWrites=true&w=0");
            _db = client.GetDatabase("Sportsbook");

            fixtureCollection = _db.GetCollection<Fixture>("Fixture");

            _httpClient = httpClient;
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
                        Message = $"{result.Error.Code} : {result.Error.Message}",
                        Result = new LivestreamResponseDTO
                        {
                            LiveStreamURL = result.Url
                        }
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

            var result = fixtures.OrderBy(o => o.FiStDa).Select(s => new TopLeague 
            {
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa
            }).ToList();

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

            var result = fixtures.OrderBy(o => o.FiStDa).Select(s => new TopFixture
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                SegmentId = s.SegId,
                SegmentName = s.SegNa,
                LeagueId = s.LeId,
                LeagueName = s.LeNa,
                FixtureId = s.FiId,
                FixtureName = s.FiNa
            }).Take(10).ToList();

            return new BaseResponseDTO<TopFixturesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TopFixturesResponseDTO
                {
                    TopFixtures = result
                }
            };
        }
    }
}
