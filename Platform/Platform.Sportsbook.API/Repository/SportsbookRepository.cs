using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Platform.Shared.Data.Platform;
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
using System.Transactions;
using System.Xml;

namespace Platform.Sportsbook.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private static IMongoDatabase _db;
        private readonly IMemCache _memCache;
        private readonly SportsbookContext _sportsbookContext;
        private readonly IConfiguration _configuration;
        private static IMongoCollection<Fixture> fixtureCollection;
        private static IMongoCollection<Market> marketCollection;
        private static IMongoCollection<MarketMapping> marketMappingCollection;
        private static IMongoCollection<Selection> selectionCollection;
        private static IMongoCollection<BettingStatus> bettingStatusCollection;

        private static IMongoCollection<Fixture> primaryFixtureCollection;
        private static IMongoCollection<Market> primaryMarketCollection;
        private static IMongoCollection<Selection> primarySelectionCollection;
        private static IMongoCollection<BettingStatus> primaryBettingStatusCollection;
        private readonly HttpClient _httpClient;
        private readonly IRabbitMQRepository _rabbitMQRepository;
        private readonly List<string> _africaClientCodes = new List<string>();
        private readonly List<string> _countryWhiteList = new List<string>();

        public SportsbookRepository(SportsbookContext sportsbookContext, HttpClient httpClient, IMemCache memCache, IConfiguration configuration, IRabbitMQRepository rabbitMQRepository)
        {
            _configuration = configuration;

            _africaClientCodes = _configuration["ConnectionStrings:MongoDB-Sportsbook"].Split(',').ToList();
            _countryWhiteList = _configuration["Custom:CountryWhiteList"].Split(',').ToList();

            if (_db == null)
            {
                var client = new MongoClient(_configuration["ConnectionStrings:MongoDB-Sportsbook"]);
                _db = client.GetDatabase("Sportsbook");

                if (bool.Parse(_configuration["Custom:IsNearestReplica"]))
                {
                    fixtureCollection = _db.GetCollection<Fixture>("Fixture").WithReadPreference(new ReadPreference(ReadPreferenceMode.Nearest));
                    marketCollection = _db.GetCollection<Market>("Market").WithReadPreference(new ReadPreference(ReadPreferenceMode.Nearest));
                    marketMappingCollection = _db.GetCollection<MarketMapping>("MarketMapping").WithReadPreference(new ReadPreference(ReadPreferenceMode.Nearest));
                    selectionCollection = _db.GetCollection<Selection>("Selection").WithReadPreference(new ReadPreference(ReadPreferenceMode.Nearest));
                    bettingStatusCollection = _db.GetCollection<BettingStatus>("BettingStatus").WithReadPreference(new ReadPreference(ReadPreferenceMode.Nearest));

                    primaryFixtureCollection = _db.GetCollection<Fixture>("Fixture").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                    primaryMarketCollection = _db.GetCollection<Market>("Market").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                    primarySelectionCollection = _db.GetCollection<Selection>("Selection").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                    primaryBettingStatusCollection = _db.GetCollection<BettingStatus>("BettingStatus").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                }
                else
                {
                    fixtureCollection = _db.GetCollection<Fixture>("Fixture").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                    marketCollection = _db.GetCollection<Market>("Market").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                    marketMappingCollection = _db.GetCollection<MarketMapping>("MarketMapping").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                    selectionCollection = _db.GetCollection<Selection>("Selection").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                    bettingStatusCollection = _db.GetCollection<BettingStatus>("BettingStatus").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));

                    primaryFixtureCollection = _db.GetCollection<Fixture>("Fixture").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                    primaryMarketCollection = _db.GetCollection<Market>("Market").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                    primarySelectionCollection = _db.GetCollection<Selection>("Selection").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                    primaryBettingStatusCollection = _db.GetCollection<BettingStatus>("BettingStatus").WithReadPreference(new ReadPreference(ReadPreferenceMode.Primary));
                }
            }

            _sportsbookContext = sportsbookContext;
            _httpClient = httpClient;
            _memCache = memCache;

            _rabbitMQRepository = rabbitMQRepository;

            _httpClient.BaseAddress = new Uri("https://av-api.betradar.com");
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer D6ewpdvADa8k5U7Xcx");
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

            if (!string.IsNullOrEmpty(fixture.GeAl) && !string.IsNullOrEmpty(request.IPCountryCode))
            {
                if (_countryWhiteList.Contains(request.IPCountryCode.ToUpper()))
                {

                }
                else
                {
                    if (!fixture.GeAl.Split(",").Any(g => g.ToUpper() == request.IPCountryCode.ToUpper()))
                        return new BaseResponseDTO<LivestreamResponseDTO>
                        {
                            StatusCode = StatusCode.Livestream_Is_Not_Available_In_Your_Region,
                            Message = StatusCode.Livestream_Is_Not_Available_In_Your_Region.ToString().Replace('_', ' ')
                        };
                }
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

        public async Task<BaseResponseDTO<BetSettlementScoreResponseDTO>> GetBetSettlementScore(BetSettlementScoreRequestDTO request)
        {
            var setlementScore = string.Empty;
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiId == request.FixtureId);
            var fixture = await fixtureCollection.Find(fixtureFilter).FirstOrDefaultAsync();

            if (fixture == null)
            {
                return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                {
                    StatusCode = StatusCode.No_Record_Found,
                    Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                };
            }

            try
            {

                #region SR API to get score
                //using (var client = new HttpClient())
                //{
                //    client.BaseAddress = new Uri("https://api.betradar.com");
                //    client.DefaultRequestHeaders.Add("x-access-token", "wLCaM7vc80CADU33rz");

                //    var response = await client.GetAsync($"/v1/sports/en/sport_events/{fixture.FiSp}/summary.xml");

                //    if (response.IsSuccessStatusCode)
                //    {
                //        var result = await response.Content.ReadAsStringAsync();

                //        XmlDocument doc = new XmlDocument();

                //        doc.LoadXml(result);

                //        #region Validate feed

                //        var sportStatusNode = doc.ChildNodes[1].ChildNodes[2];

                //        var _endMatchStatus = new[] { (int)MatchStatus.Ended, (int)MatchStatus.AET, (int)MatchStatus.AOT, (int)MatchStatus.AP, (int)MatchStatus.Retired };

                //        // applicable to all sports
                //        // if there are still no final home or away score yet skip the current iteration.
                //        if (sportStatusNode.Attributes["home_score"] == null || sportStatusNode.Attributes["away_score"] == null)
                //        {
                //            return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                //            {
                //                StatusCode = StatusCode.No_Record_Found,
                //                Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                //            };
                //        }
                //        else
                //        {
                //            setlementScore = $"{sportStatusNode.Attributes["home_score"].Value} - {sportStatusNode.Attributes["away_score"].Value}";
                //        }

                //        if (sportStatusNode.ChildNodes.Count > 0)
                //        {
                //            //get from result node
                //            var resultsNode = sportStatusNode.ChildNodes[1];
                //            if(resultsNode.ChildNodes.Count > 0)
                //            {
                //                var resultNode = resultsNode.ChildNodes[0];
                //                var resultMatchStatusCode = resultNode.Attributes["match_status_code"] == null ? 0 : Convert.ToInt32(resultNode.Attributes["match_status_code"].Value);
                //                if (_endMatchStatus.Contains(resultMatchStatusCode))
                //                {
                //                    if (resultNode.Attributes["home_score"] == null || resultNode.Attributes["away_score"] == null)
                //                    {
                //                        return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                //                        {
                //                            StatusCode = StatusCode.No_Record_Found,
                //                            Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                //                        };
                //                    }
                //                    setlementScore = $"{resultNode.Attributes["home_score"].Value} - {resultNode.Attributes["away_score"].Value}";
                //                }
                //            }

                //        }

                //        #endregion

                //        return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                //        {
                //            StatusCode = StatusCode.Success,
                //            Message = StatusCode.Success.ToString(),
                //            Result = new BetSettlementScoreResponseDTO
                //            {
                //                Score = setlementScore,
                //            }
                //        };
                //    }
                //    else
                //    {
                //        return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                //        {
                //            StatusCode = StatusCode.No_Record_Found,
                //            Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                //        };
                //    }
                //}

                #endregion

                setlementScore = fixture.Sc;

                return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetSettlementScoreResponseDTO
                    {
                        Score = setlementScore,
                    }
                };

            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }


        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(TopLeaguesRequestDTO request)
        {
            var mKey = $"ntl:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TopLeaguesResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TopLeaguesResponseDTO()
                };
            }

            var result = new List<TempTopLeague>();

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var clientTopLeagues = new List<ClientTopLeague>();

            var key = $"clo:{request.ClientId.ToString().ToUpper()}";

            var cache = _memCache.GetCache<List<ClientTopLeague>>(key);

            if (cache != null)
                clientTopLeagues = cache;
            else
            {
                clientTopLeagues = await _sportsbookContext.ClientTopLeague.Where(c => c.ClientId == request.ClientId).ToListAsync();
                _memCache.AddOrUpdateCache(key, clientTopLeagues, DateTimeOffset.UtcNow.AddMinutes(5));
            }

            if (clientTopLeagues.Count == 0)
            {
                result = fixtures.Select(s => new TempTopLeague
                {
                    SegmentId = s.SegId,
                    SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                    LeagueId = s.LeId,
                    LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                    SortOrder = sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();
            }
            else
            {
                foreach (var league in clientTopLeagues.OrderBy(o => o.SortOrder).ToList())
                {
                    var res = fixtures.Where(c => c.SegId == league.SegmentId && c.LeId == league.LeagueId).FirstOrDefault();

                    if (res != null)
                    {
                        if (!result.Any(c => c.SegmentId == res.SegId && c.LeagueId == res.LeId))
                        {
                            result.Add(new TempTopLeague
                            {
                                SegmentId = res.SegId,
                                SegmentName = GetSegmentName(res.SegNa, res.SegNaTr, request.LanguageCode),
                                LeagueId = res.LeId,
                                LeagueName = GetLeagueName(res.LeNa, res.LeNaTr, request.LanguageCode),
                                SortOrder = league.SortOrder
                            });
                        }
                    }
                }

                if (result.Count == 0)
                {
                    result = fixtures.Select(s => new TempTopLeague
                    {
                        SegmentId = s.SegId,
                        SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                        LeagueId = s.LeId,
                        LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                        SortOrder = sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                         == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();
                }
            }

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

            var returnResult = new BaseResponseDTO<TopLeaguesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TopLeaguesResponseDTO
                {
                    TopLeagues = finalResult
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(TopLeaguesRequestDTO request)
        {
            var mKey = $"nal:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TopLeaguesResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TopLeaguesResponseDTO()
                };
            }

            var result = fixtures.Select(s => new TempTopLeague
            {
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
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

            var returnResult = new BaseResponseDTO<TopLeaguesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TopLeaguesResponseDTO
                {
                    TopLeagues = finalResult
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }

        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(TopLeaguesRequest2DTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

            //Addtional Filter
            if (request.SegmentId > 0)
                fixtures = fixtures.Where(s => s.SegId == request.SegmentId).ToList();

            if (request.LeagueId > 0)
                fixtures = fixtures.Where(s => s.LeId == request.LeagueId).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TopLeaguesResponseDTO()
                };
            }

            var result = fixtures.Select(s => new TempTopLeague
            {
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
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
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 1 && c.FiTyId == (int)FixtureType.Match);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000).ToList();

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
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
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
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000).ToList();

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
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
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
            var mKey = $"ns:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportGroup.ToString().ToUpper()}";

            var mCache = _memCache.GetCache<BaseResponseDTO<SportsResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);

            if (request.SportGroup != Shared.Enums.SportGroup.All)
            {
                var sportGroup = GetSportGroups(request.SportGroup);
                var selectedSportIds = sportGroup != null ? sportGroup.Sports.Split(',').Select(int.Parse).Distinct().ToList() : new List<int>();

                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId < 3 && selectedSportIds.Contains(c.SpId));
            }

            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

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

            var leagueSortOrder = GetTierLeagueOrder().Where(c => sportIds.Contains(c.SportId)).ToList();
            var sportSortOrder = GetSportOrder().Where(c => c.ClientId == request.ClientId && c.IsActive == true).ToList();
            var segmentOrder = GetSegmentOrder().Where(c => c.ClientId == request.ClientId).ToList();
            var leagueOrder = GetLeagueOrder().Where(c => c.ClientId == request.ClientId).ToList();
            var activeSportIds = sportSortOrder.Select(s => s.SportId).ToList();

            var result = fixtures.Where(c => activeSportIds.Contains(c.SpId) && c.FiTyId == (int)FixtureType.Match).Select(s => new TempOrderedLeague
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                LeagueSortOrder = leagueOrder.FirstOrDefault(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId)?.SortOrder ??
                    leagueSortOrder.FirstOrDefault(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId)?.SortOrder + 1000 ?? 2000
            }).ToList();

            var sportResult = fixtures.Where(c => activeSportIds.Contains(c.SpId)).Select(s => new TempOrderedSport
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                HasOutrights = fixtures.Any(c => c.SpId == s.SpId && c.FiTyId == (int)FixtureType.Outright),
                SortOrder = sportSortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sportSortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
            }).ToList().OrderBy(o => o.SortOrder).ThenBy(o => o.SportName).ToList();

            var finalResult = new List<OrderedSport>();

            foreach (var item in sportResult)
            {
                if (finalResult.Any(c => c.SportId == item.SportId)) continue;

                finalResult.Add(item);

                if (item.SportId == 1055 || item.SportId == 1058)
                {
                    var temp = fixtures.Where(c => c.SpId == item.SportId).FirstOrDefault();

                    if (temp != null)
                    {
                        result.Add(new TempOrderedLeague
                        {
                            SportId = temp.SpId,
                            SportName = GetSportName(temp.SpNa, temp.SpNaTr, request.LanguageCode),
                            SegmentId = temp.SegId,
                            SegmentName = GetSegmentName(temp.SegNa, temp.SegNaTr, request.LanguageCode),
                            LeagueId = temp.LeId,
                            LeagueName = GetLeagueName(temp.LeNa, temp.LeNaTr, request.LanguageCode),
                            LeagueSortOrder = 2000
                        });
                    }
                }
                else
                {
                    if (item.HasOutrights)
                    {
                        result.Add(new TempOrderedLeague
                        {
                            SportId = item.SportId,
                            SportName = string.Empty,
                            SegmentId = 999999,
                            SegmentName = "Outrights",
                            LeagueId = 0,
                            LeagueName = string.Empty,
                            LeagueSortOrder = 99999
                        });
                    }
                }
            }

            var final = new List<SportDTO>();

            foreach (var sport in finalResult)
            {
                if (sport.SportId == 5)
                {
                    final.Add(new SportDTO
                    {
                        SportId = sport.SportId,
                        SportName = sport.SportName,
                        Segments = result.Where(c => c.SportId == sport.SportId).GroupBy(gse => gse.SegmentId).Select(s => new SegmentDTO
                        {
                            SegmentId = s.Key,
                            SegmentName = s.Select(se => se.SegmentName).FirstOrDefault(),
                            SortOrder = s.Select(se => se.LeagueSortOrder).FirstOrDefault(),
                            Leagues = s.GroupBy(gle => gle.LeagueId).Select(l => new LeagueDTO
                            {
                                LeagueId = l.Key,
                                LeagueName = l.Select(le => le.LeagueName).FirstOrDefault(),
                                SortOrder = l.Select(le => le.LeagueSortOrder).FirstOrDefault()
                            }).OrderBy(o => o.LeagueId).ThenBy(o => o.LeagueName).ToList()
                        }).OrderBy(o => o.SegmentId).ThenBy(o => o.SegmentName).ToList()
                    });
                }
                else if (segmentOrder.Any(c => c.SportId == sport.SportId))
                {
                    var segment = segmentOrder.Where(c => c.SportId == sport.SportId).ToList();

                    final.Add(new SportDTO
                    {
                        SportId = sport.SportId,
                        SportName = sport.SportName,
                        Segments = result.Where(c => c.SportId == sport.SportId).GroupBy(gse => gse.SegmentId).Select(s => new SegmentDTO
                        {
                            SegmentId = s.Key,
                            SegmentName = s.Select(se => se.SegmentName).FirstOrDefault(),
                            SortOrder = segment?.FirstOrDefault(c => c.SegmentId == s.Key)?.SortOrder == null && s.Select(se => se.SegmentId).FirstOrDefault() == 999999 ? 2000
                                        : segment?.FirstOrDefault(c => c.SegmentId == s.Key)?.SortOrder ?? 1000,
                            Leagues = s.GroupBy(gle => gle.LeagueId).Select(l => new LeagueDTO
                            {
                                LeagueId = l.Key,
                                LeagueName = l.Select(le => le.LeagueName).FirstOrDefault(),
                                SortOrder = l.Select(le => le.LeagueSortOrder).FirstOrDefault()
                            }).OrderBy(o => o.SortOrder).ThenBy(o => o.LeagueName).ToList()
                        }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ToList()
                    });
                }
                else
                {
                    final.Add(new SportDTO
                    {
                        SportId = sport.SportId,
                        SportName = sport.SportName,
                        Segments = result.Where(c => c.SportId == sport.SportId).GroupBy(gse => gse.SegmentId).Select(s => new SegmentDTO
                        {
                            SegmentId = s.Key,
                            SegmentName = s.Select(se => se.SegmentName).FirstOrDefault(),
                            SortOrder = s.Select(se => se.LeagueSortOrder).FirstOrDefault(),
                            Leagues = s.GroupBy(gle => gle.LeagueId).Select(l => new LeagueDTO
                            {
                                LeagueId = l.Key,
                                LeagueName = l.Select(le => le.LeagueName).FirstOrDefault(),
                                SortOrder = l.Select(le => le.LeagueSortOrder).FirstOrDefault()
                            }).OrderBy(o => o.SortOrder).ThenBy(o => o.LeagueName).ToList()
                        }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ToList()
                    });
                }
            }

            var returnResult = new BaseResponseDTO<SportsResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SportsResponseDTO
                {
                    Sports = final
                }
            };

            if (final.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(120));

            return returnResult;
        }
        public async Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups()
        {
            var result = _memCache.GetCache<BaseResponseDTO<MarketTemplateGroupResponseDTO>>("mtg");

            if (result != null)
                return result;

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

            var initialResult = new BaseResponseDTO<MarketTemplateGroupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new MarketTemplateGroupResponseDTO
                {
                    Sports = sportGroup
                }
            };

            if (initial.Count > 0)
                _memCache.AddOrUpdateCache("mtg", initialResult, DateTimeOffset.UtcNow.AddDays(1));

            return initialResult;
        }
        public async Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates()
        {
            var result = GetSelectorMarketTemplate();
            var finalResult = new List<SportSelectorMarketTemplateDTO>();

            foreach (var sport in result.GroupBy(g => g.SportId).ToList())
            {
                finalResult.Add(new SportSelectorMarketTemplateDTO
                {
                    SportId = sport.Key,
                    MarketTemplates = sport.Select(s => new SelectorMarketTemplateDTO
                    {
                        MarketTemplateId = s.MarketTemplateId,
                        MarketTemplateCode = s.MarketTemplateCode,
                        SortOrder = s.SortOrder
                    }).OrderBy(o => o.SortOrder).ToList()
                });
            }

            return new BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = finalResult
            };
        }
        //private async Task<Tuple<List<Fixture>, List<Market>, List<Selection>, BettingStatus>> GetBetFixtures(BetFixtureRequestDTO request)
        //{
        //    var fixtureIds = request.Selections.Select(s => s.FixtureId).ToList();
        //    var fixtureFilter = Builders<Fixture>.Filter.Where(c => fixtureIds.Contains(c.FiId));
        //    var fixtures = primaryFixtureCollection.Find(fixtureFilter).ToListAsync();

        //    var marketIds = request.Selections.Select(s => s.MarketId).ToList();
        //    var marketFilter = Builders<Market>.Filter.Where(c => marketIds.Contains(c.MaId));
        //    var markets = primaryMarketCollection.Find(marketFilter).ToListAsync();

        //    var selectionIds = request.Selections.Select(s => s.SelectionId).ToList();
        //    var selectionFilter = Builders<Selection>.Filter.Where(c => selectionIds.Contains(c.SeId));
        //    var selections = primarySelectionCollection.Find(selectionFilter).ToListAsync();

        //    var allSelectionFilter = Builders<Selection>.Filter.Where(c => marketIds.Contains(c.MaId));
        //    var allSelections = primarySelectionCollection.Find(allSelectionFilter).ToListAsync();

        //    var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
        //    var bettingStatus = primaryBettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

        //    await Task.WhenAll(fixtures, markets, selections, allSelections, bettingStatus);

        //    var newFixtures = fixtures.Result;
        //    var newMarkets = markets.Result;
        //    var newSelections = selections.Result;
        //    var newAllSelections = allSelections.Result;

        //    if (IsOEEnable(request.ClientCode))
        //    {
        //        newFixtures = newFixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

        //        var finalMarkets = new List<Market>();
        //        var finalSelections = new List<Selection>();

        //        foreach (var fixture in newFixtures)
        //        {
        //            var initialMarkets = newMarkets.Where(c => c.FiId == fixture.FiId).ToList();

        //            foreach (var market in initialMarkets)
        //            {
        //                if (fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright)
        //                {
        //                    market.MaStId = market.MaStId;
        //                    market.MaStNa = market.MaStNa;
        //                }
        //                else
        //                {
        //                    market.MaStId = market.ClCo.Contains(request.ClientCode) ? market.MaStId : (int)MarketStatus.Inactive;
        //                    market.MaStNa = market.ClCo.Contains(request.ClientCode) ? market.MaStNa : MarketStatus.Inactive.ToString();
        //                }

        //                finalMarkets.Add(market);


        //                var clientOdds = new List<ClientOdds>();

        //                var marketSelections = newAllSelections.Where(c => c.MaId == market.MaId).ToList();

        //                if (marketSelections.Count > 0)
        //                {
        //                    var activeSelections = marketSelections.Where(c => c.MaId == market.MaId && c.IsAc == true && c.Od > 0m).ToList();
        //                    var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

        //                    clientOdds = GetClientOdds(activeSelections.Count, margin, marketSelections, market.ClCoMa, request.ClientCode);
        //                }

        //                foreach (var selection in marketSelections)
        //                {
        //                    if (fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright)
        //                    {
        //                        finalSelections.Add(selection);
        //                    }
        //                    else
        //                    {
        //                        selection.Od = clientOdds.Where(c => c.SelectionId == selection.SeId).Select(ss => ss.Odds).FirstOrDefault();

        //                        finalSelections.Add(selection);
        //                    }
        //                }
        //            }
        //        }

        //        if (finalMarkets.Count > 0)
        //            newMarkets = finalMarkets;

        //        if (finalSelections.Count > 0)
        //            newSelections = finalSelections;
        //    }

        //    return new Tuple<List<Fixture>, List<Market>, List<Selection>, BettingStatus>(newFixtures, newMarkets, newSelections, bettingStatus.Result);
        //}
        private async Task<Tuple<List<Fixture>, List<Market>, List<SelectionExtension>, BettingStatus, List<Selection>>> GetBetFixtures2(BetFixtureRequestDTO request)
        {
            var selectionIds = request.Selections.Select(s => s.SelectionId).ToList();
            var selectionFilter = Builders<Selection>.Filter.Where(c => selectionIds.Contains(c.SeId));
            var selections = await primarySelectionCollection.Find(selectionFilter).ToListAsync();

            var newFixtures = new List<Fixture>();
            var newMarkets = new List<Market>();
            var newSelectionExtension = new List<SelectionExtension>();
            var newAllSelections = new List<Selection>();
            var newBettingStatus = new BettingStatus();

            if (selections.Count > 0)
            {
                var fixtureIds = selections.Select(s => s.FiId).Distinct().ToList();
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => fixtureIds.Contains(c.FiId));
                var fixtures = primaryFixtureCollection.Find(fixtureFilter).ToListAsync();

                var marketIds = selections.Select(s => s.MaId).Distinct().ToList();
                var marketFilter = Builders<Market>.Filter.Where(c => marketIds.Contains(c.MaId));
                var markets = primaryMarketCollection.Find(marketFilter).ToListAsync();

                var allSelectionFilter = Builders<Selection>.Filter.Where(c => marketIds.Contains(c.MaId));
                var allSelections = primarySelectionCollection.Find(allSelectionFilter).ToListAsync();

                var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
                var bettingStatus = primaryBettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

                await Task.WhenAll(fixtures, markets, allSelections, bettingStatus);

                newFixtures = fixtures.Result;
                newMarkets = markets.Result;
                var isBinary = selections.Count > 2 ? false : true;

                newSelectionExtension = selections.Select(s => new SelectionExtension()
                {
                    SeId = s.SeId,
                    SpId = s.SpId,
                    SegId = s.SegId,
                    LeId = s.LeId,
                    FiId = s.FiId,
                    MaId = s.MaId,
                    MaTeId = s.MaTeId,
                    SeTeId = s.SeTeId,
                    SeNa = s.SeNa,
                    Od = s.Od,
                    OddsRef = s.Od,
                    HoOd = GetHongkongOdds(s.Od, isBinary),
                    MaOd = GetMalayOdds(s.Od, isBinary),
                    InOd = GetIndoOdds(s.Od, isBinary),
                    IsAc = s.IsAc,
                    SeSoOr = s.SeSoOr,
                    SeStId = s.SeStId,
                    SeStNa = s.SeStNa,
                    SeSp = s.SeSp,
                    FiStDa = s.FiStDa,
                    FiStId = s.FiStId,
                    MaStId = s.MaStId,
                    CoId = s.CoId,
                    Pr = s.Pr,
                    WoPr = s.WoPr,
                    LoPr = s.LoPr,
                    VoPr = s.VoPr,
                    HaWoPr = s.HaWoPr,
                    HaLoPr = s.HaLoPr,
                    DeHeFa = s.DeHeFa,
                    ClBoOd = s.ClBoOd,
                    SeNaTr = s.SeNaTr,
                    ClBoSeNa = s.ClBoSeNa,
                    ClBoSeNaTr = s.ClBoSeNaTr,
                }).ToList();

                newAllSelections = allSelections.Result;
                newBettingStatus = bettingStatus.Result;
            }

            if (IsOEEnable(request.ClientCode))
            {
                newFixtures = newFixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

                var finalMarkets = new List<Market>();
                var finalSelections = new List<SelectionExtension>();

                foreach (var fixture in newFixtures)
                {
                    var initialMarkets = newMarkets.Where(c => c.FiId == fixture.FiId).ToList();

                    foreach (var market in initialMarkets)
                    {
                        if (fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright)
                        {
                            market.MaStId = market.MaStId;
                            market.MaStNa = market.MaStNa;
                        }
                        else
                        {
                            market.MaStId = market.ClCo.Contains(request.ClientCode) ? market.MaStId : (int)MarketStatus.Inactive;
                            market.MaStNa = market.ClCo.Contains(request.ClientCode) ? market.MaStNa : MarketStatus.Inactive.ToString();
                        }

                        finalMarkets.Add(market);

                        var clientOdds = new List<ClientOdds>();

                        var marketSelections = newAllSelections.Where(c => c.MaId == market.MaId).ToList();
                        var isBinary = marketSelections.Count > 2 ? false : true;

                        if (marketSelections.Count > 0)
                        {
                            var activeSelections = marketSelections.Where(c => c.MaId == market.MaId && c.IsAc == true && c.Od > 0m).ToList();
                            var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

                            clientOdds = GetClientOdds(activeSelections.Count, margin, marketSelections, market.ClCoMa, request.ClientCode);
                        }

                        foreach (var selection in marketSelections)
                        {
                            SelectionExtension tempSelectionExtension = new SelectionExtension()
                            {
                                SeId = selection.SeId,
                                SpId = selection.SpId,
                                SegId = selection.SegId,
                                LeId = selection.LeId,
                                FiId = selection.FiId,
                                MaId = selection.MaId,
                                MaTeId = selection.MaTeId,
                                SeTeId = selection.SeTeId,
                                SeNa = selection.SeNa,
                                Od = selection.Od,
                                OddsRef = selection.Od,
                                IsAc = selection.IsAc,
                                SeSoOr = selection.SeSoOr,
                                SeStId = selection.SeStId,
                                SeStNa = selection.SeStNa,
                                SeSp = selection.SeSp,
                                FiStDa = selection.FiStDa,
                                FiStId = selection.FiStId,
                                MaStId = selection.MaStId,
                                CoId = selection.CoId,
                                Pr = selection.Pr,
                                WoPr = selection.WoPr,
                                LoPr = selection.LoPr,
                                VoPr = selection.VoPr,
                                HaWoPr = selection.HaWoPr,
                                HaLoPr = selection.HaLoPr,
                                DeHeFa = selection.DeHeFa,
                                ClBoOd = selection.ClBoOd,
                                SeNaTr = selection.SeNaTr,
                                ClBoSeNa = selection.ClBoSeNa,
                                ClBoSeNaTr = selection.ClBoSeNaTr,
                            };

                            if (fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright)
                            {
                                tempSelectionExtension.Od = Convert.ToDecimal(GetClientBoostedOdds(selection.ClBoOd, request.ClientCode, request.IsBoosted, selection.Od.ToString("0.00")));
                                tempSelectionExtension.OddsRef = Convert.ToDecimal(selection.Od.ToString("0.00"));
                                tempSelectionExtension.MaOd = GetMalayOdds(selection.Od, isBinary);
                                tempSelectionExtension.HoOd = GetHongkongOdds(selection.Od, isBinary);
                                tempSelectionExtension.InOd = GetIndoOdds(selection.Od, isBinary);
                            }
                            else
                            {
                                var clientOdd = clientOdds.FirstOrDefault(c => c.SelectionId == selection.SeId);

                                tempSelectionExtension.Od = Convert.ToDecimal(GetClientBoostedOdds(selection.ClBoOd, request.ClientCode, request.IsBoosted, clientOdd.DecimalOdds.ToString("0.00")));
                                tempSelectionExtension.OddsRef = Convert.ToDecimal(clientOdd.DecimalOdds.ToString("0.00"));
                                tempSelectionExtension.MaOd = clientOdd.MalaysianOdds;
                                tempSelectionExtension.HoOd = clientOdd.HongkongOdds;
                                tempSelectionExtension.InOd = clientOdd.IndonesianOdds;
                            }

                            finalSelections.Add(tempSelectionExtension);

                        }
                    }
                }

                if (finalMarkets.Count > 0)
                    newMarkets = finalMarkets;

                if (finalSelections.Count > 0)
                    newSelectionExtension = finalSelections;
            }

            return new Tuple<List<Fixture>, List<Market>, List<SelectionExtension>, BettingStatus, List<Selection>>(newFixtures, newMarkets, newSelectionExtension, newBettingStatus, newAllSelections);
        }

        public async Task<BaseResponseDTO<BetSelectionProbabilityResponseDTO>> GetBetSelectionProbabilities(BetSelectionProbabilityRequestDTO request)
        {
            var fixtureIds = request.Selections.Select(s => s.Item1).Distinct().ToList();
            var marketIds = request.Selections.Select(s => s.Item2).Distinct().ToList();
            var selectionIds = request.Selections.Select(s => s.Item3).Distinct().ToList();

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => fixtureIds.Contains(c.FiId));
            var fixtureProjection = Builders<Fixture>.Projection.Include(c => c.FiId).Include(c => c.IsSu).Include(c => c.FiStId);
            var fixtures = primaryFixtureCollection.Find(fixtureFilter).Project<FixtureProbabilityFilter>(fixtureProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => marketIds.Contains(c.MaId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.MaId).Include(c => c.MaStId);
            var markets = primaryMarketCollection.Find(marketFilter).Project<MarketProbabilityFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => selectionIds.Contains(c.SeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.SeId).Include(c => c.FiId).Include(c => c.MaId).Include(c => c.MaStId).Include(c => c.IsAc).Include(c => c.Pr).Include(c => c.WoPr).Include(c => c.LoPr).Include(c => c.VoPr).Include(c => c.HaWoPr).Include(c => c.HaLoPr).Include(c => c.SeStId);
            var selections = primarySelectionCollection.Find(selectionFilter).Project<SelectionProbabilityFilter>(selectionProjection).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var bettingStatus = primaryBettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(fixtures, markets, selections, bettingStatus);

            var newFixtures = fixtures.Result;
            var newMarkets = markets.Result;
            var newSelections = selections.Result;
            var newBettingStatus = bettingStatus.Result;

            if (newSelections.Count == 0)
            {
                return new BaseResponseDTO<BetSelectionProbabilityResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetSelectionProbabilityResponseDTO()
                };
            }

            return new BaseResponseDTO<BetSelectionProbabilityResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetSelectionProbabilityResponseDTO
                {
                    Selections = newSelections.Select(s => new BetSelectionProbabilityDTO
                    {
                        SelectionId = s.SeId,
                        FixtureStatusId = newFixtures.Where(c => c.FiId == s.FiId).Select(ss => ss.FiStId).FirstOrDefault(),
                        MarketStatusId = newMarkets.Where(c => c.MaId == s.MaId).Select(ss => ss.MaStId).FirstOrDefault(),
                        IsGlobalBetStop = newBettingStatus.IsBeSo,
                        IsSuspended = newFixtures.Where(c => c.FiId == s.FiId).Select(ss => ss.IsSu).FirstOrDefault(),
                        IsActive = s.IsAc,
                        Probability = s.Pr,
                        WonProbability = s.WoPr,
                        LostProbability = s.LoPr,
                        VoidProbability = s.VoPr,
                        HalfWonProbability = s.HaWoPr,
                        HalfLostProbability = s.HaLoPr,
                        SelectionStatusId = s.SeStId,
                    }).ToList()
                }
            };
        }

        private async Task<Tuple<List<Fixture>, List<Market>, List<Selection>>> GetBetFixtureTranslations(BetFixtureTranslationRequestDTO request)
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

            await Task.WhenAll(fixtures, markets, selections);

            return new Tuple<List<Fixture>, List<Market>, List<Selection>>(fixtures.Result, markets.Result, selections.Result);
        }
        public async Task<BaseResponseDTO<BetFixtureTranslationResponseDTO>> GetBetFixtureTranslation(BetFixtureTranslationRequestDTO request)
        {
            var result = await GetBetFixtureTranslations(request);

            return new BaseResponseDTO<BetFixtureTranslationResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetFixtureTranslationResponseDTO
                {
                    Selections = request.Selections.Select(s => new BetFixtureSelectionTranslationResponseDTO
                    {
                        SportId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SpId).FirstOrDefault(),
                        SportName = GetSportName(result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SpNa).FirstOrDefault(), result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SpNaTr).FirstOrDefault(), request.LanguageCode),
                        SegmentId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SegId).FirstOrDefault(),
                        SegmentName = GetSegmentName(result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SegNa).FirstOrDefault(), result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.SegNaTr).FirstOrDefault(), request.LanguageCode),
                        LeagueId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.LeId).FirstOrDefault(),
                        LeagueName = GetLeagueName(result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.LeNa).FirstOrDefault(), result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.LeNaTr).FirstOrDefault(), request.LanguageCode),
                        FixtureId = result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.FiId).FirstOrDefault(),
                        FixtureName = GetFixtureName(result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.FiNa).FirstOrDefault(), result.Item1.Where(c => c.FiId == s.FixtureId).Select(ss => ss.FiNaTr).FirstOrDefault(), request.LanguageCode),
                        MarketId = result.Item2.Where(c => c.MaId == s.MarketId).Select(ss => ss.MaId).FirstOrDefault(),
                        MarketName = GetMarketName(result.Item2.Where(c => c.FiId == s.FixtureId && c.MaId == s.MarketId).Select(ss => ss.MaNa).FirstOrDefault(), result.Item2.Where(c => c.FiId == s.FixtureId && c.MaId == s.MarketId).Select(ss => ss.MaNaTr).FirstOrDefault(), request.LanguageCode),
                        SelectionId = result.Item3.Where(c => c.SeId == s.SelectionId).Select(ss => ss.SeId).FirstOrDefault(),
                        SelectionName = GetSelectionName(result.Item3.Where(c => c.FiId == s.FixtureId && c.MaId == s.MarketId && c.SeId == s.SelectionId).Select(ss => ss.SeNa).FirstOrDefault(), result.Item3.Where(c => c.FiId == s.FixtureId && c.MaId == s.MarketId && c.SeId == s.SelectionId).Select(ss => ss.SeNaTr).FirstOrDefault(), request.LanguageCode)
                    }).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request)
        {
            var result = await GetBetFixtures2(request);

            var raw = new List<BetFixtureSelectionResponseDTO>();

            var betBuilderOddsStatus = new Tuple<decimal, StatusCode>(0m, StatusCode.Success);
            if (request.IsBetBuilder)
                betBuilderOddsStatus = await GetBetBuilderOddsStatus(request.OddsChangeType, request.Selections, result, request.TotalOdds);

            foreach (var selection in request.Selections)
            {
                var initialSelectionExtension = result.Item3.Where(c => c.SeId == selection.SelectionId).FirstOrDefault();
                var initialFixture = result.Item1.Where(c => c.FiId == initialSelectionExtension.FiId).FirstOrDefault();
                var initialMarket = result.Item2.Where(c => c.MaId == initialSelectionExtension.MaId).FirstOrDefault();
                var selectionCount = result.Item5.Where(c => c.MaId == initialMarket.MaId).Count();

                raw.Add(new BetFixtureSelectionResponseDTO
                {
                    SportId = initialFixture.SpId,
                    SportName = initialFixture.SpNa,
                    SegmentId = initialFixture.SegId,
                    SegmentName = initialFixture.SegNa,
                    LeagueId = initialFixture.LeId,
                    LeagueName = initialFixture.LeNa,
                    FixtureId = initialFixture.FiId,
                    FixtureName = initialFixture.FiNa,
                    FixtureType = (FixtureType)initialFixture.FiTyId,
                    MarketTemplateId = initialMarket.MaTeId,
                    MarketId = initialMarket.MaId,
                    MarketName = initialMarket.MaNa,
                    Line = initialMarket.Li,
                    SelectionTemplateId = initialSelectionExtension.SeTeId,
                    SelectionId = initialSelectionExtension.SeId,
                    SelectionName = initialSelectionExtension.SeNa,
                    DecimalOdds = initialSelectionExtension.Od,
                    MalaysianOdds = initialSelectionExtension.MaOd,
                    HongkongOdds = initialSelectionExtension.HoOd,
                    IndonesianOdds = initialSelectionExtension.InOd,
                    OddsRef = initialSelectionExtension.OddsRef,
                    FixtureStatus = (FixtureStatus)initialSelectionExtension.FiStId,
                    FixtureStartDate = initialSelectionExtension.FiStDa,
                    Score = initialFixture.Sc,
                    FixtureSpecifier = initialFixture.FiSp,
                    SelectionSpecifier = initialSelectionExtension.SeSp,
                    CorrelationId = initialSelectionExtension.CoId,
                    Probability = initialSelectionExtension.Pr,
                    WonProbability = initialSelectionExtension.WoPr,
                    LostProbability = initialSelectionExtension.LoPr,
                    VoidProbability = initialSelectionExtension.VoPr,
                    HalfWonProbability = initialSelectionExtension.HaWoPr,
                    HalfLostProbability = initialSelectionExtension.HaLoPr,
                    SelectionCount = selectionCount,
                    BetSelectionState = GetBetSelectionStatus(request.OddsChangeType, selection, result, request.ClientCode, request.IsBetBuilder, selectionCount)
                });
            }

            return new BaseResponseDTO<BetFixtureResponseDTO>
            {
                StatusCode = request.IsBetBuilder ? betBuilderOddsStatus.Item2 : StatusCode.Success,
                Message = request.IsBetBuilder ? betBuilderOddsStatus.Item2.ToString() : StatusCode.Success.ToString(),
                Result = new BetFixtureResponseDTO
                {
                    TotalOdds = betBuilderOddsStatus.Item1,
                    Selections = raw
                }
            };
        }

        public async Task<BaseResponseDTO<TWResponseDTO>> GetUserFavoritesAll(UserFavoriteRequestDTO request)
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

            var userFavorites = new List<UserFavoriteDTO>();

            var leagues = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.League).Select(s => Convert.ToInt32(s.Id)).Distinct().ToList();

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            if (leagues.Count > 0)
            {
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && leagues.Contains(c.LeId));
                var tempLeagueResult = fixtureCollection.Find(leagueFilter).ToListAsync();

                var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
                var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

                await Task.WhenAll(tempLeagueResult, tempBettingStatus);

                var leagueResult = tempLeagueResult.Result;
                if (IsOEEnable(request.ClientCode))
                    leagueResult = leagueResult.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

                var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

                if (leagueResult.Count > 0)
                {
                    var fixtureIds = leagueResult.Select(s => Convert.ToInt64(s.FiId)).ToList();
                    var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

                    var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
                    var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

                    var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
                    var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

                    var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
                    var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

                    await Task.WhenAll(marketCount, markets, selections);

                    foreach (var item in leagueResult)
                    {
                        if (!userFavorites.Any(c => c.SportId == item.SpId && c.Id == item.LeId.ToString() && c.HierarchyId == (int)UserFavoriteHierarchy.League))
                        {
                            userFavorites.Add(new UserFavoriteDTO
                            {
                                SportId = item.SpId,
                                SportName = item.SpNa,
                                HierarchyId = (int)UserFavoriteHierarchy.League,
                                HierarchyName = UserFavoriteHierarchy.League.ToString(),
                                Id = item.LeId.ToString(),
                                Name = item.LeNa
                            });
                        }

                        if (result.Any(c => c.FixtureId == item.FiId.ToString())) continue;

                        var filteredBetBuilderMarketTemplates = new List<BetBuilderMarketTemplates>();
                        var isBetBuilder = false;

                        if (betBuilderSportIds.Contains(item.SpId) && item.FiTyId == (int)FixtureType.Match)
                        {

                            if(item.FiStId == (int)FixtureStatus.Prematch)
                                filteredBetBuilderMarketTemplates = betBuilderMarketTemplates.Where(c => c.SportId == item.SpId && c.Prematch == true).ToList();
                            else if (item.FiStId == (int)FixtureStatus.Live)
                                filteredBetBuilderMarketTemplates = betBuilderMarketTemplates.Where(c => c.SportId == item.SpId && c.Live == true).ToList();

                            isBetBuilder = filteredBetBuilderMarketTemplates.Any() ? true : false;

                        }

                        result.Add(new FixtureDTO
                        {
                            SportId = item.SpId,
                            SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                            SegmentId = item.SegId,
                            SegmentName = GetSegmentName(item.SegNa, item.SegNaTr, request.LanguageCode),
                            LeagueId = item.LeId,
                            LeagueName = GetLeagueName(item.LeNa, item.LeNaTr, request.LanguageCode),
                            ProviderFixtureId = item.PrFiId,
                            fixtureTypeId = item.FiTyId,
                            FixtureId = item.FiId.ToString(),
                            FixtureName = GetFixtureName(item.FiNa, item.FiNaTr, request.LanguageCode),
                            HomeName = GetHomeName(item.FiNa, item.FiNaTr, request.LanguageCode, (FixtureType)item.FiTyId),
                            AwayName = GetAwayName(item.FiNa, item.FiNaTr, request.LanguageCode, (FixtureType)item.FiTyId),
                            FixtureStartDate = item.FiStDa,
                            IsLive = item.IsOfLi,
                            IsLiveStream = IsLiveStream(item.LiId, item.GeAl, request.IpCountryCode),
                            FixtureStatusId = item.SpId > 1000 || item.FiTyId == (int)FixtureType.Outright ? item.FiStId : !IsOEEnable(request.ClientCode) || item.ClCo.Contains(request.ClientCode) ? item.FiStId : (int)FixtureStatus.Closed,
                            FixtureStatusName = item.SpId > 1000 || item.FiTyId == (int)FixtureType.Outright ? item.FiStNa : !IsOEEnable(request.ClientCode) || item.ClCo.Contains(request.ClientCode) ? item.FiStNa : FixtureStatus.Closed.ToString(),
                            MarketCount = marketCount.Result.Where(c => c.FiId == item.FiId).Count(),
                            SMSCode = string.Empty,
                            StatisticsURL = GetStatisticsURL(request.ClientId, item.StUr, request.LanguageCode),
                            Score = item.SpId == 138 ? string.Empty : item.Sc,
                            CurrentPeriod = CurrentPeriodHelper.ToShortName(item.CuPe),
                            CurrentMinute = item.CuMi.ToString(),
                            BetStop = item.IsSu ? new List<int> { 1 } : new List<int> { },
                            IsBetStop = item.IsSu,
                            IsBetStopGlobal = bettingStatus.IsBeSo,
                            IsBetBuilder = isBetBuilder,
                            Markets = GetMainMarkets(item.SpId, item.FiTyId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList(), request.LanguageCode, request.ClientCode, item.FiStId, filteredBetBuilderMarketTemplates)
                        });
                    }
                }
            }

            var fixtures = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtures.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && fixtures.Contains(c.FiId));
                var tempFixtureResult = fixtureCollection.Find(fixtureFilter).ToListAsync();

                var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
                var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

                await Task.WhenAll(tempFixtureResult, tempBettingStatus);

                var fixtureResult = tempFixtureResult.Result;
                if (IsOEEnable(request.ClientCode))
                    fixtureResult = fixtureResult.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

                var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

                if (fixtureResult.Count > 0)
                {
                    var fixtureIds = fixtureResult.Select(s => Convert.ToInt64(s.FiId)).ToList();
                    var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

                    var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
                    var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

                    var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
                    var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

                    var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
                    var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
                    var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

                    await Task.WhenAll(marketCount, markets, selections);

                    foreach (var item in fixtureResult)
                    {
                        if (!userFavorites.Any(c => c.SportId == item.SpId && c.Id == item.FiId.ToString() && c.HierarchyId == (int)UserFavoriteHierarchy.Fixture))
                        {
                            userFavorites.Add(new UserFavoriteDTO
                            {
                                SportId = item.SpId,
                                SportName = item.SpNa,
                                HierarchyId = (int)UserFavoriteHierarchy.Fixture,
                                HierarchyName = UserFavoriteHierarchy.Fixture.ToString(),
                                Id = item.FiId.ToString(),
                                Name = item.FiNa
                            });
                        }

                        if (result.Any(c => c.FixtureId == item.FiId.ToString())) continue;

                        var filteredBetBuilderMarketTemplates = new List<BetBuilderMarketTemplates>();
                        var isBetBuilder = false;

                        if (betBuilderSportIds.Contains(item.SpId) && item.FiTyId == (int)FixtureType.Match)
                        {
                            filteredBetBuilderMarketTemplates = betBuilderMarketTemplates.Where(c => c.SportId == item.SpId).ToList();
                            isBetBuilder = true;
                        }

                        result.Add(new FixtureDTO
                        {
                            SportId = item.SpId,
                            SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                            SegmentId = item.SegId,
                            SegmentName = GetSegmentName(item.SegNa, item.SegNaTr, request.LanguageCode),
                            LeagueId = item.LeId,
                            LeagueName = GetLeagueName(item.LeNa, item.LeNaTr, request.LanguageCode),
                            ProviderFixtureId = item.PrFiId,
                            fixtureTypeId = item.FiTyId,
                            FixtureId = item.FiId.ToString(),
                            FixtureName = GetFixtureName(item.FiNa, item.FiNaTr, request.LanguageCode),
                            HomeName = GetHomeName(item.FiNa, item.FiNaTr, request.LanguageCode, (FixtureType)item.FiTyId),
                            AwayName = GetAwayName(item.FiNa, item.FiNaTr, request.LanguageCode, (FixtureType)item.FiTyId),
                            FixtureStartDate = item.FiStDa,
                            IsLive = item.IsOfLi,
                            IsLiveStream = IsLiveStream(item.LiId, item.GeAl, request.IpCountryCode),
                            FixtureStatusId = item.SpId > 1000 || item.FiTyId == (int)FixtureType.Outright ? item.FiStId : !IsOEEnable(request.ClientCode) || item.ClCo.Contains(request.ClientCode) ? item.FiStId : (int)FixtureStatus.Closed,
                            FixtureStatusName = item.SpId > 1000 || item.FiTyId == (int)FixtureType.Outright ? item.FiStNa : !IsOEEnable(request.ClientCode) || item.ClCo.Contains(request.ClientCode) ? item.FiStNa : FixtureStatus.Closed.ToString(),
                            MarketCount = marketCount.Result.Where(c => c.FiId == item.FiId).Count(),
                            SMSCode = string.Empty,
                            StatisticsURL = GetStatisticsURL(request.ClientId, item.StUr, request.LanguageCode),
                            Score = item.SpId == 138 ? string.Empty : item.Sc,
                            CurrentPeriod = CurrentPeriodHelper.ToShortName(item.CuPe),
                            CurrentMinute = item.CuMi.ToString(),
                            BetStop = item.IsSu ? new List<int> { 1 } : new List<int> { },
                            IsBetStop = item.IsSu,
                            IsBetStopGlobal = bettingStatus.IsBeSo,
                            IsBetBuilder = isBetBuilder,
                            Markets = GetMainMarkets(item.SpId, item.FiTyId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FiId)).ToList(), request.LanguageCode, request.ClientCode, item.FiStId, filteredBetBuilderMarketTemplates)
                        });
                    }
                }
            }

            await _rabbitMQRepository.PublishToRabbitMQAPI(new MessageRequestDTO
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageType = "OnVisitFavorites",
                RoutingKey = $"r.oe.sb.engine.onvisitfavorites.{request.ClientCode.ToLower()}",
                Payload = JsonSerializer.Serialize(new
                {
                    UserId = request.UserId,
                    Favorites = userFavorites
                })
            });

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
            var mKey = $"nuf:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.UserId.ToUpper()}";

            var mCache = _memCache.GetCache<BaseResponseDTO<UserFavoriteResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

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
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && leagueIds.Contains(c.LeId));
                var leagueResult = await fixtureCollection.Find(leagueFilter).ToListAsync();

                foreach (var item in leagueResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.LeId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                        HierarchyId = (int)UserFavoriteHierarchy.League,
                        HierarchyName = UserFavoriteHierarchy.League.ToString(),
                        Id = item.LeId.ToString(),
                        Name = GetLeagueName(item.LeNa, item.LeNaTr, request.LanguageCode)
                    });
                }
            }

            var fixtureIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtureIds.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && fixtureIds.Contains(c.FiId));
                var fixtureResult = await fixtureCollection.Find(fixtureFilter).ToListAsync();
                if (IsOEEnable(request.ClientCode))
                    fixtureResult = fixtureResult.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

                foreach (var item in fixtureResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.FiId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                        HierarchyId = (int)UserFavoriteHierarchy.Fixture,
                        HierarchyName = UserFavoriteHierarchy.Fixture.ToString(),
                        Id = item.FiId.ToString(),
                        Name = GetFixtureName(item.FiNa, item.FiNaTr, request.LanguageCode)
                    });
                }
            }

            var returnResult = new BaseResponseDTO<UserFavoriteResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new UserFavoriteResponseDTO
                {
                    Favorites = result.OrderBy(o => o.Name).ToList()
                }
            };

            if (result.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddMinutes(1));

            return returnResult;
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddFavorites(UserFavoriteInsertDeleteRequestDTO request)
        {
            var result = new List<UserFavoriteDTO>();

            if (request.UserFavoriteHierarchy == UserFavoriteHierarchy.League)
            {
                var leagueIdDataTypeTest = int.TryParse(request.Id, out int tempLeagueId);

                if (leagueIdDataTypeTest == false)
                {
                    return new BaseResponseDTO<UserFavoriteResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "LeagueId is invalid",
                        Result = new UserFavoriteResponseDTO()
                    };
                }
            }

            if (request.UserFavoriteHierarchy == UserFavoriteHierarchy.Fixture)
            {
                var fixtureIdDataTypeTest = int.TryParse(request.Id, out int tempFixtureId);

                if (fixtureIdDataTypeTest == true)
                {
                    return new BaseResponseDTO<UserFavoriteResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = "FixtureId is invalid",
                        Result = new UserFavoriteResponseDTO()
                    };
                }
            }

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
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && leagueIds.Contains(c.LeId));
                var leagueResult = await fixtureCollection.Find(leagueFilter).ToListAsync();

                foreach (var item in leagueResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.LeId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                        HierarchyId = (int)UserFavoriteHierarchy.League,
                        HierarchyName = UserFavoriteHierarchy.League.ToString(),
                        Id = item.LeId.ToString(),
                        Name = GetLeagueName(item.LeNa, item.LeNaTr, request.LanguageCode)
                    });
                }
            }

            var fixtureIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtureIds.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && fixtureIds.Contains(c.FiId));
                var fixtureResult = await fixtureCollection.Find(fixtureFilter).ToListAsync();
                if (IsOEEnable(request.ClientCode))
                    fixtureResult = fixtureResult.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

                foreach (var item in fixtureResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.FiId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                        HierarchyId = (int)UserFavoriteHierarchy.Fixture,
                        HierarchyName = UserFavoriteHierarchy.Fixture.ToString(),
                        Id = item.FiId.ToString(),
                        Name = GetFixtureName(item.FiNa, item.FiNaTr, request.LanguageCode)
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
                var leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && leagueIds.Contains(c.LeId));
                var leagueResult = await fixtureCollection.Find(leagueFilter).ToListAsync();
                if (IsOEEnable(request.ClientCode))
                    leagueResult = leagueResult.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
                foreach (var item in leagueResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.LeId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                        HierarchyId = (int)UserFavoriteHierarchy.League,
                        HierarchyName = UserFavoriteHierarchy.League.ToString(),
                        Id = item.LeId.ToString(),
                        Name = GetLeagueName(item.LeNa, item.LeNaTr, request.LanguageCode)
                    });
                }
            }

            var fixtureIds = favorites.Where(c => c.HierarchyId == (int)UserFavoriteHierarchy.Fixture).Select(s => Convert.ToInt64(s.Id)).Distinct().ToList();

            if (fixtureIds.Count > 0)
            {
                var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && fixtureIds.Contains(c.FiId));
                var fixtureResult = await fixtureCollection.Find(fixtureFilter).ToListAsync();
                if (IsOEEnable(request.ClientCode))
                    fixtureResult = fixtureResult.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

                foreach (var item in fixtureResult)
                {
                    if (result.Any(c => c.HierarchyId == (int)UserFavoriteHierarchy.League && c.SportId == item.SpId && c.Id == item.FiId.ToString())) continue;

                    result.Add(new UserFavoriteDTO
                    {
                        SportId = item.SpId,
                        SportName = GetSportName(item.SpNa, item.SpNaTr, request.LanguageCode),
                        HierarchyId = (int)UserFavoriteHierarchy.Fixture,
                        HierarchyName = UserFavoriteHierarchy.Fixture.ToString(),
                        Id = item.FiId.ToString(),
                        Name = GetFixtureName(item.FiNa, item.FiNaTr, request.LanguageCode)
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
            var mKey = $"nctf:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var sKey = $"sctf:{request.ClientId.ToString().ToUpper()}";

            var cache = _memCache.GetCache<List<ClientTopFixture>>(sKey);

            var clientFixtures = new List<ClientTopFixture>();

            if (cache != null)
                clientFixtures = cache;
            else
            {
                clientFixtures = await _sportsbookContext.ClientTopFixture.Where(c => c.ClientId == request.ClientId).ToListAsync();
                _memCache.AddOrUpdateCache(sKey, clientFixtures, DateTimeOffset.UtcNow.AddMinutes(5));
            }

            if (clientFixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var tempFixtureIds = clientFixtures.Select(s => s.FixtureId).ToList();

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => tempFixtureIds.Contains(c.FiId));
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result.Where(c => (c.FiStId == (int)FixtureStatus.Prematch || c.FiStId == (int)FixtureStatus.Live) && c.FiTyId == (int)FixtureType.Match).ToList();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureTypeId = s.FiTyId,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates), 
                SortOrder = clientFixtures.Where(c => c.FixtureId == s.FiId).Select(s => s.SortOrder).FirstOrDefault()
            }).OrderBy(o => o.SortOrder).ToList();

            var initialResult = new List<TempFixtureDTO>();

            var limit = 10;

            foreach (var item in result)
            {
                if (initialResult.Count == limit) break;

                initialResult.Add(item);
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();
            var marketTemplateIds = new List<string>();

            foreach (var item in initialResult)
            {
                var templates = GetMainMarketTemplates(item.SportId);

                foreach (var template in templates)
                {
                    if (marketTemplateIds.Any(c => c == template)) continue;

                    marketTemplateIds.Add(template);
                }
            }

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            foreach (var item in initialResult)
            {
                var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(item.SportId) && item.FixtureTypeId == (int)FixtureType.Match ? betBuilderMarketTemplates.Where(c => c.SportId == item.SportId).ToList() : new List<BetBuilderMarketTemplates>();

                var currentMarkets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates);

                //if (currentMarkets.Count == 0) continue;

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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    Markets = currentMarkets
                });

                if (finalResult.Count == 10) break;
            }

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(TWFLiveFixtureCountRequestDTO request)
        {
            var mKey = $"nlfc:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportGroupId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWFixtureCountResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            FilterDefinition<Fixture> fixtureFilter;
            if (request.SportGroupId != 0)
            {
                var sportsId = (await GetSportGroups()).Result.SportGroups.FirstOrDefault(c => c.SportGroupId == request.SportGroupId).Sports.Split(",").Select(s => Convert.ToInt32(s)).ToList();
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Live && c.FiTyId == (int)FixtureType.Match && sportsId.Contains(c.SpId));
            }
            else
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Live && c.FiTyId == (int)FixtureType.Match);

            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWFixtureCountResponseDTO()
                };
            }

            var sortOrder = GetSportOrder().Where(c => c.ClientId == request.ClientId && c.IsActive == true).ToList();

            var result = new List<FixtureCount>();

            foreach (var item in sortOrder.OrderBy(o => o.SortOrder).ToList())
            {
                var initialList = fixtures.Where(c => c.SpId == item.SportId).ToList();
                var count = initialList.Count();
                var sport = initialList.FirstOrDefault();

                if (count == 0) continue;

                result.Add(new FixtureCount
                {
                    SportId = item.SportId,
                    SportName = GetSportName(sport.SpNa, sport.SpNaTr, request.LanguageCode),
                    Count = count
                });
            }

            var returnResult = new BaseResponseDTO<TWFixtureCountResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWFixtureCountResponseDTO
                {
                    Sports = result
                }
            };

            if (result.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(TWFTodayFixtureCountRequestDTO request)
        {
            var mKey = $"ntfc:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.MinuteUTCOffset}:{request.SportGroupId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWFixtureCountResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var dateTimeRange = GetDateTimeRange(DateTime.UtcNow, request.MinuteUTCOffset);

            FilterDefinition<Fixture> fixtureFilter;
            if (request.SportGroupId != 0)
            {
                var sportsId = (await GetSportGroups()).Result.SportGroups.FirstOrDefault(c => c.SportGroupId == request.SportGroupId).Sports.Split(",").Select(s => Convert.ToInt32(s)).ToList();
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2 && sportsId.Contains(c.SpId));
            }
            else
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);

            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWFixtureCountResponseDTO()
                };
            }

            var sortOrder = GetSportOrder().Where(c => c.ClientId == request.ClientId && c.IsActive == true).ToList();

            var result = new List<FixtureCount>();

            foreach (var item in sortOrder.OrderBy(o => o.SortOrder).ToList())
            {
                var initialList = fixtures.Where(c => c.SpId == item.SportId).ToList();
                var count = initialList.Count();
                var sport = initialList.FirstOrDefault();

                if (count == 0) continue;

                if (initialList.Count > 300)
                    count = 300;

                result.Add(new FixtureCount
                {
                    SportId = item.SportId,
                    SportName = GetSportName(sport.SpNa, sport.SpNaTr, request.LanguageCode),
                    Count = count
                });
            }

            var returnResult = new BaseResponseDTO<TWFixtureCountResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWFixtureCountResponseDTO
                {
                    Sports = result
                }
            };

            if (result.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        private string GetStatisticsURL(Guid clientId, string uriPath, LanguageCode languageCode)
        {
            switch (clientId.ToString().ToUpper())
            {
                case "5093617A-9367-EB11-89FB-00224815B68B":
                case "BFED6DA4-D1B5-4437-665D-08D8EE99AC1C":
                    return string.IsNullOrEmpty(uriPath) ? string.Empty : $"https://st-cdn001.akamaized.net/song882{uriPath}".Replace("{languageCode}", languageCode.ToString().ToLower());

                case "A96AB8BC-2970-EB11-89FC-00224815B68B":
                case "917070EC-1DC1-4E10-28E8-08D8EE99CA8E":
                    return string.IsNullOrEmpty(uriPath) ? string.Empty : $"https://st-cdn001.akamaized.net/song883{uriPath}".Replace("{languageCode}", languageCode.ToString().ToLower());

                default:
                    return string.IsNullOrEmpty(uriPath) ? string.Empty : $"https://st-cdn001.akamaized.net/song884{uriPath}".Replace("{languageCode}", languageCode.ToString().ToLower());
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(TWLiveFixtureRequestDTO request)
        {
            var mKey = $"nlf:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Live && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();
            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureTypeId = s.FiTyId,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    SortOrder = item.SortOrder,
                    Markets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates)
                });
            });

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(TWLeagueLiveFixtureRequestDTO request)
        {
            var mKey = $"nlfbl:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}:{request.SegmentId}:{request.LeagueId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Live && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureTypeId = s.FiTyId,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    SortOrder = item.SortOrder,
                    Markets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates)
                });
            });

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(TWTodayFixtureRequestDTO request)
        {
            var mKey = $"ntf:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}:{request.MinuteUTCOffset}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var dateTimeRange = GetDateTimeRange(DateTime.UtcNow, request.MinuteUTCOffset);

            FilterDefinition<Fixture> fixtureFilter;

            if (request.SportId == 1055 || request.SportId == 1058)
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Outright && c.SpId == request.SportId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            else
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);

            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            List<TierLeagueOrder> sortOrder;
            List<TempFixtureDTO> result;

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            if (request.SportId == 1055 || request.SportId == 1058)
            {
                result = fixtures.Select(s => new TempFixtureDTO
                {
                    SportId = s.SpId,
                    SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                    SegmentId = s.SegId,
                    SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                    LeagueId = s.LeId,
                    LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = s.PrFiId,
                    FixtureId = s.FiId.ToString(),
                    FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    FixtureStartDate = s.FiStDa,
                    IsLive = s.IsOfLi,
                    IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                    FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                    FixtureTypeId = s.FiTyId,
                    MarketCount = 0,
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                    Score = s.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                    CurrentMinute = s.CuMi.ToString(),
                    BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = s.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    IsBetBuilder = false,
                }).OrderBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }
            else
            {
                sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

                result = fixtures.Select(s => new TempFixtureDTO
                {
                    SportId = s.SpId,
                    SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                    SegmentId = s.SegId,
                    SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                    LeagueId = s.LeId,
                    LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = s.PrFiId,
                    FixtureId = s.FiId.ToString(),
                    FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    FixtureStartDate = s.FiStDa,
                    IsLive = s.IsOfLi,
                    IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                    FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                    FixtureTypeId = s.FiTyId,
                    MarketCount = 0,
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                    Score = s.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                    CurrentMinute = s.CuMi.ToString(),
                    BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = s.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
                    SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }

            var initialResult = new List<TempFixtureDTO>();

            foreach (var item in result)
            {
                if (initialResult.Any(c => c.FixtureId == item.FixtureId)) continue;


                initialResult.Add(item);

                if (initialResult.Count == 100 && _africaClientCodes.Contains(request.ClientCode.ToUpper()))
                    break;

                if (initialResult.Count == 300) //HK Brands
                    break;
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    SortOrder = item.SortOrder,
                    Markets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates)
                });
            });

            if (request.SportId == 5)
                finalResult = finalResult.OrderBy(o => o.SegmentId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            else
                finalResult = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var final = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, final, DateTimeOffset.UtcNow.AddSeconds(5));

            return final;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(TWLeagueTodayFixtureRequestDTO request)
        {
            var mKey = $"ntfbl:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.MinuteUTCOffset}:{request.SportId}:{request.SegmentId}:{request.LeagueId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var dateTimeRange = GetDateTimeRange(DateTime.UtcNow, request.MinuteUTCOffset);

            FilterDefinition<Fixture> fixtureFilter;

            if (request.SportId == 1055 || request.SportId == 1058)
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Outright && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            else
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);

            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            List<TierLeagueOrder> sortOrder;
            List<TempFixtureDTO> result;

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            if (request.SportId == 1055 || request.SportId == 1058)
            {
                result = fixtures.Select(s => new TempFixtureDTO
                {
                    SportId = s.SpId,
                    SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                    SegmentId = s.SegId,
                    SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                    LeagueId = s.LeId,
                    LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = s.PrFiId,
                    FixtureId = s.FiId.ToString(),
                    FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    FixtureStartDate = s.FiStDa,
                    IsLive = s.IsOfLi,
                    IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                    FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                    FixtureTypeId = s.FiTyId,
                    MarketCount = 0,
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                    Score = s.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                    CurrentMinute = s.CuMi.ToString(),
                    BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = s.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    IsBetBuilder = false
                }).OrderBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }
            else
            {
                sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

                result = fixtures.Select(s => new TempFixtureDTO
                {
                    SportId = s.SpId,
                    SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                    SegmentId = s.SegId,
                    SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                    LeagueId = s.LeId,
                    LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = s.PrFiId,
                    FixtureId = s.FiId.ToString(),
                    FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    FixtureStartDate = s.FiStDa,
                    IsLive = s.IsOfLi,
                    IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                    FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                    FixtureTypeId = s.FiTyId,
                    MarketCount = 0,
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                    Score = s.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                    CurrentMinute = s.CuMi.ToString(),
                    BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = s.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
                    SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }

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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    SortOrder = item.SortOrder,
                    Markets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates)
                });
            });

            if (request.SportId == 5)
                finalResult = finalResult.OrderBy(o => o.SegmentId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            else
                finalResult = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var final = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, final, DateTimeOffset.UtcNow.AddSeconds(5));

            return final;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(TWUpcomingFixtureRequestDTO request)
        {
            var mKey = $"nuf:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.MinuteUTCOffset}:{request.SportId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var dateTimeRange = GetDateTimeRange(DateTime.UtcNow, request.MinuteUTCOffset);

            FilterDefinition<Fixture> fixtureFilter;

            if (request.SportId == 1055 || request.SportId == 1058)
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Outright && c.SpId == request.SportId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            else
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);

            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            List<TierLeagueOrder> sortOrder;
            List<TempFixtureDTO> result;

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            if (request.SportId == 1055 || request.SportId == 1058)
            {
                result = fixtures.Select(s => new TempFixtureDTO
                {
                    SportId = s.SpId,
                    SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                    SegmentId = s.SegId,
                    SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                    LeagueId = s.LeId,
                    LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = s.PrFiId,
                    FixtureId = s.FiId.ToString(),
                    FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    FixtureStartDate = s.FiStDa,
                    IsLive = s.IsOfLi,
                    IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                    FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                    FixtureTypeId = s.FiTyId,
                    MarketCount = 0,
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                    Score = s.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                    CurrentMinute = s.CuMi.ToString(),
                    BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = s.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    IsBetBuilder = false
                }).OrderBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }
            else
            {
                sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

                result = fixtures.Select(s => new TempFixtureDTO
                {
                    SportId = s.SpId,
                    SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                    SegmentId = s.SegId,
                    SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                    LeagueId = s.LeId,
                    LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = s.PrFiId,
                    FixtureId = s.FiId.ToString(),
                    FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                    FixtureStartDate = s.FiStDa,
                    IsLive = s.IsOfLi,
                    IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                    FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                    FixtureTypeId = s.FiTyId,
                    MarketCount = 0,
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                    Score = s.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                    CurrentMinute = s.CuMi.ToString(),
                    BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = s.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
                    SortOrder = sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            }

            var initialResult = new List<TempFixtureDTO>();

            foreach (var item in result)
            {
                if (initialResult.Any(c => c.FixtureId == item.FixtureId)) continue;

                initialResult.Add(item);

                if (initialResult.Count == 100 && _africaClientCodes.Contains(request.ClientCode.ToUpper()))
                    break;
            }

            var finalResult = new List<FixtureDTO>();

            var fixtureIds = initialResult.Select(s => Convert.ToInt64(s.FixtureId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    SortOrder = item.SortOrder,
                    Markets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates)
                });
            });

            if (request.SportId == 5)
                finalResult = finalResult.OrderBy(o => o.SegmentId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            else
                finalResult = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var final = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, final, DateTimeOffset.UtcNow.AddSeconds(5));

            return final;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(TWDailyFixtureRequestDTO request)
        {
            var mKey = $"ndf:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}:{request.MinuteUTCOffset}:{request.DateUTC}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var dateTimeRange = GetDateTimeRange(request.DateUTC, request.MinuteUTCOffset);
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureTypeId = s.FiTyId,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    SortOrder = item.SortOrder,
                    Markets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates)
                });
            });

            if (request.SportId == 5)
                finalResult = finalResult.OrderBy(o => o.SegmentId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            else
                finalResult = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };

            if (finalResult.Count > 0)
            {
                if (request.DateUTC > DateTime.UtcNow)
                    _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddMinutes(1));
                else
                    _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));
            }

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(TWLeagueDailyFixtureRequestDTO request)
        {
            var mKey = $"ndfbl:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.MinuteUTCOffset}:{request.DateUTC}:{request.SportId}:{request.SegmentId}:{request.LeagueId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var dateTimeRange = GetDateTimeRange(request.DateUTC, request.MinuteUTCOffset);
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId && c.FiStDa > dateTimeRange.Item1 && c.FiStDa < dateTimeRange.Item2);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var result = fixtures.Select(s => new TempFixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureTypeId = s.FiTyId,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    IsBetBuilder = item.IsBetBuilder,
                    SortOrder = item.SortOrder,
                    Markets = GetMainMarkets(item.SportId, item.FixtureTypeId, markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.FixtureStatusId, filteredBetBuilderMarketTemplates)
                });
            });

            if (request.SportId == 5)
                finalResult = finalResult.OrderBy(o => o.SegmentId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();
            else
                finalResult = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };

            if (finalResult.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(TWOutrightFixtureRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Outright && c.SpId == request.SportId);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

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
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureTypeId = s.FiTyId,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    SortOrder = item.SortOrder,
                    Markets = GetOutrightMarkets(markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.SportId, item.FixtureTypeId)
                });
            });

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(TWLeagueOutrightFixtureRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Outright && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

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
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureTypeId = s.FiTyId,
                MarketCount = 0,
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            Parallel.ForEach(initialResult, item =>
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
                    fixtureTypeId = item.FixtureTypeId,
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
                    Score = item.SportId == 138 ? string.Empty : item.Score,
                    CurrentPeriod = item.CurrentPeriod,
                    CurrentMinute = item.CurrentMinute,
                    BetStop = item.BetStop,
                    IsBetStop = item.IsBetStop,
                    IsBetStopGlobal = item.IsBetStopGlobal,
                    SortOrder = item.SortOrder,
                    Markets = GetOutrightMarkets(markets.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(item.FixtureId)).ToList(), request.LanguageCode, request.ClientCode, item.SportId, item.FixtureTypeId)
                });
            });

            return new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = finalResult.OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(TWTopLeagueFixtureRequestDTO request)
        {
            var mKey = $"ntlf:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var resultInitial = new List<TempTopLeague>();

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId).ToList();

            var clientTopLeagues = new List<ClientTopLeague>();

            var key = $"clo:{request.ClientId.ToString().ToUpper()}";

            var cache = _memCache.GetCache<List<ClientTopLeague>>(key);

            if (cache != null)
                clientTopLeagues = cache;
            else
            {
                clientTopLeagues = await _sportsbookContext.ClientTopLeague.Where(c => c.ClientId == request.ClientId).ToListAsync();
                _memCache.AddOrUpdateCache(key, clientTopLeagues, DateTimeOffset.UtcNow.AddMinutes(5));
            }

            if (clientTopLeagues.Count == 0)
            {
                resultInitial = fixtures.Select(s => new TempTopLeague
                {
                    SegmentId = s.SegId,
                    SegmentName = s.SegNa,
                    LeagueId = s.LeId,
                    LeagueName = s.LeNa,
                    SortOrder = sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();
            }
            else
            {
                foreach (var league in clientTopLeagues.OrderBy(o => o.SortOrder).ToList())
                {
                    var res = fixtures.Where(c => c.SegId == league.SegmentId && c.LeId == league.LeagueId).FirstOrDefault();

                    if (res != null)
                    {
                        if (!resultInitial.Any(c => c.SegmentId == res.SegId && c.LeagueId == res.LeId))
                        {
                            resultInitial.Add(new TempTopLeague
                            {
                                SegmentId = res.SegId,
                                SegmentName = res.SegNa,
                                LeagueId = res.LeId,
                                LeagueName = res.LeNa,
                                SortOrder = league.SortOrder
                            });
                        }
                    }
                }

                if (resultInitial.Count == 0)
                {
                    resultInitial = fixtures.Select(s => new TempTopLeague
                    {
                        SegmentId = s.SegId,
                        SegmentName = s.SegNa,
                        LeagueId = s.LeId,
                        LeagueName = s.LeNa,
                        SortOrder = sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                         == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ToList();
                }
            }

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

            var fixtureFilter2 = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && leagueIds.Contains(c.LeId));
            var fixtures2 = await fixtureCollection.Find(fixtureFilter2).ToListAsync();

            if (IsOEEnable(request.ClientCode))
                fixtures2 = fixtures2.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            var fixtureIds = fixtures2.Select(s => Convert.ToInt64(s.FiId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();
            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            var result = fixtures2.Select(s => new FixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                fixtureTypeId = s.FiTyId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                MarketCount = marketCount.Result.Where(c => c.FiId == s.FiId).Count(),
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.SpId == 138 ? string.Empty : s.Sc, /*don't display score for kabbadi sport*/
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
                SortOrder = clientTopLeagues.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault() == 0 ? sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault() == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault() : clientTopLeagues.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault(),
                Markets = GetMainMarkets(s.SpId, s.FiTyId, markets.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), request.LanguageCode, request.ClientCode, s.FiStId, filteredBetBuilderMarketTemplates)
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };

            if (result.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWSegmentFixtureRequestDTO request)
        {
            var mKey = $"nfbs:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}:{request.SegmentId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.SegId == request.SegmentId);
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWResponseDTO()
                };
            }

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();
            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            var sortOrder = GetTierLeagueOrder().Where(c => c.SportId == request.SportId && c.SegmentId == request.SegmentId).ToList();

            var fixtureIds = fixtures.Select(s => Convert.ToInt64(s.FiId)).ToList();
            var marketTemplateIds = GetMainMarketTemplates(request.SportId);

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var result = fixtures.Select(s => new FixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                fixtureTypeId = s.FiTyId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                MarketCount = marketCount.Result.Where(c => c.FiId == s.FiId).Count(),
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.SpId == 138 ? string.Empty : s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
                SortOrder = sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault() == 0 ? 1000 : sortOrder.Where(c => c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault(),
                Markets = GetMainMarkets(s.SpId, s.FiTyId, markets.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), request.LanguageCode, request.ClientCode, s.FiStId, filteredBetBuilderMarketTemplates)
            }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ThenBy(o => o.LeagueName).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            if (request.SportId == 5)
                result = result.OrderBy(o => o.SegmentId).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };

            if (result.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWLeagueFixtureRequestDTO request)
        {
            var mKey = $"nfbl:{request.ClientId.ToString().ToUpper()}:{request.LanguageCode.ToString().ToUpper()}:{request.SportId}:{request.SegmentId}:{request.LeagueId}";

            var mCache = _memCache.GetCache<BaseResponseDTO<TWResponseDTO>>(mKey);

            if (mCache != null)
                return mCache;

            FilterDefinition<Fixture> fixtureFilter;

            if (request.SportId == 1055 || request.SportId == 1058)
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Outright && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId && c.FiStDa > DateTime.UtcNow);
            else
                fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId && c.SegId == request.SegmentId && c.LeId == request.LeagueId);

            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

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

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();
            var filteredBetBuilderMarketTemplates = betBuilderSportIds.Contains(request.SportId) ? betBuilderMarketTemplates.Where(c => c.SportId == request.SportId).ToList() : new List<BetBuilderMarketTemplates>();

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);

            var result = fixtures.Select(s => new FixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                fixtureTypeId = s.FiTyId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IpCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                MarketCount = marketCount.Result.Where(c => c.FiId == s.FiId).Count(),
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.SpId == 138 ? string.Empty : s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStop = s.IsSu,
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBetBuilder = IsBetBuilder(s.SpId, s.FiTyId, s.FiStId, betBuilderMarketTemplates),
                Markets = GetMainMarkets(s.SpId, s.FiTyId, markets.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), request.LanguageCode, request.ClientCode, s.FiStId, filteredBetBuilderMarketTemplates)
            }).OrderBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList();

            var returnResult = new BaseResponseDTO<TWResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWResponseDTO
                {
                    Fixtures = result
                }
            };

            if (result.Count > 0)
                _memCache.AddOrUpdateCache(mKey, returnResult, DateTimeOffset.UtcNow.AddSeconds(5));

            return returnResult;
        }
        public async Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(TWSearchRequestDTO request)
        {
            var result = new List<SearchResponse>();

            FilterDefinition<Fixture> leagueFilter;
            Task<List<Fixture>> leagues;

            FilterDefinition<Fixture> fixtureFilter;
            Task<List<Fixture>> fixtures;

            var sportsId = new List<int>();
            if (request.SportGroupId != 0)
                sportsId = (await GetSportGroups()).Result.SportGroups.FirstOrDefault(c => c.SportGroupId == request.SportGroupId).Sports.Split(",").Select(s => Convert.ToInt32(s)).ToList();

            if (request.LanguageCode == LanguageCode.EN)
            {
                if (request.SportGroupId != 0)
                {
                    leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.LeNa.ToLower().Contains(request.SearchText.ToLower()) && sportsId.Contains(c.SpId));
                    fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.FiNa.ToLower().Contains(request.SearchText.ToLower()) && sportsId.Contains(c.SpId));
                }
                else
                {
                    leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.LeNa.ToLower().Contains(request.SearchText.ToLower()));
                    fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.FiNa.ToLower().Contains(request.SearchText.ToLower()));
                }

                leagues = fixtureCollection.Find(leagueFilter).ToListAsync();
                fixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();
            }
            else
            {
                if (request.SportGroupId != 0)
                {
                    leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.LeNaTr.Any(s => s.LeNa.ToLower().Contains(request.SearchText.ToLower())) && sportsId.Contains(c.SpId));
                    fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.FiNaTr.Any(s => s.FiNa.ToLower().Contains(request.SearchText.ToLower())) && sportsId.Contains(c.SpId));
                }
                else
                {
                    leagueFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.LeNaTr.Any(s => s.LeNa.ToLower().Contains(request.SearchText.ToLower())));
                    fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.FiNaTr.Any(s => s.FiNa.ToLower().Contains(request.SearchText.ToLower())));
                }

                leagues = fixtureCollection.Find(leagueFilter).ToListAsync();
                fixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();
            }

            await Task.WhenAll(leagues, fixtures);

            var finalLeagues = leagues.Result;
            if (IsOEEnable(request.ClientCode))
                finalLeagues = finalLeagues.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var finalFixtures = fixtures.Result;
            if (IsOEEnable(request.ClientCode))
                finalFixtures = finalFixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

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

                if (request.LanguageCode == LanguageCode.EN)
                {
                    if (league.LeNa.ToLower().StartsWith(request.SearchText.ToLower()))
                    {
                        leagueListStartsWith.Add(new SearchResponse
                        {
                            SportId = league.SpId,
                            SportName = GetSportName(league.SpNa, league.SpNaTr, request.LanguageCode),
                            SegmentId = league.SegId,
                            SegmentName = GetSegmentName(league.SegNa, league.SegNaTr, request.LanguageCode),
                            LeagueId = league.LeId,
                            LeagueName = GetLeagueName(league.LeNa, league.LeNaTr, request.LanguageCode),
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
                                SportName = GetSportName(league.SpNa, league.SpNaTr, request.LanguageCode),
                                SegmentId = league.SegId,
                                SegmentName = GetSegmentName(league.SegNa, league.SegNaTr, request.LanguageCode),
                                LeagueId = league.LeId,
                                LeagueName = GetLeagueName(league.LeNa, league.LeNaTr, request.LanguageCode),
                                FixtureId = string.Empty,
                                FixtureName = string.Empty
                            });
                        }
                    }
                }
                else
                {
                    if (league.LeNaTr.Where(c => c.LaCo == request.LanguageCode.ToString()).Select(s => s.LeNa).FirstOrDefault().ToLower().StartsWith(request.SearchText.ToLower()))
                    {
                        leagueListStartsWith.Add(new SearchResponse
                        {
                            SportId = league.SpId,
                            SportName = GetSportName(league.SpNa, league.SpNaTr, request.LanguageCode),
                            SegmentId = league.SegId,
                            SegmentName = GetSegmentName(league.SegNa, league.SegNaTr, request.LanguageCode),
                            LeagueId = league.LeId,
                            LeagueName = GetLeagueName(league.LeNa, league.LeNaTr, request.LanguageCode),
                            FixtureId = string.Empty,
                            FixtureName = string.Empty
                        });
                    }
                    else
                    {
                        if (league.LeNaTr.Where(c => c.LaCo == request.LanguageCode.ToString()).Select(s => s.LeNa).FirstOrDefault().ToLower().Contains(request.SearchText.ToLower()))
                        {
                            leagueListNotStartsWith.Add(new SearchResponse
                            {
                                SportId = league.SpId,
                                SportName = GetSportName(league.SpNa, league.SpNaTr, request.LanguageCode),
                                SegmentId = league.SegId,
                                SegmentName = GetSegmentName(league.SegNa, league.SegNaTr, request.LanguageCode),
                                LeagueId = league.LeId,
                                LeagueName = GetLeagueName(league.LeNa, league.LeNaTr, request.LanguageCode),
                                FixtureId = string.Empty,
                                FixtureName = string.Empty
                            });
                        }
                    }
                }

            }

            foreach (var fixture in finalFixtures)
            {
                if (fixtureListStartsWith.Any(c => c.FixtureId == fixture.FiId.ToString())) continue;
                if (fixtureListNotStartsWith.Any(c => c.FixtureId == fixture.FiId.ToString())) continue;

                if (request.LanguageCode == LanguageCode.EN)
                {
                    if (fixture.FiNa.Split(" vs. ").First().ToLower().StartsWith(request.SearchText.ToLower()) || fixture.FiNa.Split(" vs. ").Last().ToLower().StartsWith(request.SearchText.ToLower()))
                    {
                        fixtureListStartsWith.Add(new SearchResponse
                        {
                            SportId = fixture.SpId,
                            SportName = GetSportName(fixture.SpNa, fixture.SpNaTr, request.LanguageCode),
                            SegmentId = fixture.SegId,
                            SegmentName = GetSegmentName(fixture.SegNa, fixture.SegNaTr, request.LanguageCode),
                            LeagueId = fixture.LeId,
                            LeagueName = GetLeagueName(fixture.LeNa, fixture.LeNaTr, request.LanguageCode),
                            FixtureId = fixture.FiId.ToString(),
                            FixtureName = GetFixtureName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode)
                        });
                    }
                    else
                    {
                        if (fixture.FiNa.ToLower().Contains(request.SearchText.ToLower()))
                        {
                            fixtureListNotStartsWith.Add(new SearchResponse
                            {
                                SportId = fixture.SpId,
                                SportName = GetSportName(fixture.SpNa, fixture.SpNaTr, request.LanguageCode),
                                SegmentId = fixture.SegId,
                                SegmentName = GetSegmentName(fixture.SegNa, fixture.SegNaTr, request.LanguageCode),
                                LeagueId = fixture.LeId,
                                LeagueName = GetLeagueName(fixture.LeNa, fixture.LeNaTr, request.LanguageCode),
                                FixtureId = fixture.FiId.ToString(),
                                FixtureName = GetFixtureName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode)
                            });
                        }
                    }
                }
                else
                {
                    if (fixture.FiNaTr.Where(c => c.LaCo == request.LanguageCode.ToString()).Select(s => s.FiNa).FirstOrDefault().Split(" vs. ").First().ToLower().StartsWith(request.SearchText.ToLower()) || fixture.FiNa.Split(" vs. ").Last().ToLower().StartsWith(request.SearchText.ToLower()))
                    {
                        fixtureListStartsWith.Add(new SearchResponse
                        {
                            SportId = fixture.SpId,
                            SportName = GetSportName(fixture.SpNa, fixture.SpNaTr, request.LanguageCode),
                            SegmentId = fixture.SegId,
                            SegmentName = GetSegmentName(fixture.SegNa, fixture.SegNaTr, request.LanguageCode),
                            LeagueId = fixture.LeId,
                            LeagueName = GetLeagueName(fixture.LeNa, fixture.LeNaTr, request.LanguageCode),
                            FixtureId = fixture.FiId.ToString(),
                            FixtureName = GetFixtureName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode)
                        });
                    }
                    else
                    {
                        if (fixture.FiNaTr.Where(c => c.LaCo == request.LanguageCode.ToString()).Select(s => s.FiNa).FirstOrDefault().Split(" vs. ").First().ToLower().Contains(request.SearchText.ToLower()))
                        {
                            fixtureListNotStartsWith.Add(new SearchResponse
                            {
                                SportId = fixture.SpId,
                                SportName = GetSportName(fixture.SpNa, fixture.SpNaTr, request.LanguageCode),
                                SegmentId = fixture.SegId,
                                SegmentName = GetSegmentName(fixture.SegNa, fixture.SegNaTr, request.LanguageCode),
                                LeagueId = fixture.LeId,
                                LeagueName = GetLeagueName(fixture.LeNa, fixture.LeNaTr, request.LanguageCode),
                                FixtureId = fixture.FiId.ToString(),
                                FixtureName = GetFixtureName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode)
                            });
                        }
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
            var tempFixture = fixtureCollection.Find(fixtureFilter).FirstOrDefaultAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixture, tempBettingStatus);

            var fixture = tempFixture.Result;

            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

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
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);


            var clientInitial = GetClientMarketTemplateGroups().Where(c => c.SportId == fixture.SpId && c.ClientId == request.ClientId).ToList();
            var marketTemplateGroups = GetSportMarketTemplateGroup(fixture.SpId, request.ClientId, clientInitial);

            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            var filteredBetBuilderMarketTemplates = new List<BetBuilderMarketTemplates>();
            var isBetBuilder = false;

            if (betBuilderSportIds.Contains(fixture.SpId) && fixture.FiTyId == (int)FixtureType.Match)
            {
                if (fixture.FiStId == (int)FixtureStatus.Prematch)
                    filteredBetBuilderMarketTemplates = betBuilderMarketTemplates.Where(c => c.SportId == fixture.SpId && c.Prematch == true).ToList();
                else if (fixture.FiStId == (int)FixtureStatus.Live)
                    filteredBetBuilderMarketTemplates = betBuilderMarketTemplates.Where(c => c.SportId == fixture.SpId && c.Live == true).ToList();

                isBetBuilder = filteredBetBuilderMarketTemplates.Any() ? true : false;
            }

            var finalMarkets = GetMarkets(markets.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList(), request.LanguageCode, request.ClientCode, fixture.SpId, fixture.FiTyId, fixture.FiStId, clientInitial, filteredBetBuilderMarketTemplates);

            var result = new List<FixtureDTO>
            {
                new FixtureDTO
                {
                    SportId = fixture.SpId,
                    SportName = GetSportName(fixture.SpNa, fixture.SpNaTr, request.LanguageCode),
                    SegmentId = fixture.SegId,
                    SegmentName = GetSegmentName(fixture.SegNa, fixture.SegNaTr, request.LanguageCode),
                    LeagueId = fixture.LeId,
                    LeagueName = GetLeagueName(fixture.LeNa, fixture.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = fixture.PrFiId,
                    fixtureTypeId = fixture.FiTyId,
                    FixtureId = fixture.FiId.ToString(),
                    FixtureName = GetFixtureName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode, (FixtureType)fixture.FiTyId),
                    AwayName = GetAwayName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode, (FixtureType)fixture.FiTyId),
                    FixtureStartDate = fixture.FiStDa,
                    IsLive = fixture.IsOfLi,
                    IsLiveStream = IsLiveStream(fixture.LiId, fixture.GeAl, request.IpCountryCode),
                    FixtureStatusId = fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright ? fixture.FiStId : !IsOEEnable(request.ClientCode) || fixture.ClCo.Contains(request.ClientCode) ? fixture.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright ? fixture.FiStNa : !IsOEEnable(request.ClientCode) || fixture.ClCo.Contains(request.ClientCode) ? fixture.FiStNa : FixtureStatus.Closed.ToString(),
                    MarketCount = markets.Result.Where(c => c.FiId == fixture.FiId).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, fixture.StUr, request.LanguageCode),
                    Score = fixture.SpId == 138 ? string.Empty : fixture.Sc, /*empty score for kabaddi sport*/
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(fixture.CuPe),
                    CurrentMinute = fixture.CuMi.ToString(),
                    BetStop = fixture.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = fixture.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    IsBetBuilder = isBetBuilder,
                    MarketTemplateGroups = marketTemplateGroups,
                    Markets = finalMarkets
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
        public async Task<BaseResponseDTO<TWLimitedResponseDTO>> GetFixtureDetailsLimited(TWFixtureDetailsRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiId == Convert.ToInt64(request.FixtureId));
            var tempFixture = fixtureCollection.Find(fixtureFilter).FirstOrDefaultAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixture, tempBettingStatus);

            var fixture = tempFixture.Result;
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixture == null)
            {
                return new BaseResponseDTO<TWLimitedResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new TWLimitedResponseDTO()
                };
            }

            var fixtureIds = new List<long> { fixture.FiId };

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.ClCo).Include(c => c.ClCoMa);
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            var result = new List<FixtureLimitedDTO>
            {
                new FixtureLimitedDTO
                {
                    SportId = fixture.SpId,
                    SportName = fixture.SpNa,
                    SegmentId = fixture.SegId,
                    SegmentName = fixture.SegNa,
                    LeagueId = fixture.LeId,
                    LeagueName = fixture.LeNa,
                    FixtureId = fixture.FiId.ToString(),
                    ProviderFixtureId = fixture.PrFiId,
                    FixtureName = fixture.FiNa,
                    Home = fixture.FiTyId == (int)FixtureType.Match ? fixture.FiNa.Split(" vs. ").First().Trim() : string.Empty,
                    Away = fixture.FiTyId == (int)FixtureType.Match ? fixture.FiNa.Split(" vs. ").Last().Trim() : string.Empty,
                    FixtureStartDate = fixture.FiStDa,
                    FixtureStatus = fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright ? fixture.FiStNa : !IsOEEnable(request.ClientCode) || fixture.ClCo.Contains(request.ClientCode) ? fixture.FiStNa : FixtureStatus.Closed.ToString(),
                    IsLive = fixture.SpId == 21 ? fixture.FiStId == (int)FixtureStatus.Live ? true : false : fixture.IsOfLi,
                    IsLiveStream = IsLiveStream(fixture.LiId, fixture.GeAl, request.IpCountryCode),
                    Markets = GetMarketsLimited(markets.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList(), request.ClientCode, fixture.SpId, fixture.FiTyId)
                }
            };

            return new BaseResponseDTO<TWLimitedResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new TWLimitedResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtureMarketSelectionDetails(TWFixtureMarketSelectionDetailsRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiId == Convert.ToInt64(request.FixtureId));
            var tempFixture = fixtureCollection.Find(fixtureFilter).FirstOrDefaultAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixture, tempBettingStatus);

            var fixture = tempFixture.Result;
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

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

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && c.MaId == Convert.ToInt64(request.MarketId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(markets, selections);

            var clientInitial = GetClientMarketTemplateGroups().Where(c => c.SportId == fixture.SpId && c.ClientId == request.ClientId).ToList();
            var marketTemplateGroups = GetSportMarketTemplateGroup(fixture.SpId, request.ClientId, clientInitial);

            var tempMarkets = GetMarkets(markets.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(fixture.FiId)).ToList(), request.LanguageCode, request.ClientCode, fixture.SpId, fixture.FiTyId, fixture.FiStId, clientInitial, new List<BetBuilderMarketTemplates>());

            var finalMarkets = new List<MarketDTO>();
            foreach (var market in tempMarkets)
            {
                if (market.Selections.Any(s => s.SelectionId == request.SelectionId))
                {
                    finalMarkets.Add(new MarketDTO
                    {
                        MarketId = market.MarketId,
                        MarketName = market.MarketName,
                        MarketStatusId = market.MarketStatusId,
                        MarketStatusName = market.MarketStatusName,
                        MarketTemplateId = market.MarketTemplateId,
                        Line = market.Line,
                        IsCashout = market.IsCashout,
                        Selections = market.Selections.Where(s => s.SelectionId == request.SelectionId).ToList()
                    });
                }
            }

            var result = new List<FixtureDTO>
            {
                new FixtureDTO
                {
                    SportId = fixture.SpId,
                    SportName = GetSportName(fixture.SpNa, fixture.SpNaTr, request.LanguageCode),
                    SegmentId = fixture.SegId,
                    SegmentName = GetSegmentName(fixture.SegNa, fixture.SegNaTr, request.LanguageCode),
                    LeagueId = fixture.LeId,
                    LeagueName = GetLeagueName(fixture.LeNa, fixture.LeNaTr, request.LanguageCode),
                    ProviderFixtureId = fixture.PrFiId,
                    fixtureTypeId = fixture.FiTyId,
                    FixtureId = fixture.FiId.ToString(),
                    FixtureName = GetFixtureName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode),
                    HomeName = GetHomeName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode, (FixtureType)fixture.FiTyId),
                    AwayName = GetAwayName(fixture.FiNa, fixture.FiNaTr, request.LanguageCode, (FixtureType)fixture.FiTyId),
                    FixtureStartDate = fixture.FiStDa,
                    IsLive = fixture.IsOfLi,
                    IsLiveStream = IsLiveStream(fixture.LiId, fixture.GeAl, request.IpCountryCode),
                    FixtureStatusId = fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright ? fixture.FiStId : !IsOEEnable(request.ClientCode) || fixture.ClCo.Contains(request.ClientCode) ? fixture.FiStId : (int)FixtureStatus.Closed,
                    FixtureStatusName = fixture.SpId > 1000 || fixture.FiTyId == (int)FixtureType.Outright ? fixture.FiStNa : !IsOEEnable(request.ClientCode) || fixture.ClCo.Contains(request.ClientCode) ? fixture.FiStNa : FixtureStatus.Closed.ToString(),
                    MarketCount = markets.Result.Where(c => c.FiId == fixture.FiId).Count(),
                    SMSCode = string.Empty,
                    StatisticsURL = GetStatisticsURL(request.ClientId, fixture.StUr, request.LanguageCode),
                    Score = fixture.SpId == 138 ? string.Empty : fixture.Sc,
                    CurrentPeriod = CurrentPeriodHelper.ToShortName(fixture.CuPe),
                    CurrentMinute = fixture.CuMi.ToString(),
                    BetStop = fixture.IsSu ? new List<int> { 1 } : new List<int> { },
                    IsBetStop = fixture.IsSu,
                    IsBetStopGlobal = bettingStatus.IsBeSo,
                    Markets = finalMarkets
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
        private SportMarketTemplateGroup GetSportMarketTemplateGroup(int sportId, Guid clientId, List<ClientMarketTemplateGroup> clientInitial)
        {
            var sportMarketTemplateGroup = new SportMarketTemplateGroup();

            if (clientInitial.Any())
            {
                var prematch = clientInitial.Where(c => c.FixtureStatusId == 0 && c.MarketTemplates != "0").ToList();
                var live = clientInitial.Where(c => c.FixtureStatusId == 1 && c.MarketTemplates != "0").ToList();

                sportMarketTemplateGroup = new SportMarketTemplateGroup
                {
                    SportId = sportId,
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
            }
            else
            {
                var initial = GetMarketTemplateGroup().Where(c => c.SportId == sportId).ToList();

                var prematch = initial.Where(c => c.FixtureStatusId == 0 && c.MarketTemplates != "0").ToList();
                var live = initial.Where(c => c.FixtureStatusId == 1 && c.MarketTemplates != "0").ToList();

                sportMarketTemplateGroup = new SportMarketTemplateGroup
                {
                    SportId = sportId,
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
            }

            return sportMarketTemplateGroup;
        }
        private List<MarketLimitedDTO> GetMarketsLimited(List<MarketFilter> markets, List<SelectionFilter> selections, string clientCode, int sportId, int fixtureTypeId)
        {
            if (IsOEEnable(clientCode))
            {
                if (sportId > 1000 || fixtureTypeId == (int)FixtureType.Outright)
                {
                    var result = new List<MarketLimitedDTO>();

                    foreach (var market in markets)
                    {
                        var filteredSelections = selections.Where(c => c.MaId == market.MaId).ToList();
                        var isBinary = filteredSelections.Count > 2 ? false : true;

                        result.Add(new MarketLimitedDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = market.MaNa,
                            Line = market.Li,
                            MarketStatus = market.MaStNa,
                            Selections = filteredSelections.Select(s => new SelectionLimitedDTO
                            {
                                SelectionId = s.SeId.ToString(),
                                SelectionName = s.SeNa,
                                Odds = s.Od.ToString("0.00"),
                                //MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                //HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                //IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                            }).OrderBy(o => Convert.ToInt64(o.SelectionId)).ToList()
                        });
                    }

                    return result.OrderBy(o => Convert.ToInt32(o.MarketTemplateId)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
                else
                {
                    var filteredMarkets = markets.Where(c => c.ClCo != null && c.ClCo.Contains(clientCode)).ToList();
                    var result = new List<MarketLimitedDTO>();

                    foreach (var market in filteredMarkets)
                    {
                        var clientOdds = new List<ClientOdds>();

                        var tempSelections = selections.Where(c => c.MaId == market.MaId).ToList();

                        if (tempSelections.Count > 0)
                        {
                            var activeSelections = tempSelections.Where(c => c.IsAc == true && c.Od > 0m).ToList();
                            var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

                            clientOdds = GetClientOdds(activeSelections.Count, margin, tempSelections, market.ClCoMa, clientCode);
                        }

                        result.Add(new MarketLimitedDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = market.MaNa,
                            Line = market.Li,
                            MarketStatus = market.ClCo.Contains(clientCode) ? market.MaStNa : MarketStatus.Inactive.ToString(),
                            Selections = selections.Where(c => c.MaId == market.MaId).Select(s => new SelectionLimitedDTO
                            {
                                SelectionId = s.SeId.ToString(),
                                SelectionName = s.SeNa,
                                //Odds = clientOdds.Where(c => c.SelectionId == s.SeId).Select(ss => ss.Odds).FirstOrDefault().ToString(),
                                Odds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).DecimalOdds.ToString(),
                                //MalaysianOdds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).MalaysianOdds.ToString(),
                                //HongkongOdds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).HongkongOdds.ToString(),
                                //IndonesianOdds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).IndonesianOdds.ToString(),
                            }).OrderBy(o => Convert.ToInt64(o.SelectionId)).ToList()
                        });
                    }

                    return result.OrderBy(o => Convert.ToInt32(o.MarketTemplateId)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
            }
            else
            {
                var result = new List<MarketLimitedDTO>();

                foreach (var market in markets)
                {
                    var filteredSelections = selections.Where(c => c.MaId == market.MaId).ToList();
                    var isBinary = filteredSelections.Count > 2 ? false : true;

                    result.Add(new MarketLimitedDTO
                    {
                        MarketTemplateId = market.MaTeId,
                        MarketId = market.MaId.ToString(),
                        MarketName = market.MaNa,
                        Line = market.Li,
                        MarketStatus = market.MaStNa,
                        Selections = filteredSelections.Select(s => new SelectionLimitedDTO
                        {
                            SelectionId = s.SeId.ToString(),
                            SelectionName = s.SeNa,
                            Odds = s.Od.ToString("0.00"),
                            //MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                            //HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                            //IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                        }).OrderBy(o => Convert.ToInt64(o.SelectionId)).ToList()
                    });
                }

                return result.OrderBy(o => Convert.ToInt32(o.MarketTemplateId)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
            }

        }
        private List<MarketDTO> GetMainMarkets(int sportId, int fixtureTypeId, List<MarketFilter> markets, List<SelectionFilter> selections, LanguageCode languageCode, string clientCode, int fixtureStatusId, List<BetBuilderMarketTemplates> betBuilderMarketTemplates)
        {
            var marketTemplates = GetMainMarketTemplates(sportId);

            if (IsOEEnable(clientCode))
            {
                if (sportId > 1000 || fixtureTypeId == (int)FixtureType.Outright)
                {
                    var filteredMarkets = markets.Where(c => marketTemplates.Contains(c.MaTeId)).ToList();
                    var result = new List<MarketDTO>();

                    foreach (var market in filteredMarkets)
                    {
                        var filteredSelections = selections.Where(c => c.MaId == market.MaId).ToList();
                        var isBinary = filteredSelections.Count > 2 ? false : true;

                        result.Add(new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.MaStId,
                            MarketStatusName = market.MaStNa,
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            IsBetBuilder = false,
                            Selections = filteredSelections.Select(s => new SelectionDTO
                            {
                                SelectionTemplateId = s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                                DecimalOdds = s.Od.ToString("0.00"),
                                MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                                SortOrder = s.SeSoOr
                            }).OrderBy(o => o.SortOrder).ToList()
                        });
                    }

                    return result.OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
                else
                {
                    var filteredMarkets = markets.Where(c => marketTemplates.Contains(c.MaTeId) && c.ClCo != null && c.ClCo.Contains(clientCode)).ToList();
                    var result = new List<MarketDTO>();

                    foreach (var market in filteredMarkets)
                    {
                        var clientOdds = new List<ClientOdds>();

                        var tempSelections = selections.Where(c => c.MaId == market.MaId).ToList();

                        if (tempSelections.Count > 0)
                        {
                            var activeSelections = tempSelections.Where(c => c.IsAc == true && c.Od > 0m).ToList();
                            var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

                            clientOdds = GetClientOdds(activeSelections.Count, margin, tempSelections, market.ClCoMa, clientCode);
                        }

                        var selectionsToAdd = new List<SelectionDTO>();

                        foreach (var item in selections.Where(c => c.MaId == market.MaId).ToList())
                        {
                            var odds = clientOdds.FirstOrDefault(c => c.SelectionId == item.SeId);

                            selectionsToAdd.Add(new SelectionDTO
                            {
                                SelectionTemplateId = item.SeTeId,
                                SelectionId = item.SeId.ToString(),
                                SelectionName = GetSelectionName(item.SeNa, item.SeNaTr, languageCode),
                                DecimalOdds = odds.DecimalOdds.ToString("0.00"),
                                MalaysianOdds = odds.MalaysianOdds.ToString("0.00"),
                                HongkongOdds = odds.HongkongOdds.ToString("0.00"),
                                IndonesianOdds = odds.IndonesianOdds.ToString("0.00"),
                                IsActive = odds.DecimalOdds > 1m && item.IsAc,
                                CorrelationId = item.CoId,
                                SortOrder = item.SeSoOr
                            });
                        }

                        var isBetBuilder = false;
                        var betBuilderMarketTemplate = betBuilderMarketTemplates.FirstOrDefault(c => c.SportId == sportId && c.MarketTemplateId == market.MaTeId);

                        if (betBuilderMarketTemplate != null)
                        {
                            if (fixtureStatusId == (int)FixtureStatus.Prematch && betBuilderMarketTemplate.Prematch == true)
                            {
                                isBetBuilder = true;
                            }
                            else if (fixtureStatusId == (int)FixtureStatus.Live && betBuilderMarketTemplate.Live == true)
                            {
                                isBetBuilder = true;
                            }
                        }

                        var marketToAdd = new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.ClCo.Contains(clientCode) ? market.MaStId : (int)MarketStatus.Inactive,
                            MarketStatusName = market.ClCo.Contains(clientCode) ? market.MaStNa : MarketStatus.Inactive.ToString(),
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            IsBetBuilder = isBetBuilder,
                            Selections = selectionsToAdd.OrderBy(o => o.SortOrder).ToList()
                        };

                        result.Add(marketToAdd);
                    }

                    return result.OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
            }
            else
            {
                var filteredMarkets = markets.Where(c => marketTemplates.Contains(c.MaTeId)).ToList();
                var result = new List<MarketDTO>();

                foreach (var market in filteredMarkets)
                {
                    var isBetBuilder = false;
                    var betBuilderMarketTemplate = betBuilderMarketTemplates.FirstOrDefault(c => c.SportId == sportId && c.MarketTemplateId == market.MaTeId);

                    if (fixtureTypeId == (int)FixtureType.Match && betBuilderMarketTemplate != null)
                    {
                        if (fixtureStatusId == (int)FixtureStatus.Prematch && betBuilderMarketTemplate.Prematch == true)
                        {
                            isBetBuilder = true;
                        }
                        else if (fixtureStatusId == (int)FixtureStatus.Live && betBuilderMarketTemplate.Live == true)
                        {
                            isBetBuilder = true;
                        }
                    }

                    var filteredSelections = selections.Where(c => c.MaId == market.MaId).ToList();
                    var isBinary = filteredSelections.Count > 2 ? false : true;

                    result.Add(new MarketDTO
                    {
                        MarketTemplateId = market.MaTeId,
                        MarketId = market.MaId.ToString(),
                        MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                        Line = market.Li,
                        IsCashout = market.CaStId == 1 ? true : false,
                        MarketStatusId = market.MaStId,
                        MarketStatusName = market.MaStNa,
                        SortOrder = Convert.ToInt32(market.MaTeId),
                        IsBetBuilder = isBetBuilder,
                        Selections = filteredSelections.Select(s => new SelectionDTO
                        {
                            SelectionTemplateId = s.SeTeId,
                            SelectionId = s.SeId.ToString(),
                            SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                            DecimalOdds = s.Od.ToString("0.00"),
                            MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                            HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                            IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                            IsActive = s.IsAc,
                            CorrelationId = s.CoId,
                            SortOrder = s.SeSoOr
                        }).OrderBy(o => o.SortOrder).ToList()
                    });
                }

                return result.OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
            }

        }
        private List<BoostedMarketDTO> GetBoostedMarkets(int sportId, int fixtureTypeId, List<MarketFilter> markets, List<SelectionFilter> selections, LanguageCode languageCode, string clientCode, bool isBoosted, int fixtureStatusId, List<ClientMarketTemplateGroup> clientMarketTemplateGroups)
        {
            if (IsOEEnable(clientCode))
            {
                if (sportId > 1000 || fixtureTypeId == (int)FixtureType.Outright)
                {
                    var filteredMarkets = markets.Where(c => c.IsBo == true).ToList();
                    var result = new List<BoostedMarketDTO>();

                    foreach (var market in filteredMarkets)
                    {
                        var filteredSelections = selections.Where(c => c.MaId == market.MaId && !string.IsNullOrEmpty(c.ClBoOd) && c.ClBoOd.Contains(clientCode)).ToList();
                        var isBinary = filteredSelections.Count > 2 ? false : true;

                        result.Add(new BoostedMarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.MaStId,
                            MarketStatusName = market.MaStNa,
                            IsBoosted = market.IsBo,
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            Selections = filteredSelections.Select(s => new BoostedSelectionDTO
                            {
                                SelectionTemplateId = s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetBoostedSelectionName(clientCode, s.SeNa, s.ClBoSeNa, s.ClBoSeNaTr, languageCode),
                                DecimalOdds = s.Od.ToString("0.00"),
                                MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                                BoostedOdds = GetClientBoostedOdds(s.ClBoOd, clientCode, isBoosted, s.Od.ToString("0.00")),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                                SortOrder = s.SeSoOr,
                            }).OrderBy(o => o.SortOrder).ToList()
                        });
                    }

                    return result.Where(m => m.Selections.Any()).OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
                else
                {
                    var marketTemplateIds = new List<string>();

                    if (clientMarketTemplateGroups.Any())
                    {
                        var marketTemplateGroup = clientMarketTemplateGroups.FirstOrDefault(c => c.FixtureStatusId == fixtureStatusId && c.Name.ToLower() == "all markets");
                        marketTemplateIds = marketTemplateGroup != null ? marketTemplateGroup.MarketTemplates.Split(',').ToList() : new List<string>();
                    }

                    var filteredMarkets = markets.Where(c => c.ClCo != null && c.ClCo.Contains(clientCode) && c.IsBo == true).ToList(); //marketTemplates.Contains(c.MaTeId) &&
                    var result = new List<BoostedMarketDTO>();

                    foreach (var market in filteredMarkets)
                    {
                        var clientOdds = new List<ClientOdds>();

                        var tempSelections = selections.Where(c => c.MaId == market.MaId).ToList();

                        if (tempSelections.Count > 0)
                        {
                            var activeSelections = tempSelections.Where(c => c.IsAc == true && c.Od > 0m).ToList();
                            var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

                            clientOdds = GetClientOdds(activeSelections.Count, margin, tempSelections, market.ClCoMa, clientCode);
                        }

                        result.Add(new BoostedMarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.ClCo.Contains(clientCode) ? market.MaStId : (int)MarketStatus.Inactive,
                            MarketStatusName = market.ClCo.Contains(clientCode) ? market.MaStNa : MarketStatus.Inactive.ToString(),
                            IsBoosted = market.IsBo,
                            SortOrder = marketTemplateIds.Any(c => c == market.MaTeId) ? marketTemplateIds.FindIndex(c => c == market.MaTeId) + 1 : 1000 + Convert.ToInt32(market.MaTeId),
                            Selections = selections.Where(c => c.MaId == market.MaId && !string.IsNullOrEmpty(c.ClBoOd) && c.ClBoOd.Contains(clientCode)).Select(s => new BoostedSelectionDTO
                            {
                                SelectionTemplateId = s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetBoostedSelectionName(clientCode, s.SeNa, s.ClBoSeNa, s.ClBoSeNaTr, languageCode),
                                //DecimalOdds = clientOdds.Where(c => c.SelectionId == s.SeId).Select(ss => ss.Odds).FirstOrDefault().ToString("0.00"),
                                //BoostedOdds = GetClientBoostedOdds(s.ClBoOd, clientCode, isBoosted, clientOdds.Where(c => c.SelectionId == s.SeId).Select(ss => ss.Odds).FirstOrDefault().ToString("0.00")),
                                DecimalOdds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).DecimalOdds.ToString("0.00"),
                                MalaysianOdds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).MalaysianOdds.ToString("0.00"),
                                HongkongOdds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).HongkongOdds.ToString("0.00"),
                                IndonesianOdds = clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).IndonesianOdds.ToString("0.00"),
                                BoostedOdds = GetClientBoostedOdds(s.ClBoOd, clientCode, isBoosted, clientOdds.FirstOrDefault(c => c.SelectionId == s.SeId).DecimalOdds.ToString("0.00")),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                                SortOrder = s.SeSoOr
                            }).OrderBy(o => o.SortOrder).ToList()
                        });
                    }

                    return result.Where(m => m.Selections.Any()).OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
            }
            else
            {
                var marketTemplateIds = new List<string>();

                if (clientMarketTemplateGroups.Any())
                {
                    var marketTemplateGroup = clientMarketTemplateGroups.FirstOrDefault(c => c.FixtureStatusId == fixtureStatusId && c.Name.ToLower() == "all markets");
                    marketTemplateIds = marketTemplateGroup != null ? marketTemplateGroup.MarketTemplates.Split(',').ToList() : new List<string>();
                }

                var filteredMarkets = markets.Where(c => c.IsBo == true).ToList(); //marketTemplates.Contains(c.MaTeId) && 
                var result = new List<BoostedMarketDTO>();

                foreach (var market in filteredMarkets)
                {
                    var filteredSelections = selections.Where(c => c.MaId == market.MaId && !string.IsNullOrEmpty(c.ClBoOd) && c.ClBoOd.Contains(clientCode)).ToList();
                    var isBinary = filteredSelections.Count > 2 ? false : true;

                    result.Add(new BoostedMarketDTO
                    {
                        MarketTemplateId = market.MaTeId,
                        MarketId = market.MaId.ToString(),
                        MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                        Line = market.Li,
                        IsCashout = market.CaStId == 1 ? true : false,
                        MarketStatusId = market.MaStId,
                        MarketStatusName = market.MaStNa,
                        IsBoosted = market.IsBo,
                        SortOrder = marketTemplateIds.Any(c => c == market.MaTeId) ? marketTemplateIds.FindIndex(c => c == market.MaTeId) + 1 : 1000 + Convert.ToInt32(market.MaTeId),
                        Selections = filteredSelections.Select(s => new BoostedSelectionDTO
                        {
                            SelectionTemplateId = s.SeTeId,
                            SelectionId = s.SeId.ToString(),
                            SelectionName = GetBoostedSelectionName(clientCode, s.SeNa, s.ClBoSeNa, s.ClBoSeNaTr, languageCode),
                            DecimalOdds = s.Od.ToString("0.00"),
                            MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                            HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                            IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                            BoostedOdds = GetClientBoostedOdds(s.ClBoOd, clientCode, isBoosted, s.Od.ToString("0.00")),
                            IsActive = s.IsAc,
                            CorrelationId = s.CoId,
                            SortOrder = s.SeSoOr
                        }).OrderBy(o => o.SortOrder).ToList()
                    });
                }

                return result.Where(m => m.Selections.Any()).OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
            }

        }
        private List<MarketDTO> GetMarkets(List<MarketFilter> markets, List<SelectionFilter> selections, LanguageCode languageCode, string clientCode, int sportId, int fixtureTypeId, int fixtureStatusId, List<ClientMarketTemplateGroup> clientMarketTemplateGroups, List<BetBuilderMarketTemplates> betBuilderMarketTemplates)
        {
            if (IsOEEnable(clientCode))
            {
                if (sportId > 1000 || fixtureTypeId == (int)FixtureType.Outright)
                {
                    var result = new List<MarketDTO>();

                    foreach (var market in markets)
                    {
                        var filteredSelections = selections.Where(c => c.MaId == market.MaId).ToList();
                        var isBinary = filteredSelections.Count > 2 ? false : true;

                        result.Add(new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.MaStId,
                            MarketStatusName = market.MaStNa,
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            IsBetBuilder = false,
                            Selections = filteredSelections.Select(s => new SelectionDTO
                            {
                                SelectionTemplateId = s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                                DecimalOdds = s.Od.ToString("0.00"),
                                MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                                SortOrder = s.SeSoOr
                            }).OrderBy(o => o.SortOrder).ToList()
                        });
                    }

                    return result.OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
                else
                {
                    var marketTemplateIds = new List<string>();

                    if (clientMarketTemplateGroups.Any())
                    {
                        var marketTemplateGroup = clientMarketTemplateGroups.FirstOrDefault(c => c.FixtureStatusId == fixtureStatusId && c.Name.ToLower() == "all markets");
                        marketTemplateIds = marketTemplateGroup != null ? marketTemplateGroup.MarketTemplates.Split(',').ToList() : new List<string>();
                    }

                    var filteredMarkets = markets.Where(c => c.ClCo != null && c.ClCo.Contains(clientCode)).ToList();
                    var result = new List<MarketDTO>();

                    foreach (var market in filteredMarkets)
                    {
                        var clientOdds = new List<ClientOdds>();

                        var tempSelections = selections.Where(c => c.MaId == market.MaId).ToList();

                        if (tempSelections.Count > 0)
                        {
                            var activeSelections = tempSelections.Where(c => c.IsAc == true && c.Od > 0m).ToList();
                            var margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

                            clientOdds = GetClientOdds(activeSelections.Count, margin, tempSelections, market.ClCoMa, clientCode);
                        }

                        var selectionsToAdd = new List<SelectionDTO>();

                        foreach (var item in selections.Where(c => c.MaId == market.MaId).ToList())
                        {
                            var odds = clientOdds.FirstOrDefault(c => c.SelectionId == item.SeId);

                            selectionsToAdd.Add(new SelectionDTO
                            {
                                SelectionTemplateId = item.SeTeId,
                                SelectionId = item.SeId.ToString(),
                                SelectionName = GetSelectionName(item.SeNa, item.SeNaTr, languageCode),
                                DecimalOdds = odds.DecimalOdds.ToString("0.00"),
                                MalaysianOdds = odds.MalaysianOdds.ToString("0.00"),
                                HongkongOdds = odds.HongkongOdds.ToString("0.00"),
                                IndonesianOdds = odds.IndonesianOdds.ToString("0.00"),
                                IsActive = odds.DecimalOdds > 1m && item.IsAc,
                                CorrelationId = item.CoId,
                                SortOrder = item.SeSoOr
                            });
                        }

                        var isBetBuilder = false;
                        var betBuilderMarketTemplate = betBuilderMarketTemplates.FirstOrDefault(c => c.SportId == sportId && c.MarketTemplateId == market.MaTeId);

                        if (fixtureTypeId == (int)FixtureType.Match && betBuilderMarketTemplate != null)
                        {
                            if (fixtureStatusId == (int)FixtureStatus.Prematch && betBuilderMarketTemplate.Prematch == true)
                            {
                                isBetBuilder = true;
                            }
                            else if (fixtureStatusId == (int)FixtureStatus.Live && betBuilderMarketTemplate.Live == true)
                            {
                                isBetBuilder = true;
                            }
                        }

                        var marketToAdd = new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.ClCo.Contains(clientCode) ? market.MaStId : (int)MarketStatus.Inactive,
                            MarketStatusName = market.ClCo.Contains(clientCode) ? market.MaStNa : MarketStatus.Inactive.ToString(),
                            SortOrder = marketTemplateIds.Any(c => c == market.MaTeId) ? marketTemplateIds.FindIndex(c => c == market.MaTeId) + 1 : 1000 + Convert.ToInt32(market.MaTeId),
                            IsBetBuilder = isBetBuilder,
                            Selections = selectionsToAdd.OrderBy(o => o.SortOrder).ToList()
                        };

                        result.Add(marketToAdd);
                    }

                    return result.OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
                }
            }
            else
            {
                var marketTemplateIds = new List<string>();

                if (clientMarketTemplateGroups.Any())
                {
                    var marketTemplateGroup = clientMarketTemplateGroups.FirstOrDefault(c => c.FixtureStatusId == fixtureStatusId && c.Name.ToLower() == "all markets");
                    marketTemplateIds = marketTemplateGroup != null ? marketTemplateGroup.MarketTemplates.Split(',').ToList() : new List<string>();
                }

                var result = new List<MarketDTO>();

                foreach (var market in markets)
                {
                    var isBetBuilder = false;
                    var betBuilderMarketTemplate = betBuilderMarketTemplates.FirstOrDefault(c => c.SportId == sportId && c.MarketTemplateId == market.MaTeId);

                    if (fixtureTypeId == (int)FixtureType.Match && betBuilderMarketTemplate != null)
                    {
                        if (fixtureStatusId == (int)FixtureStatus.Prematch && betBuilderMarketTemplate.Prematch == true)
                        {
                            isBetBuilder = true;
                        }
                        else if (fixtureStatusId == (int)FixtureStatus.Live && betBuilderMarketTemplate.Live == true)
                        {
                            isBetBuilder = true;
                        }
                    }

                    var filteredSelections = selections.Where(c => c.MaId == market.MaId).ToList();
                    var isBinary = filteredSelections.Count > 2 ? false : true;

                    result.Add(new MarketDTO
                    {
                        MarketTemplateId = market.MaTeId,
                        MarketId = market.MaId.ToString(),
                        MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                        Line = market.Li,
                        IsCashout = market.CaStId == 1 ? true : false,
                        MarketStatusId = market.MaStId,
                        MarketStatusName = market.MaStNa,
                        SortOrder = marketTemplateIds.Any(c => c == market.MaTeId) ? marketTemplateIds.FindIndex(c => c == market.MaTeId) + 1 : 1000 + Convert.ToInt32(market.MaTeId),
                        IsBetBuilder = isBetBuilder,
                        Selections = filteredSelections.Select(s => new SelectionDTO
                        {
                            SelectionTemplateId = s.SeTeId,
                            SelectionId = s.SeId.ToString(),
                            SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                            DecimalOdds = s.Od.ToString("0.00"),
                            MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                            HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                            IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                            IsActive = s.IsAc,
                            CorrelationId = s.CoId,
                            SortOrder = s.SeSoOr
                        }).OrderBy(o => o.SortOrder).ToList()
                    });
                }

                return result.OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
            }
        }
        private List<MarketDTO> GetOutrightMarkets(List<MarketFilter> markets, List<SelectionFilter> selections, LanguageCode languageCode, string clientCode, int sportId, int fixtureTypeId)
        {
            var result = new List<MarketDTO>();

            if (IsOEEnable(clientCode))
            {
                foreach (var market in markets)
                {
                    var selectionsToAdd = selections.Where(c => c.MaId == market.MaId).ToList();
                    var isBinary = selectionsToAdd.Count > 2 ? false : true;

                    if (selectionsToAdd.Count > 3)
                    {
                        result.Add(new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.MaStId,
                            MarketStatusName = market.MaStNa,
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            Selections = selectionsToAdd.Where(c => c.IsAc == true).Select(s => new SelectionDTO
                            {
                                SelectionTemplateId = s.SeNa.ToLower() == "yes" ? "1" : s.SeNa.ToLower() == "no" ? "2" : s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                                DecimalOdds = s.Od.ToString("0.00"),
                                MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                            }).OrderBy(o => Convert.ToDecimal(o.DecimalOdds)).ToList()
                        });
                    }
                    else
                    {
                        result.Add(new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.MaStId,
                            MarketStatusName = market.MaStNa,
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            Selections = selectionsToAdd.Select(s => new SelectionDTO
                            {
                                SelectionTemplateId = s.SeNa.ToLower() == "yes" ? "1" : s.SeNa.ToLower() == "no" ? "2" : s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                                DecimalOdds = s.Od.ToString("0.00"),
                                MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                                SortOrder = s.SeSoOr
                            }).OrderBy(o => o.SortOrder).ToList()
                        });
                    }
                }
            }
            else
            {
                foreach (var market in markets)
                {
                    var selectionsToAdd = selections.Where(c => c.MaId == market.MaId).ToList();
                    var isBinary = selectionsToAdd.Count > 2 ? false : true;

                    if (selectionsToAdd.Count > 3)
                    {
                        result.Add(new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.MaStId,
                            MarketStatusName = market.MaStNa,
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            Selections = selectionsToAdd.Select(s => new SelectionDTO
                            {
                                SelectionTemplateId = s.SeNa.ToLower() == "yes" ? "1" : s.SeNa.ToLower() == "no" ? "2" : s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                                DecimalOdds = s.Od.ToString("0.00"),
                                MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                            }).OrderBy(o => Convert.ToDecimal(o.DecimalOdds)).ToList()
                        });
                    }
                    else
                    {
                        result.Add(new MarketDTO
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId.ToString(),
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, languageCode),
                            Line = market.Li,
                            IsCashout = market.CaStId == 1 ? true : false,
                            MarketStatusId = market.MaStId,
                            MarketStatusName = market.MaStNa,
                            SortOrder = Convert.ToInt32(market.MaTeId),
                            Selections = selectionsToAdd.Select(s => new SelectionDTO
                            {
                                SelectionTemplateId = s.SeNa.ToLower() == "yes" ? "1" : s.SeNa.ToLower() == "no" ? "2" : s.SeTeId,
                                SelectionId = s.SeId.ToString(),
                                SelectionName = GetSelectionName(s.SeNa, s.SeNaTr, languageCode),
                                DecimalOdds = s.Od.ToString("0.00"),
                                MalaysianOdds = GetMalayOdds(s.Od, isBinary).ToString("0.00"),
                                HongkongOdds = GetHongkongOdds(s.Od, isBinary).ToString("0.00"),
                                IndonesianOdds = GetIndoOdds(s.Od, isBinary).ToString("0.00"),
                                IsActive = s.IsAc,
                                CorrelationId = s.CoId,
                                SortOrder = s.SeSoOr
                            }).OrderBy(o => o.SortOrder).ToList()
                        });
                    }
                }
            }

            return result.OrderBy(o => Convert.ToInt32(o.SortOrder)).ThenBy(o => o.Line).ThenBy(o => o.MarketName).ToList();
        }

        private BetSelectionState GetBetSelectionStatus(OddsChangeType oddsChangeType, BetFixtureSelectionRequestDTO current, Tuple<List<Fixture>, List<Market>, List<SelectionExtension>, BettingStatus, List<Selection>> result, string clientCode, bool isBetBuilder, int selectionCount)
        {

            var selection = result.Item3.Where(c => c.SeId == current.SelectionId).FirstOrDefault();
            var fixture = result.Item1.Where(c => c.FiId == selection.FiId).FirstOrDefault();
            var market = result.Item2.Where(c => c.MaId == selection.MaId).FirstOrDefault();
            var isBinary = selectionCount > 2 ? false : true;

            var bettingStatus = result.Item4;

            //if (isBoosted)
            //{
            //    string boostedOddsStr = GetClientBoostedOdds(selection.ClBoOd, clientCode);
            //    if (!string.IsNullOrEmpty(boostedOddsStr))
            //        selection.Od = Convert.ToDecimal(boostedOddsStr);
            //}


            if ((fixture.SpId == 1001 || fixture.SpId == 1002 || fixture.SpId == 1055 || fixture.SpId == 1058) && fixture.FiStDa < DateTime.UtcNow)
            {
                return new BetSelectionState
                {
                    SelectionId = current.SelectionId.ToString(),
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
                    StatusCode = StatusCode.Market_Is_Not_Offered,
                    Message = StatusCode.Market_Is_Not_Offered.ToString().Replace('_', ' ')
                };
            }

            if (bettingStatus != null && bettingStatus.IsBeSo == true)
            {
                return new BetSelectionState
                {
                    SelectionId = current.SelectionId.ToString(),
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
                    StatusCode = StatusCode.Betstop_On_Global,
                    Message = StatusCode.Betstop_On_Global.ToString().Replace('_', ' ')
                };
            }

            if (fixture == null || market == null || selection == null)
            {
                return new BetSelectionState
                {
                    SelectionId = current.SelectionId.ToString(),
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
                    StatusCode = StatusCode.Market_Is_Not_Offered,
                    Message = StatusCode.Market_Is_Not_Offered.ToString().Replace('_', ' ')
                };
            }

            //if (selection.FiId != current.FixtureId || selection.MaId != current.MarketId || selection.SeId != current.SelectionId || selection.SpId != current.SportId)
            //{
            //    return new BetSelectionState
            //    {
            //        SelectionId = selection.SeId.ToString(),
            //        Odds = selection.Od,
            //        CorrelationId = selection.CoId,
            //        StatusCode = StatusCode.Market_Is_Inactive,
            //        Message = StatusCode.Market_Is_Inactive.ToString().Replace('_', ' '),
            //    };
            //}

            if (selection.SeId != current.SelectionId || selection.SpId != current.SportId)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Is_Not_Offered,
                    Message = StatusCode.Market_Is_Not_Offered.ToString().Replace('_', ' '),
                };
            }

            if (fixture.IsSu == true && market.MaStId != (int)MarketStatus.Active)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Is_Not_Offered,
                    Message = StatusCode.Market_Is_Not_Offered.ToString().Replace('_', ' '),
                };
            }

            if (fixture.IsSu)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Has_Been_Suspended,
                    Message = StatusCode.Market_Has_Been_Suspended.ToString().Replace('_', ' '),
                };
            }

            if (market.MaStId == (int)MarketStatus.Suspended)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
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
                    DecimalOdds = current.Odds,
                    MalaysianOdds = GetMalayOdds(current.Odds, isBinary),
                    HongkongOdds = GetHongkongOdds(current.Odds, isBinary),
                    IndonesianOdds = GetIndoOdds(current.Odds, isBinary),
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Market_Is_Not_Offered,
                    Message = StatusCode.Market_Is_Not_Offered.ToString().Replace('_', ' ')
                };
            }

            if (selection.IsAc == false)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    DecimalOdds = selection.Od,
                    MalaysianOdds = selection.MaOd,
                    HongkongOdds = selection.HoOd,
                    IndonesianOdds = selection.InOd,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Odds_Is_Inactive,
                    Message = StatusCode.Odds_Is_Inactive.ToString().Replace('_', ' ')
                };
            }

            if (isBetBuilder == false && selection.Od <= 1m)
            {
                return new BetSelectionState
                {
                    SelectionId = selection.SeId.ToString(),
                    DecimalOdds = selection.Od,
                    MalaysianOdds = selection.MaOd,
                    HongkongOdds = selection.HoOd,
                    IndonesianOdds = selection.InOd,
                    CorrelationId = selection.CoId,
                    StatusCode = StatusCode.Odds_Must_Be_Greater_Or_Higher_To_1,
                    Message = StatusCode.Odds_Must_Be_Greater_Or_Higher_To_1.ToString().Replace('_', ' ')
                };
            }

            switch (oddsChangeType)
            {
                case OddsChangeType.None:
                    if (isBetBuilder == false && current.Odds != selection.Od)
                    {
                        return new BetSelectionState
                        {
                            SelectionId = selection.SeId.ToString(),
                            DecimalOdds = selection.Od,
                            MalaysianOdds = selection.MaOd,
                            HongkongOdds = selection.HoOd,
                            IndonesianOdds = selection.InOd,
                            CorrelationId = selection.CoId,
                            StatusCode = StatusCode.Odds_Or_Lines_Have_Changed,
                            Message = StatusCode.Odds_Or_Lines_Have_Changed.ToString().Replace('_', ' ')
                        };
                    }
                    break;
                case OddsChangeType.Any:
                    break;
                case OddsChangeType.Higher:
                    if (isBetBuilder == false && current.Odds > selection.Od)
                    {
                        return new BetSelectionState
                        {
                            SelectionId = selection.SeId.ToString(),
                            DecimalOdds = selection.Od,
                            MalaysianOdds = selection.MaOd,
                            HongkongOdds = selection.HoOd,
                            IndonesianOdds = selection.InOd,
                            CorrelationId = selection.CoId,
                            StatusCode = StatusCode.Odds_Or_Lines_Have_Changed,
                            Message = StatusCode.Odds_Or_Lines_Have_Changed.ToString().Replace('_', ' ')
                        };
                    }
                    break;
                default:
                    break;
            }

            return new BetSelectionState
            {
                SelectionId = selection.SeId.ToString(),
                DecimalOdds = selection.Od,
                MalaysianOdds = selection.MaOd,
                HongkongOdds = selection.HoOd,
                IndonesianOdds = selection.InOd,
                CorrelationId = selection.CoId,
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }

        private async Task<Tuple<decimal, StatusCode>> GetBetBuilderOddsStatus(OddsChangeType oddsChangeType, List<BetFixtureSelectionRequestDTO> selections, Tuple<List<Fixture>, List<Market>, List<SelectionExtension>, BettingStatus, List<Selection>> result, decimal currentTotalOdds)
        {

            if (result.Item1.Select(f => f.FiId).Distinct().Count() > 1)
                return new Tuple<decimal, StatusCode>(currentTotalOdds, StatusCode.Market_Is_Not_Offered);

            var oddsResult = await GetBetBuilderSelectionsOdds(new BetBuilderSelectionsOddsRequestDTO
            {
                Selections = selections.Select(s => new BetSelectionOddsRequest
                {
                    FixtureId = s.FixtureId.ToString(),
                    MarketId = s.MarketId.ToString(),
                    SelectionId = s.SelectionId.ToString(),
                    SportId = s.SportId,
                    CorrelationId = s.CorrelationId
                }).ToList(),
                LanguageCode = LanguageCode.EN
            });

            if (oddsResult.StatusCode != StatusCode.Success)
                return new Tuple<decimal, StatusCode>(oddsResult.Result.TotalOdds, StatusCode.Market_Is_Not_Offered);

            switch (oddsChangeType)
            {
                case OddsChangeType.None:
                    if (oddsResult.Result.TotalOdds != currentTotalOdds)
                        return new Tuple<decimal, StatusCode>(oddsResult.Result.TotalOdds, StatusCode.Odds_Or_Lines_Have_Changed);
                    break;
                case OddsChangeType.Any:
                    break;
                case OddsChangeType.Higher:
                    if (oddsResult.Result.TotalOdds > currentTotalOdds)
                        return new Tuple<decimal, StatusCode>(oddsResult.Result.TotalOdds, StatusCode.Odds_Or_Lines_Have_Changed);
                    break;
                default:
                    break;
            }

            return new Tuple<decimal, StatusCode>(oddsResult.Result.TotalOdds, StatusCode.Success);
        }

        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(ActiveFixturesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

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
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                FixtureType = s.FiTyNa,
                FixtureId = s.FiId.ToString(),
                ProviderFixtureId = s.PrFiId,
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                Home = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                Away = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStatus = s.FiTyId == (int)FixtureType.Outright || s.SpId > 1000 ? s.FiStNa : s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                FixtureStartDate = s.FiStDa,
                IsLive = s.SpId == 21 ? s.FiStId == (int)FixtureStatus.Live ? true : false : s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IPCountryCode),
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
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

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
                PrematchCount = s.Where(c => c.FiStId == (int)FixtureStatus.Prematch).Count(),
                LiveCount = s.Where(c => c.FiStId == (int)FixtureStatus.Live).Count(),
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
        public async Task<BaseResponseDTO<SportsLookupResponseDTO>> GetSportsLookup(string clientCode)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(clientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(clientCode) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<SportsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SportsLookupResponseDTO()
                };
            }

            var finalResult = new List<SportLookup>();

            foreach (var item in fixtures)
            {
                if (finalResult.Any(c => c.SportId == item.SpId)) continue;

                finalResult.Add(new SportLookup { SportId = item.SpId, SportName = item.SpNa });
            }

            return new BaseResponseDTO<SportsLookupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SportsLookupResponseDTO
                {
                    Sports = finalResult.OrderBy(o => o.SportId).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<SegmentsLookupResponseDTO>> GetSegmentsLookup(string clientCode)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(clientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(clientCode) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<SegmentsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SegmentsLookupResponseDTO()
                };
            }

            var finalResult = new List<SegmentLookup>();

            foreach (var item in fixtures)
            {
                if (finalResult.Any(c => c.SportId == item.SpId && c.SegmentId == item.SegId)) continue;

                finalResult.Add(new SegmentLookup
                {
                    SportId = item.SpId,
                    SportName = item.SpNa,
                    SegmentId = item.SegId,
                    SegmentName = item.SegNa
                });
            }

            return new BaseResponseDTO<SegmentsLookupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SegmentsLookupResponseDTO
                {
                    Segments = finalResult.OrderBy(o => o.SportId).ThenBy(o => o.SegmentId).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<LeaguesLookupResponseDTO>> GetLeaguesLookup(string clientCode)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(clientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(clientCode) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<LeaguesLookupResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new LeaguesLookupResponseDTO()
                };
            }

            var finalResult = new List<LeagueLookup>();

            foreach (var item in fixtures)
            {
                if (finalResult.Any(c => c.SportId == item.SpId && c.SegmentId == item.SegId && c.LeagueId == item.LeId)) continue;

                finalResult.Add(new LeagueLookup
                {
                    SportId = item.SpId,
                    SportName = item.SpNa,
                    SegmentId = item.SegId,
                    SegmentName = item.SegNa,
                    LeagueId = item.LeId,
                    LeagueName = item.LeNa
                });
            }

            return new BaseResponseDTO<LeaguesLookupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new LeaguesLookupResponseDTO
                {
                    Leagues = finalResult.OrderBy(o => o.SportId).ThenBy(o => o.SegmentId).ThenBy(o => o.LeagueId).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<BoostedResponseDTO>> GetBoostedFixtures(BoostedFixturesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 1 && c.IsBo == true && c.ClBo.Contains(request.ClientCode));
            var tempFixtures = fixtureCollection.Find(fixtureFilter).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var tempBettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(tempFixtures, tempBettingStatus);

            var fixtures = tempFixtures.Result;
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode, StringComparison.OrdinalIgnoreCase) || f.SpId > 1000 || f.FiTyId == (int)FixtureType.Outright).ToList();
            var bettingStatus = tempBettingStatus.Result == null ? new BettingStatus { IsBeSo = false } : tempBettingStatus.Result;

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<BoostedResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BoostedResponseDTO()
                };
            }

            var fixtureIds = fixtures.Select(s => Convert.ToInt64(s.FiId)).ToList();

            var marketStatusIds = new List<int> { (int)MarketStatus.Active };

            var marketCountFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && c.ClBo.Contains(request.ClientCode));
            var marketCountProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaId);
            var marketCount = marketCollection.Find(marketCountFilter).Project<MarketCount>(marketCountProjection).ToListAsync();

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && c.ClBo.Contains(request.ClientCode));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.IsBo).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.ClBoOd).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString()).Include(c => c.ClBoSeNa).Include(c => c.ClBoSeNaTr).ElemMatch(c => c.ClBoSeNaTr, cc => cc.ClCoLaCo == $"{request.ClientCode}:{request.LanguageCode.ToString()}");
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            await Task.WhenAll(marketCount, markets, selections);


            var clientInitial = GetClientMarketTemplateGroups();

            var result = fixtures.Select(s => new BoostedFixtureDTO
            {
                SportId = s.SpId,
                SportName = GetSportName(s.SpNa, s.SpNaTr, request.LanguageCode),
                SegmentId = s.SegId,
                SegmentName = GetSegmentName(s.SegNa, s.SegNaTr, request.LanguageCode),
                LeagueId = s.LeId,
                LeagueName = GetLeagueName(s.LeNa, s.LeNaTr, request.LanguageCode),
                ProviderFixtureId = s.PrFiId,
                FixtureId = s.FiId.ToString(),
                FixtureName = GetFixtureName(s.FiNa, s.FiNaTr, request.LanguageCode),
                HomeName = GetHomeName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                AwayName = GetAwayName(s.FiNa, s.FiNaTr, request.LanguageCode, (FixtureType)s.FiTyId),
                FixtureStartDate = s.FiStDa,
                IsLive = s.IsOfLi,
                IsLiveStream = IsLiveStream(s.LiId, s.GeAl, request.IPCountryCode),
                FixtureStatusId = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStId : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStId : (int)FixtureStatus.Closed,
                FixtureStatusName = s.SpId > 1000 || s.FiTyId == (int)FixtureType.Outright ? s.FiStNa : !IsOEEnable(request.ClientCode) || s.ClCo.Contains(request.ClientCode) ? s.FiStNa : FixtureStatus.Closed.ToString(),
                MarketCount = marketCount.Result.Where(c => c.FiId == s.FiId).Count(),
                SMSCode = string.Empty,
                StatisticsURL = GetStatisticsURL(request.ClientId, s.StUr, request.LanguageCode),
                Score = s.Sc,
                CurrentPeriod = CurrentPeriodHelper.ToShortName(s.CuPe),
                CurrentMinute = s.CuMi.ToString(),
                BetStop = s.IsSu ? new List<int> { 1 } : new List<int> { },
                IsBetStopGlobal = bettingStatus.IsBeSo,
                IsBoosted = s.IsBo,
                Markets = GetBoostedMarkets(s.SpId, s.FiTyId, markets.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), selections.Result.Where(c => c.FiId == Convert.ToInt64(s.FiId)).ToList(), request.LanguageCode, request.ClientCode, true, s.FiStId, clientInitial.Where(c => c.SportId == s.SpId && c.ClientId == request.ClientId).ToList())
            }).ToList();

            await _rabbitMQRepository.PublishToRabbitMQAPI(new MessageRequestDTO
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageType = "OnVisitBoostedFixtures",
                RoutingKey = $"r.oe.sb.engine.onvisitboostedfixtures.{request.ClientCode.ToLower()}",
                Payload = JsonSerializer.Serialize(new
                {
                    FixtureIds = fixtureIds
                })
            });

            var sportOrder = GetSportOrder().Where(c => c.ClientId == request.ClientId).ToList();
            var activeSportIds = sportOrder.Select(s => s.SportId).ToList();

            return new BaseResponseDTO<BoostedResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BoostedResponseDTO
                {
                    Fixtures = result.Where(m => activeSportIds.Contains(m.SportId) && m.Markets.Any()).OrderBy(o => sportOrder.Where(so => so.SportId == o.SportId).FirstOrDefault().SortOrder).ThenBy(o => o.FixtureStartDate).ThenBy(o => o.FixtureName).ToList()
                }
            };
        }

        public async Task<BaseResponseDTO<BetBuilderMarketTemplatesDTO>> GetBetBuilderMarketTemplatesDTO()
        {
            var result = GetBetBuilderMarketTemplates();
            var finalResult = new BetBuilderMarketTemplatesDTO();

            foreach (var betBuilder in result)
            {
                finalResult.BetBuilderMarketTemplates.Add(new BetBuilderMarketTemplateDTO
                {
                    SportId = betBuilder.SportId,
                    MarketTemplateId = betBuilder.MarketTemplateId,
                    Live = betBuilder.Live,
                    Prematch = betBuilder.Prematch
                });
            }

            return new BaseResponseDTO<BetBuilderMarketTemplatesDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = finalResult
            };
        }

        public async Task<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>> GetBetBuilderSelectionsOdds(BetBuilderSelectionsOddsRequestDTO request)
        {
            var fixtureIds = request.Selections.Select(s => Convert.ToInt64(s.FixtureId)).Distinct().ToList();

            if (fixtureIds.Count > 1)
            {
                return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                {
                    StatusCode = StatusCode.No_Record_Found,
                    Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                };
            }

            var selectionIds = request.Selections.Select(s => Convert.ToInt64(s.SelectionId)).Distinct().ToList();

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => fixtureIds.Contains(c.FiId));
            var fixtureProjection = Builders<Fixture>.Projection.Include(c => c.FiId).Include(c => c.IsSu).Include(c => c.FiStId).Include(c => c.FiSp).Include(c => c.SpId);
            var fixture = primaryFixtureCollection.Find(fixtureFilter).Project<FixtureBetBuilderFilter>(fixtureProjection).FirstOrDefaultAsync();

            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.SeId).Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = primarySelectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var bettingStatus = primaryBettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(fixture, markets, selections, bettingStatus);

            var marketIds = markets.Result.Select(s => Convert.ToInt64(s.MaId)).Distinct().ToList();
            var marketMappingFilter = Builders<MarketMapping>.Filter.Where(c => marketIds.Contains(c.MaId));
            var marketMappingProjection = Builders<MarketMapping>.Projection.Include(c => c.PrFiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaSp);
            var marketMappings = await marketMappingCollection.Find(marketMappingFilter).Project<MarketMappingFilter>(marketMappingProjection).ToListAsync();

            var newFixture = fixture.Result;
            var newMarkets = markets.Result;
            var newMarketMappings = marketMappings;
            var newSelections = selections.Result;
            var newBettingStatus = bettingStatus.Result;

            if (newSelections.Count == 0)
            {
                return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetBuilderSelectionsOddsResponseDTO()
                };
            }

            var selectionOddsResponse = new BetBuilderSelectionsOddsResponseDTO();
            selectionOddsResponse.FixtureId = newFixture.FiId.ToString();
            selectionOddsResponse.TotalOdds = 0.0m;

            #region SR API to calculate odds - Filtered
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.betradar.com");
                client.DefaultRequestHeaders.Add("x-access-token", "wLCaM7vc80CADU33rz");

                var payload = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
                if (request.Selections.Any())
                {

                    payload = payload + $"<filterSelections xmlns=\"http://schemas.sportradar.com/custombet/v1/endpoints\">";
                    payload = payload + $"<selection id=\"{newFixture.FiSp}\">";

                    foreach (var sel in request.Selections)
                    {
                        var marketMapping = marketMappings.FirstOrDefault(f => f.MaId == Convert.ToInt64(sel.MarketId));
                        var selection = selections.Result.FirstOrDefault(s => s.SeId == Convert.ToInt64(sel.SelectionId));

                        var marketSpecifier = marketMapping.MaSp;

                        if (string.IsNullOrEmpty(marketSpecifier))
                            payload = payload + $"<market market_id=\"{marketMapping.MaTeId}\" outcome_id=\"{selection.SeTeId}\"/>";
                        else
                            payload = payload + $"<market market_id=\"{marketMapping.MaTeId}\" specifiers=\"{marketSpecifier}\" outcome_id=\"{selection.SeTeId}\"/>";
                    }

                    payload = payload + $"</selection>";
                    payload = payload + $"</filterSelections>";
                }

                var response = await client.PostAsync($"/v1/custombet/calculate-filter", new StringContent(payload, System.Text.Encoding.UTF8, "application/xml"));
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    XmlDocument doc = new XmlDocument();

                    doc.LoadXml(result);

                    #region Validate response

                    XmlElement root = doc.DocumentElement;

                    if (root.Name != "filtered_calculation_response")
                    {
                        return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                        {
                            StatusCode = StatusCode.No_Record_Found,
                            Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                        };
                    }

                    foreach (XmlElement node in root.ChildNodes)
                    {
                        if (node.Name == "calculation")
                        {
                            selectionOddsResponse.TotalOdds = Convert.ToDecimal(node.Attributes["odds"].Value);
                        }
                        else if (node.Name == "available_selections")
                        {
                            var eventNode = node.ChildNodes[0];

                            if (eventNode.ChildNodes.Count == 0)
                                continue;

                            var marketNodes = eventNode.ChildNodes[0];

                            var marketResponse = GetBetBuilderMarketsXmlFiltered(marketNodes.ChildNodes, newMarketMappings, newMarkets, newSelections, request.LanguageCode);
                            if (marketResponse.Any())
                                selectionOddsResponse.Markets.AddRange(marketResponse);
                        }
                    }


                    #endregion

                    return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = selectionOddsResponse
                    };
                }
                else
                {
                    return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                    {
                        StatusCode = StatusCode.No_Record_Found,
                        Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                    };
                }
            }

            #endregion
        }


        private List<string> GetMainMarketTemplates(int sportId)
        {
            var result = GetSelectorMarketTemplate().Where(c => c.SportId == sportId).ToList();

            if (result == null || result.Count == 0)
                return new List<string>();

            return result.Select(s => s.MarketTemplateId).ToList();
        }
        private List<SelectorMarketTemplate> GetSelectorMarketTemplate()
        {
            return _memCache.GetCache($"smt", () => _sportsbookContext.SelectorMarketTemplate.ToList(), 24);
        }
        private Tuple<DateTime, DateTime> GetDateTimeRange(DateTime utcDate, int minuteUTCOffset)
        {
            var hours = minuteUTCOffset / 60;
            var remainderMinutes = minuteUTCOffset % 60;

            if (hours >= 0)
            {
                var utc = utcDate.Date;
                var startTime = utc.AddHours(hours * -1).AddMinutes(remainderMinutes * -1).AddMilliseconds(-1);
                var endTime = startTime.AddDays(1).AddMilliseconds(1);

                if (endTime < utcDate)
                {
                    startTime = startTime.AddDays(1);
                    endTime = endTime.AddDays(1);
                }

                return new Tuple<DateTime, DateTime>(startTime, endTime);
            }
            else
            {
                var utc = utcDate.Date;
                var startTime = utc.AddHours(hours * -1).AddMinutes(remainderMinutes * -1).AddMilliseconds(-1);
                var endTime = startTime.AddDays(1).AddMilliseconds(1);

                return new Tuple<DateTime, DateTime>(startTime, endTime);
            }
        }
        private List<TierLeagueOrder> GetTierLeagueOrder()
        {
            return _memCache.GetCache($"tlo", () => _sportsbookContext.TierLeagueOrder.ToList(), 24);
        }
        private List<Shared.Data.Sportsbook.SQLEntities.MarketTemplateGroup> GetMarketTemplateGroup()
        {
            return _memCache.GetCache($"mtg2", () => _sportsbookContext.MarketTemplateGroup.ToList(), 24);
        }
        private List<ClientMarketTemplateGroup> GetClientMarketTemplateGroups()
        {
            return _memCache.GetCache($"cmtg", () => _sportsbookContext.ClientMarketTemplateGroup.ToList(), 24);
        }
        public async Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>> GetClientMarketSortOrder(MarketSortOrderRequestDTO request)
        {
            var initialResult = GetClientMarketTemplateGroups();

            var result = initialResult.Where(c => c.ClientId == request.ClientId && c.Id == 1).Select(s => new ClientMarketTemplateGroupDTO
            {
                FixtureStatusId = s.FixtureStatusId,
                SportId = s.SportId,
                MarketTemplateIds = s.MarketTemplates
            }).ToList();

            return new BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientMarketTemplateGroupResponseDTO2
                {
                    MarketTemplateGroups = result
                }
            };
        }
        public async Task<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>> GetClientFixtureSortOrder(FixtureSortOrderRequestDTO request)
        {
            var initialResult = GetTierLeagueOrder();

            var result = initialResult.Select(s => new ClientFixtureSortOrderDTO
            {
                SportId = s.SportId,
                SegmentId = s.SegmentId,
                LeagueId = s.LeagueId,
                SortOrder = s.SortOrder
            }).ToList();

            return new BaseResponseDTO<ClientFixtureSortOrderResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientFixtureSortOrderResponseDTO
                {
                    Fixtures = result
                }
            };
        }
        private List<ClientSportOrder> GetSportOrder()
        {
            return _memCache.GetCache($"so", () => _sportsbookContext.ClientSportOrder.ToList(), 24);
        }
        private List<ClientSegmentOrder> GetSegmentOrder()
        {
            return _memCache.GetCache($"seo", () => _sportsbookContext.ClientSegmentOrder.ToList(), 24);
        }
        private List<ClientLeagueOrder> GetLeagueOrder()
        {
            return _memCache.GetCache($"leo", () => _sportsbookContext.ClientLeagueOrder.ToList(), 24);
        }
        private Shared.Data.Sportsbook.SQLEntities.SportGroup GetSportGroups(Shared.Enums.SportGroup sportGroup)
        {
            var result = _memCache.GetCache($"sg", () => _sportsbookContext.SportGroup.ToList(), 24);

            return result == null ? null : result.Where(c => c.SportGroupId == (int)sportGroup).FirstOrDefault();
        }
        public async Task<BaseResponseDTO<StatusCode>> ManualSettlement(BetSettlementRequestDTO request)
        {

            if (request.Selections.Count > 0)
            {
                string manualSettlementConfig = string.IsNullOrEmpty(_configuration["Custom:ManualSettlement"]) ? "Old" : _configuration["Custom:ManualSettlement"];
                if (manualSettlementConfig.ToUpper() == "NEW")
                {
                    var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiId == long.Parse(request.FixtureId));
                    var fixture = await fixtureCollection.Find(fixtureFilter).FirstOrDefaultAsync();

                    if (fixture == null)
                    {
                        return new BaseResponseDTO<StatusCode>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString()
                        };
                    }

                    //var marketTemplateIds = GetMainMarketTemplates(fixture.SpId);

                    var marketFilter = Builders<Market>.Filter.Where(c => c.FiId == long.Parse(request.FixtureId));
                    var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa);
                    var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

                    var marketMappingFilter = Builders<MarketMapping>.Filter.Where(c => c.PrFiId == fixture.FiSp);
                    var marketMappingProjection = Builders<MarketMapping>.Projection.Include(c => c.PrFiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaSp);
                    var marketMappings = marketMappingCollection.Find(marketMappingFilter).Project<MarketMappingFilter>(marketMappingProjection).ToListAsync();

                    var selectionIds = request.Selections.Select(s => long.Parse(s.SelectionId)).ToList();
                    var selectionFilter = Builders<Selection>.Filter.Where(c => c.FiId == long.Parse(request.FixtureId) && selectionIds.Contains(c.SeId));
                    var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId);
                    var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

                    await Task.WhenAll(markets, marketMappings, selections);

                    var selectionPayload = new List<SelectionSettlementMessage>();

                    foreach (var selection in selections.Result)
                    {
                        var requestSelection = request.Selections.Where(s => long.Parse(s.SelectionId) == selection.SeId).FirstOrDefault();

                        if (requestSelection == null)
                            continue;

                        selectionPayload.Add(new SelectionSettlementMessage
                        {
                            MarketTemplateId = markets.Result.Where(s => s.MaId == selection.MaId).Select(ss => ss.MaTeId).FirstOrDefault(),
                            SelectionTemplateId = selection.SeTeId,
                            MarketSpecifier = marketMappings.Result.Where(s => s.MaId == selection.MaId).Select(ss => ss.MaSp).FirstOrDefault(),
                            Result = GetResult(requestSelection.SelectionStatus),
                            VoidFactor = GetVoidFactor(requestSelection.SelectionStatus),
                            DeadHeatFactor = requestSelection.DeadHeatFactor
                        });
                    }

                    var payload = new BetSettlementRequestPayloadDTO
                    {
                        SportId = fixture.SpId,
                        ProviderFixtureId = fixture.PrFiId,
                        FixtureSpecifier = fixture.FiSp,
                        HomeScore = string.Empty,
                        AwayScore = string.Empty,
                        Selections = selectionPayload
                    };

                    await _rabbitMQRepository.PublishToRabbitMQAPI(new MessageRequestDTO()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "OnBetSettlement",
                        RoutingKey = $"r.uof.onbetsettlement",
                        Payload = JsonSerializer.Serialize(payload)
                    });
                }
                else
                {
                    var digit = Convert.ToString(request.FixtureId);
                    var lastDigit = digit.Substring(digit.Length - 1);

                    var payload = new
                    {
                        FixtureId = long.Parse(request.FixtureId),
                        Selections = request.Selections.Select(s =>
                            new
                            {
                                MarketId = long.Parse(s.MarketId),
                                SelectionId = long.Parse(s.SelectionId),
                                SettlementStatus = GetSettlementStatus(s.SelectionStatus),
                                DeadHeatFactor = s.DeadHeatFactor
                            }).ToList()
                    };

                    await _rabbitMQRepository.PublishToRabbitMQAPI(new MessageRequestDTO()
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        MessageType = "OnBetSettlement",
                        RoutingKey = $"r.sb.adapter.{lastDigit}.onbetsettlement",
                        Payload = JsonSerializer.Serialize(payload)
                    });

                }

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

                //TODO: call another API to sends to rabbitmq
                //_messageBroker.Publish(payload, "OnBetCancellation", $"r.sb.adapter.{lastDigit}.onbetcancellation", Guid.NewGuid().ToString());
            }

            return new BaseResponseDTO<StatusCode>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }

        public async Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(ClientTopFixtureRequestDTO request)
        {
            var topFixtures = _sportsbookContext.ClientTopFixture
                .Where(c => request.ClientIds.Contains(c.ClientId))
                .OrderBy(o => o.ClientId).ThenBy(o => o.SortOrder).ToList();

            var fixtureIds = topFixtures.Select(m => m.FixtureId).Distinct().ToList();

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => fixtureIds.Contains(c.FiId));
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (request.ClientCodes != null && request.ClientCodes.Any())
            {
                request.ClientCodes.ForEach(
                    c =>
                    {
                        if (IsOEEnable(c))
                            fixtures = fixtures.Where(f => f.ClCo.Contains(c)).ToList();
                    });
            }

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<ClientTopFixtureResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientTopFixtureResponseDTO()
                };
            }

            //var clientIds = topFixtures.Select(s => s.ClientId).Distinct().ToList();
            var clientIds = request.ClientIds.Distinct().ToList();

            var result = new List<OrderedClientTopFixture>();

            foreach (var clientId in clientIds)
            {
                var currentTopFixtures = topFixtures.Where(c => c.ClientId == clientId).ToList();
                var fixtureResult = new List<OrderedTopFixture>();

                foreach (var topfixture in currentTopFixtures)
                {
                    var fixture = fixtures.FirstOrDefault(f => f.FiId == topfixture.FixtureId);

                    if (fixture != null)
                    {
                        fixtureResult.Add(new OrderedTopFixture
                        {
                            SportId = fixture.SpId,
                            SportName = fixture.SpNa,
                            SegmentId = fixture.SegId,
                            SegmentName = fixture.SegNa,
                            LeagueId = fixture.LeId,
                            LeagueName = fixture.LeNa,
                            FixtureId = fixture.FiId.ToString(),
                            FixtureName = fixture.FiNa,
                            FixtureStartDate = fixture.FiStDa,
                            FixtureStatus = fixture.FiStNa,
                            SortOrder = topfixture.SortOrder
                        });
                    }
                }

                result.Add(new OrderedClientTopFixture
                {
                    ClientId = clientId,
                    TopFixtures = fixtureResult.OrderBy(o => o.SortOrder).ToList()
                });
            }

            return new BaseResponseDTO<ClientTopFixtureResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientTopFixtureResponseDTO()
                {
                    TopFixtures = result
                }
            };
        }

        public async Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures2(ClientTopFixtureRequestDTO2 request)
        {
            var result = new List<OrderedClientTopFixture>();

            if (request.ClientInfos != null && request.ClientInfos.Any())
            {
                var clientIds = request.ClientInfos.Select(s => s.ClientId).ToList();

                var topFixtures = _sportsbookContext.ClientTopFixture
                    .Where(c => clientIds.Contains(c.ClientId))
                    .OrderBy(o => o.ClientId).ThenBy(o => o.SortOrder).ToList();

                var fixtureIds = topFixtures.Select(m => m.FixtureId).Distinct().ToList();

                var fixtureFilter = Builders<Fixture>.Filter.Where(c => fixtureIds.Contains(c.FiId));
                var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();

                if (fixtures.Count == 0)
                {
                    return new BaseResponseDTO<ClientTopFixtureResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ClientTopFixtureResponseDTO()
                    };
                }

                var clientFixtures = request.ClientInfos.Select(s => new
                {
                    ClientId = s.ClientId,
                    ClientCode = s.ClientCode,
                    Fixtures = IsOEEnable(s.ClientCode) ? fixtures.Where(c => c.ClCo.Contains(s.ClientCode)).ToList() : fixtures
                }).ToList();

                foreach (var clientFixture in clientFixtures)
                {
                    var currentTopFixtures = topFixtures.Where(c => c.ClientId == clientFixture.ClientId).ToList();
                    var fixtureResult = new List<OrderedTopFixture>();

                    foreach (var topfixture in currentTopFixtures)
                    {
                        var fixture = fixtures.FirstOrDefault(f => f.FiId == topfixture.FixtureId);

                        if (fixture != null)
                        {
                            fixtureResult.Add(new OrderedTopFixture
                            {
                                SportId = fixture.SpId,
                                SportName = fixture.SpNa,
                                SegmentId = fixture.SegId,
                                SegmentName = fixture.SegNa,
                                LeagueId = fixture.LeId,
                                LeagueName = fixture.LeNa,
                                FixtureId = fixture.FiId.ToString(),
                                FixtureName = fixture.FiNa,
                                FixtureStartDate = fixture.FiStDa,
                                FixtureStatus = fixture.FiStNa,
                                SortOrder = topfixture.SortOrder
                            });
                        }
                    }

                    result.Add(new OrderedClientTopFixture
                    {
                        ClientId = clientFixture.ClientId,
                        TopFixtures = fixtureResult.OrderBy(o => o.SortOrder).ToList()
                    });
                }
            }

            return new BaseResponseDTO<ClientTopFixtureResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientTopFixtureResponseDTO()
                {
                    TopFixtures = result
                }
            };
        }


        public async Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(BackofficeSportsRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => (c.FiStId == (int)FixtureStatus.Prematch || c.FiStId == (int)FixtureStatus.Live) && c.SpId < 1000);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (IsOEEnable(request.ClientCode))
                fixtures = fixtures.Where(f => f.ClCo.Contains(request.ClientCode)).ToList();

            if (fixtures.Count == 0)
            {
                return new BaseResponseDTO<BackofficeSportsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BackofficeSportsResponseDTO()
                };
            }

            var sportIds = fixtures.Select(s => s.SpId).Distinct().ToList();

            var leagueSortOrder = GetTierLeagueOrder().Where(c => sportIds.Contains(c.SportId)).ToList();
            var sportSortOrder = GetSportOrder().Where(c => c.ClientId == request.ClientId).ToList();

            var fixtureResult = fixtures.Where(c => c.FiTyId == (int)FixtureType.Match)
                .Select(s => new TempOrderedFixture
                {
                    SportId = s.SpId,
                    SportName = s.SpNa,
                    SegmentId = s.SegId,
                    SegmentName = s.SegNa,
                    LeagueId = s.LeId,
                    LeagueName = s.LeNa,
                    FixtureId = s.FiId,
                    FixtureName = s.FiNa,
                    StartDate = s.FiStDa,
                    FixtureStatus = s.FiStNa,
                    SortOrder = leagueSortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : leagueSortOrder.Where(c => c.SportId == s.SpId && c.SegmentId == s.SegId && c.LeagueId == s.LeId).Select(s => s.SortOrder).FirstOrDefault()
                }).ToList();

            var sportResult = fixtures.Select(s => new TempDistinctOrderedSport
            {
                SportId = s.SpId,
                SportName = s.SpNa,
                HasOutrights = fixtures.Any(c => c.SpId == s.SpId && c.FiTyId == (int)FixtureType.Outright),
                SortOrder = sportSortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
                    == 0 ? 1000 : sportSortOrder.Where(c => c.SportId == s.SpId).Select(s => s.SortOrder).FirstOrDefault()
            }).Distinct().OrderBy(o => o.SortOrder).ThenBy(o => o.SportName).ToList();

            var finalResult = sportResult.Select(s => new OrderedSport
            {
                SportId = s.SportId,
                SportName = s.SportName
            }).ToList();

            var final = new List<BackofficeSportDTO>();

            foreach (var sport in finalResult)
            {
                final.Add(new BackofficeSportDTO
                {
                    SportId = sport.SportId,
                    SportName = sport.SportName,
                    Segments = fixtureResult.Where(c => c.SportId == sport.SportId).GroupBy(gse => gse.SegmentId).Select(s => new BackofficeSegmentDTO
                    {
                        SegmentId = s.Key,
                        SegmentName = s.Select(se => se.SegmentName).FirstOrDefault(),
                        SortOrder = s.Select(se => se.SortOrder).FirstOrDefault(),
                        Leagues = s.GroupBy(gle => gle.LeagueId).Select(l => new BackofficeLeagueDTO
                        {
                            LeagueId = l.Key,
                            LeagueName = l.Select(le => le.LeagueName).FirstOrDefault(),
                            SortOrder = l.Select(le => le.SortOrder).FirstOrDefault(),
                            Fixtures = l.Select(f => new BackofficeFixtureDTO
                            {
                                SportId = f.SportId,
                                SportName = f.SportName,
                                SegmentId = f.SegmentId,
                                SegmentName = f.SegmentName,
                                LeagueId = f.LeagueId,
                                LeagueName = f.LeagueName,
                                FixtureId = f.FixtureId.ToString(),
                                FixtureName = f.FixtureName,
                                StartDate = f.StartDate,
                                FixtureStatus = f.FixtureStatus,
                                SortOrder = f.SortOrder
                            }).OrderBy(o => o.SortOrder).ThenBy(o => o.FixtureName).ToList()
                        }).OrderBy(o => o.SortOrder).ThenBy(o => o.LeagueName).ToList()
                    }).OrderBy(o => o.SortOrder).ThenBy(o => o.SegmentName).ToList()
                });
            }

            return new BaseResponseDTO<BackofficeSportsResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BackofficeSportsResponseDTO
                {
                    Sports = final
                }
            };
        }

        public async Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(PostClientTopFixtureRequestDTO request)
        {
            // validate requests

            // check if fixtures requests are duplicate
            var duplicateFixtures = request.TopFixtures.GroupBy(g => g.FixtureId).Where(c => c.Count() > 1).ToList();

            if (duplicateFixtures.Count > 0)
            {
                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientTopFixtureMessageResponseDTO()
                    {
                        StatusCode = StatusCode.Cannot_Have_Duplicate_Top_Fixture_Entries,
                        Message = StatusCode.Cannot_Have_Duplicate_Top_Fixture_Entries.ToString().Replace('_', ' ')
                    }
                };
            }

            // check if sort order requests are duplicate
            var duplicateSortOrders = request.TopFixtures.GroupBy(g => g.SortOrder).Where(c => c.Count() > 1).ToList();

            if (duplicateSortOrders.Count > 0)
            {
                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientTopFixtureMessageResponseDTO()
                    {
                        StatusCode = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries,
                        Message = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries.ToString().Replace('_', ' ')
                    }
                };
            }

            if (request.TopFixtures.Count > 50)
            {
                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientTopFixtureMessageResponseDTO()
                    {
                        StatusCode = StatusCode.Top_Fixture_Entries_Must_Be_Equal_Or_Lower_Than_50,
                        Message = StatusCode.Top_Fixture_Entries_Must_Be_Equal_Or_Lower_Than_50.ToString().ToString().Replace('_', ' ')
                    }
                };
            }

            // delete existing records
            var currentTopFixtures = _sportsbookContext.ClientTopFixture.Where(c => c.ClientId == request.ClientId).ToList();

            if (currentTopFixtures.Count > 0)
            {
                _sportsbookContext.ClientTopFixture.RemoveRange(currentTopFixtures);
            }

            // insert new records
            var newTopFixtures = request.TopFixtures.Select(s => new ClientTopFixture
            {
                ClientId = request.ClientId,
                FixtureId = s.FixtureId,
                SortOrder = s.SortOrder
            });

            _sportsbookContext.ClientTopFixture.AddRange(newTopFixtures);

            await _sportsbookContext.SaveChangesAsync();

            return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }

        public async Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> CopyClientTopFixtures(CopyClientTopFixtureRequestDTO request)
        {
            try
            {
                // check if fixtures requests are duplicate
                var duplicateFixtures = request.TopFixtures.GroupBy(g => g.FixtureId).Where(c => c.Count() > 1).ToList();

                if (duplicateFixtures.Count > 0)
                {
                    return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Result = new ClientTopFixtureMessageResponseDTO()
                        {
                            StatusCode = StatusCode.Cannot_Have_Duplicate_Top_Fixture_Entries,
                            Message = StatusCode.Cannot_Have_Duplicate_Top_Fixture_Entries.ToString().Replace('_', ' ')
                        }
                    };
                }

                // check if sort order requests are duplicate
                var duplicateSortOrders = request.TopFixtures.GroupBy(g => g.SortOrder).Where(c => c.Count() > 1).ToList();

                if (duplicateSortOrders.Count > 0)
                {
                    return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Result = new ClientTopFixtureMessageResponseDTO()
                        {
                            StatusCode = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries,
                            Message = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries.ToString().Replace('_', ' ')
                        }
                    };
                }

                if (request.TopFixtures.Count > 50)
                {
                    return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Result = new ClientTopFixtureMessageResponseDTO()
                        {
                            StatusCode = StatusCode.Top_Fixture_Entries_Must_Be_Equal_Or_Lower_Than_50,
                            Message = StatusCode.Top_Fixture_Entries_Must_Be_Equal_Or_Lower_Than_50.ToString().ToString().Replace('_', ' ')
                        }
                    };
                }

                foreach (var clientId in request.ClientIds)
                {
                    // delete existing records
                    var currentTopFixtures = _sportsbookContext.ClientTopFixture.Where(c => c.ClientId == clientId).ToList();

                    if (currentTopFixtures.Count > 0)
                    {
                        _sportsbookContext.ClientTopFixture.RemoveRange(currentTopFixtures);
                    }

                    // insert new records
                    var newTopFixtures = request.TopFixtures.Select(s => new ClientTopFixture
                    {
                        ClientId = clientId,
                        FixtureId = s.FixtureId,
                        SortOrder = s.SortOrder
                    });

                    _sportsbookContext.ClientTopFixture.AddRange(newTopFixtures);
                }

                await _sportsbookContext.SaveChangesAsync();

                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString()
                };
            }

        }

        public async Task<BaseResponseDTO<ClientTopLeaguesResponseDTO>> GetClientIdsTopLeagues(ClientTopLeaguesRequestDTO request)
        {
            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 2 && c.FiTyId == (int)FixtureType.Match && c.SpId == request.SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();
            if (request.ClientCodes != null && request.ClientCodes.Any())
            {
                request.ClientCodes.ForEach(
                    c =>
                    {
                        if (IsOEEnable(c))
                            fixtures = fixtures.Where(f => f.ClCo.Contains(c) || f.SpId > 1000).ToList();
                    });
            }

            var finalResult = new List<ClientTopLeagueDTO>();

            foreach (var clientId in request.ClientIds)
            {
                var clientTopLeague = new ClientTopLeagueDTO()
                {
                    ClientId = clientId,
                    TopLeagues = new List<SportLeague>()
                };

                var sportLeague = _sportsbookContext.ClientTopLeague
                    .Where(c => c.ClientId == clientId && c.SportId == request.SportId)
                    .OrderBy(c => c.SortOrder).ToList();

                if (request.SegmentId > 0)
                {
                    sportLeague = sportLeague.Where(c => c.SegmentId == request.SegmentId).ToList();
                }

                if (request.LeagueId > 0)
                {
                    sportLeague = sportLeague.Where(c => c.LeagueId == request.LeagueId).ToList();
                }

                foreach (var item in sportLeague)
                {
                    var sportLeagueItem = new SportLeague()
                    {
                        LeagueId = item.LeagueId,
                        LeagueName = item.LeagueName,
                        SegmentId = item.SegmentId,
                        SegmentName = item.SegmentName,
                        SportId = item.SportId,
                        SportName = item.SportName,
                        SortOrder = item.SortOrder,
                        Status = fixtures.Where(f => f.SegId == item.SegmentId && f.LeId == item.LeagueId && f.SpId == item.SportId).Count() > 0 ? true : false
                    };
                    clientTopLeague.TopLeagues.Add(sportLeagueItem);
                }

                finalResult.Add(clientTopLeague);
            }

            return new BaseResponseDTO<ClientTopLeaguesResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientTopLeaguesResponseDTO
                {
                    ClientTopLeagues = finalResult
                }
            };
        }

        public async Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(ClientTopLeagueDTO request)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                // validate requests

                // check if league entries are duplicate
                var duplicateLeagues = request.TopLeagues.GroupBy(g => g.LeagueId).Where(c => c.Count() > 1).ToList();

                if (duplicateLeagues.Count > 0)
                {
                    return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Result = new ClientTopLeaguesMessageResponseDTO()
                        {
                            StatusCode = StatusCode.Cannot_Have_Duplicate_Top_League_Entries,
                            Message = StatusCode.Cannot_Have_Duplicate_Top_League_Entries.ToString().Replace('_', ' ')
                        }
                    };
                }

                // check if sort order requests are duplicate
                var duplicateSortOrders = request.TopLeagues.GroupBy(g => g.SortOrder).Where(c => c.Count() > 1).ToList();

                if (duplicateSortOrders.Count > 0)
                {
                    return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Result = new ClientTopLeaguesMessageResponseDTO()
                        {
                            StatusCode = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries,
                            Message = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries.ToString().Replace('_', ' ')
                        }
                    };
                }

                if (request.TopLeagues.Count > 50)
                {
                    return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Result = new ClientTopLeaguesMessageResponseDTO()
                        {
                            StatusCode = StatusCode.Top_League_Entries_Must_Be_Equal_Or_Lower_Than_50,
                            Message = StatusCode.Top_League_Entries_Must_Be_Equal_Or_Lower_Than_50.ToString().Replace('_', ' ')
                        }
                    };
                }

                // delete existing records
                var currentTopLeagues = _sportsbookContext.ClientTopLeague.Where(c => c.ClientId == request.ClientId && c.SportId == request.SportId).ToList();

                if (currentTopLeagues.Count > 0)
                {
                    _sportsbookContext.ClientTopLeague.RemoveRange(currentTopLeagues);
                }

                // insert new records
                var newTopLeagues = request.TopLeagues.Select(s => new ClientTopLeague
                {
                    ClientId = request.ClientId,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SortOrder = s.SortOrder
                });

                _sportsbookContext.ClientTopLeague.AddRange(newTopLeagues);

                await _sportsbookContext.SaveChangesAsync();

                scope.Complete();
            }

            return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }

        public async Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> CopyClientTopLeagues(CopyClientTopLeagueRequestDTO request)
        {
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    // validate requests

                    // check if league entries are duplicate
                    var duplicateLeagues = request.TopLeagues.GroupBy(g => g.LeagueId).Where(c => c.Count() > 1).ToList();

                    if (duplicateLeagues.Count > 0)
                    {
                        return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Result = new ClientTopLeaguesMessageResponseDTO()
                            {
                                StatusCode = StatusCode.Cannot_Have_Duplicate_Top_League_Entries,
                                Message = StatusCode.Cannot_Have_Duplicate_Top_League_Entries.ToString().Replace('_', ' ')
                            }
                        };
                    }

                    // check if sort order requests are duplicate
                    var duplicateSortOrders = request.TopLeagues.GroupBy(g => g.SortOrder).Where(c => c.Count() > 1).ToList();

                    if (duplicateSortOrders.Count > 0)
                    {
                        return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Result = new ClientTopLeaguesMessageResponseDTO()
                            {
                                StatusCode = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries,
                                Message = StatusCode.Cannot_Have_Duplicate_Sort_Order_Entries.ToString().Replace('_', ' ')
                            }
                        };
                    }

                    if (request.TopLeagues.Count > 50)
                    {
                        return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Result = new ClientTopLeaguesMessageResponseDTO()
                            {
                                StatusCode = StatusCode.Top_League_Entries_Must_Be_Equal_Or_Lower_Than_50,
                                Message = StatusCode.Top_League_Entries_Must_Be_Equal_Or_Lower_Than_50.ToString().Replace('_', ' ')
                            }
                        };
                    }

                    foreach (var clientId in request.ClientIds)
                    {
                        // delete existing records
                        var currentTopLeagues = _sportsbookContext.ClientTopLeague.Where(c => c.ClientId == clientId && c.SportId == request.SportId).ToList();

                        if (currentTopLeagues.Count > 0)
                        {
                            _sportsbookContext.ClientTopLeague.RemoveRange(currentTopLeagues);
                        }

                        // insert new records
                        var newTopLeagues = request.TopLeagues.Select(s => new ClientTopLeague
                        {
                            ClientId = clientId,
                            LeagueId = s.LeagueId,
                            LeagueName = s.LeagueName,
                            SegmentId = s.SegmentId,
                            SegmentName = s.SegmentName,
                            SportId = s.SportId,
                            SportName = s.SportName,
                            SortOrder = s.SortOrder
                        });

                        _sportsbookContext.ClientTopLeague.AddRange(newTopLeagues);
                    }

                    await _sportsbookContext.SaveChangesAsync();

                    scope.Complete();
                }

                return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString()
                };
            }
        }

        public async Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(string clientCode)
        {

            var fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId < 12 && c.SpId < 1000);
            var fixtureProjection = Builders<Fixture>.Projection.Include(c => c.FiId).Include(c => c.FiTyId).Include(c => c.FiStId).Include(c => c.SpId).Include(c => c.SpNa);
            var fixtures = await primaryFixtureCollection.Find(fixtureFilter).Project<FixtureClientSportFilter>(fixtureProjection).ToListAsync();
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

        public async Task<BaseResponseDTO<SportGroupResponseDTO>> GetSportGroups()
        {
            return _memCache.GetCache("sport-groups", () =>
            {
                return new BaseResponseDTO<SportGroupResponseDTO>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SportGroupResponseDTO()
                    {
                        SportGroups = _sportsbookContext.SportGroup.ToList()
                    }
                };
            }, 24);
        }

        public async Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO>> GetClientMarketTemplateGroups(ClientMarketTemplateGroupRequestDTO request)
        {
            // get client market template group
            var clientMarketTemplateGroups = await _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                .Where(c => c.SportId == request.SportId && c.ClientId == request.ClientId && c.FixtureStatusId == request.FixtureStatusId)
                .Select(s => new ClientMarketTemplateGroupModel
                {
                    MarketTemplateGroupId = s.Id,
                    Name = s.Name,
                    MarketTemplateIds = s.MarketTemplates,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive
                })
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            // get market template group and combine the MarketTemplateIds from prematch and live
            var rawMarketTemplateGroups = await _sportsbookContext.MarketTemplateGroup.AsNoTracking()
                .Where(c => c.SportId == request.SportId && c.FixtureStatusId == request.FixtureStatusId)
                .Select(s => new ClientMarketTemplateGroupModel
                {
                    MarketTemplateGroupId = s.Id,
                    Name = s.Name,
                    MarketTemplateIds = s.MarketTemplates,
                    SortOrder = s.SortOrder,
                    IsActive = true
                })
                .OrderBy(o => o.SortOrder)
                .ToListAsync();

            var marketTemplateGroups = new List<ClientMarketTemplateGroupModel>();

            if (rawMarketTemplateGroups.Any() || clientMarketTemplateGroups.Any())
            {
                var filteredRawMarketTemplateGroups = new List<ClientMarketTemplateGroupModel>();
                var sortOrder = 1000;

                if (clientMarketTemplateGroups.Any())
                    marketTemplateGroups.AddRange(clientMarketTemplateGroups);

                foreach (var marketGroup in rawMarketTemplateGroups)
                {
                    if (marketTemplateGroups.Any(c => c.Name.ToUpper() == marketGroup.Name.ToUpper()))
                        continue;

                    marketTemplateGroups.Add(new ClientMarketTemplateGroupModel
                    {
                        MarketTemplateGroupId = marketTemplateGroups.Any() ? marketTemplateGroups.Max(c => c.MarketTemplateGroupId) + 1 : 1,
                        Name = marketGroup.Name,
                        MarketTemplateIds = marketGroup.MarketTemplateIds,
                        SortOrder = sortOrder,
                        IsActive = true
                    });

                    sortOrder++;
                }

                if (!marketTemplateGroups.Any(c => c.Name.ToLower() == "all markets"))
                {
                    marketTemplateGroups.Insert(0, new ClientMarketTemplateGroupModel
                    {
                        MarketTemplateGroupId = marketTemplateGroups.Max(c => c.MarketTemplateGroupId) + 1,
                        Name = "All Markets",
                        MarketTemplateIds = string.Empty,
                        SortOrder = 1,
                        IsActive = true
                    });
                }
            }
            else
            {
                marketTemplateGroups.Add(new ClientMarketTemplateGroupModel
                {
                    MarketTemplateGroupId = 1,
                    Name = "All Markets",
                    MarketTemplateIds = string.Empty,
                    SortOrder = 1,
                    IsActive = true
                });
            }

            return new BaseResponseDTO<ClientMarketTemplateGroupResponseDTO>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientMarketTemplateGroupResponseDTO
                {
                    ClientId = request.ClientId,
                    SportId = request.SportId,
                    FixtureStatusId = request.FixtureStatusId,
                    ClientMarketTemplateGroups = marketTemplateGroups
                }
            };
        }

        public async Task<BaseResponseDTO<ClientMarketTemplateResponseDTO>> GetClientMarketTemplates(ClientMarketTemplateRequestDTO request)
        {
            var rawMarketTemplateGroups = (await GetClientMarketTemplateGroups(new ClientMarketTemplateGroupRequestDTO
            {
                ClientId = request.ClientId,
                SportId = request.SportId,
                FixtureStatusId = request.FixtureStatusId
            })).Result;

            var marketTemplateGroups = rawMarketTemplateGroups.ClientMarketTemplateGroups
                .Select(s => new
                {
                    Id = s.MarketTemplateGroupId,
                    Name = s.Name,
                    MarketTemplateIds = s.MarketTemplateIds.Split(',').ToList()
                })
                .Where(c => c.Name.ToLower() != "all markets")
                .ToList();

            var order = 1000;
            foreach (var marketTemplate in request.MarketTemplates)
            {
                // get marketTemplateGroup
                var filteredMarketTemplateGroup = marketTemplateGroups.Where(c => c.MarketTemplateIds.Contains(marketTemplate.MarketTemplateId)).ToList();

                marketTemplate.MarketTemplateGroupNames = filteredMarketTemplateGroup.Any() ? filteredMarketTemplateGroup.Select(s => s.Name).ToList() : new List<string>();

                marketTemplate.SortOrder = order;
                order++;
            }

            // get client market templates sort order
            var clientMarketTemplates = await _sportsbookContext.ClientMarketTemplateOrder.AsNoTracking()
                .Where(c => c.ClientId == request.ClientId && c.SportId == request.SportId)
                .OrderBy(o => o.SortOrder).ToListAsync();

            var filteredClientMarketTemplates = clientMarketTemplates.Join(request.MarketTemplates,
                cmt => cmt.MarketTemplateId,
                rmt => rmt.MarketTemplateId,
                (cmt, rmt) => new ClientMarketTemplateModel
                {
                    MarketTemplateGroupNames = rmt.MarketTemplateGroupNames,
                    MarketTemplateId = cmt.MarketTemplateId,
                    MarketTemplateName = rmt.MarketTemplateName,
                    SortOrder = cmt.SortOrder
                }).ToList();

            // join the 2 results to produce an output 
            var marketTemplates = new List<ClientMarketTemplateModel>();

            if (filteredClientMarketTemplates.Any())
                marketTemplates.AddRange(filteredClientMarketTemplates);

            if (request.MarketTemplates.Any())
            {
                // remove from rawMarketTemplates the records that are already existing in clientMarketTemplates
                var marketTemplateIds = clientMarketTemplates.Select(s => s.MarketTemplateId).ToList();
                request.MarketTemplates = request.MarketTemplates.Where(c => !marketTemplateIds.Contains(c.MarketTemplateId)).ToList();

                marketTemplates.AddRange(request.MarketTemplates);
            }

            return new BaseResponseDTO<ClientMarketTemplateResponseDTO>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientMarketTemplateResponseDTO
                {
                    ClientId = request.ClientId,
                    SportId = request.SportId,
                    FixtureStatusId = request.FixtureStatusId,
                    MarketTemplates = marketTemplates
                }
            };
        }

        public async Task<BaseResponseDTO<ClientMarketTemplateResponseDTO>> AddOrUpdateClientMarketTemplatesOrder(ClientMarketTemplateRequestDTO request)
        {
            var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };
            var hasVirtualSports = virtualSportIds.Any(c => c == request.SportId + 1000);

            var sportIds = new List<int>() { request.SportId };

            if (hasVirtualSports) sportIds.Add(1000 + request.SportId);

            foreach (var sportId in sportIds)
            {
                // fetch record by sportId, clientId
                var marketTemplates = await _sportsbookContext.ClientMarketTemplateOrder.Where(c => c.ClientId == request.ClientId && c.SportId == sportId).ToListAsync();

                #region ClientMarketTemplateSortOrder

                var marketTemplateIds = request.MarketTemplates.Select(s => s.MarketTemplateId).ToList();
                var editMarketTemplates = marketTemplates.Where(c => marketTemplateIds.Contains(c.MarketTemplateId)).ToList();

                // update
                foreach (var marketTemplate in editMarketTemplates)
                {
                    var editMarketTemplate = request.MarketTemplates.FirstOrDefault(c => c.MarketTemplateId == marketTemplate.MarketTemplateId);

                    if (editMarketTemplate != null && editMarketTemplate.SortOrder > 0)
                        marketTemplate.SortOrder = editMarketTemplate.SortOrder;
                }

                // insert
                marketTemplateIds = marketTemplates.Select(s => s.MarketTemplateId).ToList();
                var insertMarketTemplates = request.MarketTemplates.Where(c => !marketTemplateIds.Contains(c.MarketTemplateId))
                    .Select(s => new ClientMarketTemplateOrder
                    {
                        ClientId = request.ClientId,
                        SportId = sportId,
                        MarketTemplateId = s.MarketTemplateId,
                        SortOrder = s.SortOrder
                    })
                    .ToList();

                if (insertMarketTemplates.Any())
                    _sportsbookContext.ClientMarketTemplateOrder.AddRange(insertMarketTemplates);

                #endregion

                #region ClientMarketTemplateGroup

                // get raw data
                var marketTemplateGroupNames = _sportsbookContext.MarketTemplateGroup.AsNoTracking()
                    .Where(c => c.SportId == sportId)
                    .Select(s => s.Name)
                    .ToList();

                if (!marketTemplateGroupNames.Any())
                {
                    marketTemplateGroupNames.Add("All Markets");
                }
                else if (marketTemplateGroupNames.Any(c => c.ToLower() != "all markets"))
                {
                    marketTemplateGroupNames.Insert(0, "All Markets");
                }

                var rawMarketTemplateGroupNames = _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                    .Where(c => c.ClientId == request.ClientId && c.SportId == sportId)
                    .Select(s => s.Name)
                    .ToList();

                foreach (var marketTemplateGroupName in rawMarketTemplateGroupNames)
                {
                    if (!marketTemplateGroupNames.Any(c => c.ToLower() == marketTemplateGroupName.ToLower()))
                        marketTemplateGroupNames.Add(marketTemplateGroupName);
                }

                foreach (var marketTemplateGroupName in marketTemplateGroupNames)
                {
                    var selectedMarketTemplateIds = marketTemplateGroupName.ToLower() == "all markets" ?
                        request.MarketTemplates
                        .OrderBy(o => o.SortOrder)
                        .Select(s => s.MarketTemplateId)
                        .ToList()
                        :
                        request.MarketTemplates
                        .Where(c => c.MarketTemplateGroupNames.Contains(marketTemplateGroupName))
                        .OrderBy(o => o.SortOrder)
                        .Select(s => s.MarketTemplateId)
                        .ToList();

                    var finalMarketTemplateIds = string.Join(",", selectedMarketTemplateIds);

                    if (marketTemplateGroupName.ToLower() == "all markets")
                    {
                        _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                            request.ClientId.ToString(), sportId, (int)FixtureStatus.Prematch, marketTemplateGroupName, finalMarketTemplateIds);

                        _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                            request.ClientId.ToString(), sportId, (int)FixtureStatus.Live, marketTemplateGroupName, finalMarketTemplateIds);
                    }
                    else
                    {
                        var marketGroupModel = new List<SelectedClientMarketGroupModel>();

                        #region Prepare MarketGroupModel

                        var clientMarketTemplateGroup = _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                            .Where(c => c.ClientId == request.ClientId && c.SportId == sportId && c.Name == marketTemplateGroupName)
                            .ToList();

                        if (clientMarketTemplateGroup.Any())
                        {
                            marketGroupModel = clientMarketTemplateGroup.Select(s => new SelectedClientMarketGroupModel
                            {
                                FixtureStatusId = s.FixtureStatusId,
                                MarketTemplateIds = s.MarketTemplates
                            }).ToList();
                        }
                        else
                        {
                            var marketTemplateGroup = _sportsbookContext.MarketTemplateGroup.AsNoTracking()
                                .Where(c => c.SportId == sportId && c.Name == marketTemplateGroupName)
                                .ToList();

                            if (marketTemplateGroup != null)

                                marketGroupModel = marketTemplateGroup.Select(s => new SelectedClientMarketGroupModel
                                {
                                    FixtureStatusId = s.FixtureStatusId,
                                    MarketTemplateIds = s.MarketTemplates
                                }).ToList();
                        }

                        #endregion

                        foreach (var marketGroup in marketGroupModel)
                        {
                            if (marketGroup.FixtureStatusId == request.FixtureStatusId)
                            {
                                // modify marketTemplateIds (add or remove, sort order)
                                _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                                    request.ClientId.ToString(), sportId, request.FixtureStatusId, marketTemplateGroupName, finalMarketTemplateIds);
                            }
                            else
                            {
                                // modify marketTemplateIds (sort order)
                                var currentMarketTemplateIds = marketGroup.MarketTemplateIds.Split(',').ToList();

                                selectedMarketTemplateIds = request.MarketTemplates
                                    .Where(c => currentMarketTemplateIds.Contains(c.MarketTemplateId))
                                    .OrderBy(o => o.SortOrder)
                                    .Select(s => s.MarketTemplateId)
                                    .ToList();

                                finalMarketTemplateIds = string.Join(",", selectedMarketTemplateIds);

                                _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                                    request.ClientId.ToString(), sportId, marketGroup.FixtureStatusId, marketTemplateGroupName, finalMarketTemplateIds);
                            }
                        }

                    }
                }

                #endregion

                await _sportsbookContext.SaveChangesAsync();
            }

            return new BaseResponseDTO<ClientMarketTemplateResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientMarketTemplateResponseDTO
                {
                    ClientId = request.ClientId,
                    SportId = request.SportId,
                    MarketTemplates = request.MarketTemplates.Select(s => new ClientMarketTemplateModel
                    {
                        MarketTemplateGroupNames = s.MarketTemplateGroupNames,
                        MarketTemplateId = s.MarketTemplateId,
                        MarketTemplateName = s.MarketTemplateName,
                        SortOrder = s.SortOrder
                    })
                    .OrderBy(o => o.SortOrder)
                    .ToList()
                }
            };
        }

        public async Task<BaseResponseDTO<ClientAddMarketTemplateGroupResponseDTO>> AddClientMarketTemplateGroup(ClientAddMarketTemplateGroupRequestDTO request)
        {
            var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };
            var hasVirtualSports = virtualSportIds.Any(c => c == request.SportId + 1000);

            var sportIds = new List<int>();

            if (hasVirtualSports) sportIds.Add(1000 + request.SportId);

            sportIds.Add(request.SportId);

            var finalMarketTemplateOrders = string.Empty;

            foreach (var sportId in sportIds)
            {
                // verify if market name already exists
                foreach (var fixtureStatusId in request.FixtureStatusIds)
                {
                    var clientMarketTemplateGroup = _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                        .FirstOrDefault(c => c.ClientId == request.ClientId && c.SportId == sportId
                        && c.FixtureStatusId == fixtureStatusId && c.Name.ToLower() == request.Name.ToLower());

                    if (clientMarketTemplateGroup != null)
                    {
                        return new BaseResponseDTO<ClientAddMarketTemplateGroupResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"Market Group Name already exists in {(FixtureStatus)fixtureStatusId}",
                            Result = new ClientAddMarketTemplateGroupResponseDTO()
                        };
                    }
                }

                // fetch market template sort order
                var clientMarketTemplateOrders = _sportsbookContext.ClientMarketTemplateOrder.AsNoTracking()
                    .Where(c => c.ClientId == request.ClientId && c.SportId == sportId
                        && request.MarketTemplateIds.Contains(c.MarketTemplateId))
                    .OrderBy(o => o.SortOrder)
                    .Select(s => s.MarketTemplateId)
                    .ToList();

                finalMarketTemplateOrders = clientMarketTemplateOrders.Any() ? string.Join(',', clientMarketTemplateOrders) : string.Join(',', request.MarketTemplateIds);

                var hasClientMarketTemplateGroup = _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                    .Any(c => c.ClientId == request.ClientId && c.SportId == sportId);

                if (hasClientMarketTemplateGroup)
                {
                    // order market template by sort order
                    foreach (var fixtureStatusId in request.FixtureStatusIds)
                    {
                        _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                                request.ClientId.ToString(), sportId, fixtureStatusId, request.Name, string.Join(',', finalMarketTemplateOrders));
                    }
                }
                else
                {
                    // insert first the records from MarketTemplateGroup in ClientMarketTemplateGroup
                    var marketTemplateGroups = _sportsbookContext.MarketTemplateGroup.AsNoTracking()
                        .Where(c => c.SportId == sportId)
                        .ToList();

                    if (!marketTemplateGroups.Any() || !marketTemplateGroups.Any(c => c.Name.ToLower() == "all markets"))
                    {
                        if (!marketTemplateGroups.Any())
                        {
                            marketTemplateGroups.Add(new Shared.Data.Sportsbook.SQLEntities.MarketTemplateGroup
                            {
                                SportId = sportId,
                                Name = "All Markets",
                                FixtureStatusId = 0,
                                SortOrder = 0,
                                MarketTemplates = string.Join(',', request.MarketTemplates.Select(s => s.MarketTemplateId))
                            });
                        }
                        else if (!marketTemplateGroups.Any(c => c.Name.ToLower() == "all markets"))
                        {
                            marketTemplateGroups.Insert(0, new Shared.Data.Sportsbook.SQLEntities.MarketTemplateGroup
                            {
                                SportId = sportId,
                                Name = "All Markets",
                                FixtureStatusId = 0,
                                SortOrder = 0,
                                MarketTemplates = string.Join(',', request.MarketTemplates.Select(s => s.MarketTemplateId))
                            });
                        }
                    }

                    foreach (var marketTemplateGroup in marketTemplateGroups)
                    {
                        if (marketTemplateGroup.Name.ToLower() == "all markets")
                        {
                            _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                                request.ClientId.ToString(), sportId, (int)FixtureStatus.Prematch, marketTemplateGroup.Name, marketTemplateGroup.MarketTemplates);

                            _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                                request.ClientId.ToString(), sportId, (int)FixtureStatus.Live, marketTemplateGroup.Name, marketTemplateGroup.MarketTemplates);
                        }
                        else
                        {
                            _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                                request.ClientId.ToString(), sportId, marketTemplateGroup.FixtureStatusId, marketTemplateGroup.Name, marketTemplateGroup.MarketTemplates);
                        }
                    }

                    // order market template by sort order
                    foreach (var fixtureStatusId in request.FixtureStatusIds)
                    {
                        _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}",
                                request.ClientId.ToString(), sportId, fixtureStatusId, request.Name, string.Join(',', finalMarketTemplateOrders));
                    }
                }
            }

            return new BaseResponseDTO<ClientAddMarketTemplateGroupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientAddMarketTemplateGroupResponseDTO
                {
                    ClientId = request.ClientId,
                    SportId = request.SportId,
                    FixtureStatusIds = request.FixtureStatusIds,
                    Name = request.Name,
                    MarketTemplateIds = finalMarketTemplateOrders,
                }
            };
        }

        public async Task<BaseResponseDTO<ClientPostMarketTemplateGroupResponseDTO>> AddOrUpdateClientMarketTemplateGroup(ClientPostMarketTemplateGroupRequestDTO request)
        {
            var virtualSportIds = new int[] { 1001, 1002, 1003, 1005, 1021 };
            var hasVirtualSports = virtualSportIds.Any(c => c == request.SportId + 1000);

            var sportIds = new List<int>();

            if (hasVirtualSports) sportIds.Add(1000 + request.SportId);

            sportIds.Add(request.SportId);

            foreach (var sportId in sportIds)
            {

                foreach (var marketTemplateGroup in request.MarketTemplateGroups)
                {

                    // verify if market name already exists
                    if (request.IsAdd)
                    {
                        var clientMarketTemplateGroup = _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                                .FirstOrDefault(c => c.ClientId == request.ClientId && c.SportId == sportId
                                && c.FixtureStatusId == marketTemplateGroup.FixtureStatusId && c.Name.ToLower() == marketTemplateGroup.Name.ToLower());

                        if (clientMarketTemplateGroup != null)
                        {
                            return new BaseResponseDTO<ClientPostMarketTemplateGroupResponseDTO>
                            {
                                StatusCode = StatusCode.Error,
                                Message = $"Market Group Name already exists in {(FixtureStatus)marketTemplateGroup.FixtureStatusId}",
                                Result = new ClientPostMarketTemplateGroupResponseDTO()
                            };
                        }
                    }

                    var hasClientMarketTemplateGroup = _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                        .Any(c => c.ClientId == request.ClientId && c.SportId == sportId);

                    if (hasClientMarketTemplateGroup)
                    {
                        _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup2 {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                            request.ClientId.ToString(), sportId, marketTemplateGroup.FixtureStatusId, marketTemplateGroup.MarketTemplateGroupId,
                            marketTemplateGroup.Name, marketTemplateGroup.SortOrder, (marketTemplateGroup.IsActive ? 1 : 0), marketTemplateGroup.MarketTemplateIds);
                    }
                    else
                    {
                        // insert first the records from MarketTemplateGroup in ClientMarketTemplateGroup
                        var marketTemplateGroups = _sportsbookContext.MarketTemplateGroup.AsNoTracking()
                            .Where(c => c.SportId == sportId && c.Name.ToLower() != marketTemplateGroup.Name.ToLower())
                            .ToList();

                        if (marketTemplateGroup.Name.ToLower() != "all markets" && (!marketTemplateGroups.Any() || !marketTemplateGroups.Any(c => c.Name.ToLower() == "all markets")))
                        {
                            if (!marketTemplateGroups.Any())
                            {
                                marketTemplateGroups.Add(new Shared.Data.Sportsbook.SQLEntities.MarketTemplateGroup
                                {
                                    SportId = sportId,
                                    Name = "All Markets",
                                    FixtureStatusId = 0,
                                    SortOrder = 0,
                                    MarketTemplates = marketTemplateGroup.MarketTemplateIds
                                });
                            }
                            else if (!marketTemplateGroups.Any(c => c.Name.ToLower() == "all markets"))
                            {
                                marketTemplateGroups.Insert(0, new Shared.Data.Sportsbook.SQLEntities.MarketTemplateGroup
                                {
                                    SportId = sportId,
                                    Name = "All Markets",
                                    FixtureStatusId = 0,
                                    SortOrder = 0,
                                    MarketTemplates = marketTemplateGroup.MarketTemplateIds
                                });
                            }
                        }

                        foreach (var marketGroup in marketTemplateGroups)
                        {
                            if (marketGroup.Name.ToLower() == "all markets")
                            {
                                _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup2 {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                                    request.ClientId.ToString(), sportId, (int)FixtureStatus.Prematch, 0, marketGroup.Name, marketGroup.SortOrder, 1, marketGroup.MarketTemplates);

                                _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup2 {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                                    request.ClientId.ToString(), sportId, (int)FixtureStatus.Live, 0, marketGroup.Name, marketGroup.SortOrder, 1, marketGroup.MarketTemplates);
                            }
                            else
                            {
                                _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup2 {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                                    request.ClientId.ToString(), sportId, marketTemplateGroup.FixtureStatusId, 0, marketGroup.Name, marketGroup.SortOrder, 1, marketGroup.MarketTemplates);
                            }
                        }

                        _sportsbookContext.Database.ExecuteSqlRaw("InserOrUpdateClientMarketTemplateGroup2 {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                            request.ClientId.ToString(), sportId, marketTemplateGroup.FixtureStatusId, 0, marketTemplateGroup.Name, marketTemplateGroup.SortOrder, 1, marketTemplateGroup.MarketTemplateIds);
                    }
                }
            }

            return new BaseResponseDTO<ClientPostMarketTemplateGroupResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientPostMarketTemplateGroupResponseDTO
                {
                    ClientId = request.ClientId,
                    SportId = request.SportId,
                    MarketTemplateGroups = request.MarketTemplateGroups,
                }
            };
        }

        public async Task<BaseResponseDTO<bool>> DeleteClientMarketTemplateGroup(ClientDeleteMarketTemplateGroupRequestDTO request)
        {
            try
            {
                var marketGroup = await _sportsbookContext.ClientMarketTemplateGroup.FirstOrDefaultAsync(f => f.ClientId == request.ClientId && f.SportId == request.SportId && f.FixtureStatusId == request.FixtureStatusId && f.Id == request.MarketTemplateGroup.MarketTemplateGroupId);

                if (marketGroup != null)
                {
                    _sportsbookContext.ClientMarketTemplateGroup.Remove(marketGroup);
                    _sportsbookContext.SaveChanges();
                }

                return new BaseResponseDTO<bool>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = true
                };
            }
            catch (Exception)
            {
            }

            return new BaseResponseDTO<bool>
            {
                StatusCode = StatusCode.Error,
                Message = StatusCode.Error.ToString(),
                Result = false
            };
        }
        public async Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO>> CopyClientMarketTemplateGroup(ClientCopyMarketTemplateGroupRequestDTO request)
        {
            try
            {
                _sportsbookContext.Database.ExecuteSqlRaw("CopyClientMarketTemplateGroup {0}, {1}, {2}, {3}, {4}, {5}",
                        request.ClientId.ToString(), request.SportId, request.FixtureStatusId, request.CopyToClientId.ToString(), request.CopyToSportId, request.CopyToFixtureStatusId);

                var rawMarketTemplateGroupNames = _sportsbookContext.ClientMarketTemplateGroup.AsNoTracking()
                    .Where(c => c.ClientId == request.CopyToClientId && c.SportId == request.CopyToSportId && c.FixtureStatusId == request.CopyToFixtureStatusId)
                    .Select(s => new ClientMarketTemplateGroupModel
                    {
                        FixtureStatusId = s.FixtureStatusId,
                        MarketTemplateGroupId = s.Id,
                        Name = s.Name,
                        MarketTemplateIds = s.MarketTemplates,
                        IsActive = s.IsActive,
                        SortOrder = s.SortOrder
                    })
                    .ToList();

                return new BaseResponseDTO<ClientMarketTemplateGroupResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ClientMarketTemplateGroupResponseDTO
                    {
                        ClientId = request.CopyToClientId,
                        SportId = request.CopyToSportId,
                        FixtureStatusId = request.CopyToFixtureStatusId,
                        ClientMarketTemplateGroups = rawMarketTemplateGroupNames,
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientMarketTemplateGroupResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString(),
                    Result = new ClientMarketTemplateGroupResponseDTO
                    {
                        ClientId = request.CopyToClientId,
                        SportId = request.CopyToSportId,
                        FixtureStatusId = request.CopyToFixtureStatusId,
                        ClientMarketTemplateGroups = new List<ClientMarketTemplateGroupModel>()
                    }
                };
            }
        }

        private string GetSportName(string name, List<SportTranslationMDB> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return result.SpNa;
            else
                return name;
        }
        private string GetSegmentName(string name, List<SegmentTranslationMDB> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return result.SegNa;
            else
                return name;
        }
        private string GetLeagueName(string name, List<LeagueTranslationMDB> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return result.LeNa;
            else
                return name;
        }
        private string GetFixtureName(string name, List<FixtureTranslationMDB> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return result.FiNa;
            else
                return name;
        }
        private string GetHomeName(string name, List<FixtureTranslationMDB> translations, LanguageCode languageCode, FixtureType fixtureType)
        {
            if (translations == null)
                return fixtureType == FixtureType.Match ? name.Split(" vs. ").First().Trim() : string.Empty;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return fixtureType == FixtureType.Match ? result.FiNa.Split(" vs. ").First().Trim() : string.Empty;
            else
                return fixtureType == FixtureType.Match ? name.Split(" vs. ").First().Trim() : string.Empty;
        }
        private string GetAwayName(string name, List<FixtureTranslationMDB> translations, LanguageCode languageCode, FixtureType fixtureType)
        {
            if (translations == null)
                return fixtureType == FixtureType.Match ? name.Split(" vs. ").Last().Trim() : string.Empty;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return fixtureType == FixtureType.Match ? result.FiNa.Split(" vs. ").Last().Trim() : string.Empty;
            else
                return fixtureType == FixtureType.Match ? name.Split(" vs. ").Last().Trim() : string.Empty;
        }
        private string GetMarketName(string name, List<MarketTranslationMDB> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return result.MaNa;
            else
                return name;
        }
        private string GetSelectionName(string name, List<SelectionTranslationMDB> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).FirstOrDefault();

            if (result != null)
                return result.SeNa;
            else
                return name;
        }
        private string GetBoostedSelectionName(string clientCode, string selectionName, List<ClientBoostedSelectionNameMDB> boostedSelectionNames, List<ClientBoostedSelectionNameTranslationMDB> translations, LanguageCode languageCode)
        {
            if (boostedSelectionNames == null) //backward compatibility
                return selectionName;

            if (translations == null)
                return boostedSelectionNames.FirstOrDefault(s => s.ClCo == clientCode).BoSeNa;

            var result = translations.Where(c => c.ClCoLaCo == $"{clientCode}:{languageCode.ToString()}").FirstOrDefault();

            if (result != null)
                return result.BoSeNa;
            else
                return boostedSelectionNames.FirstOrDefault(s => s.ClCo == clientCode).BoSeNa;
        }
        private SettlementStatus GetSettlementStatus(BetStatus status)
        {
            SettlementStatus betStatus = SettlementStatus.Cancelled;
            switch (status)
            {
                case BetStatus.Lost:
                    betStatus = SettlementStatus.Lost;
                    break;
                case BetStatus.Won:
                    betStatus = SettlementStatus.Won;
                    break;
                case BetStatus.Void:
                    betStatus = SettlementStatus.Void;
                    break;
                case BetStatus.HalfWon:
                    betStatus = SettlementStatus.HalfWon;
                    break;
                case BetStatus.HalfLost:
                    betStatus = SettlementStatus.HalfLost;
                    break;
                default:
                    break;
            }
            return betStatus;
        }

        private bool GetResult(BetStatus status)
        {
            return status == BetStatus.Won || status == BetStatus.HalfWon ? true : false;
        }

        private VoidFactor GetVoidFactor(BetStatus status)
        {
            var voidFactor = VoidFactor.Zero;

            if (status == BetStatus.Won || status == BetStatus.Lost)
                return VoidFactor.Zero;

            if (status == BetStatus.Void)
                return VoidFactor.One;

            if (status == BetStatus.HalfWon || status == BetStatus.HalfLost)
                return VoidFactor.Half;

            return voidFactor;
        }

        private bool IsOEEnable(string clientCode)
        {
            return true;
            //if (string.IsNullOrEmpty(clientCode))
            //    return false;
            //return _configuration["Custom:ClientCodes"].Contains(clientCode) ? true : false;
        }

        private bool IsLiveStream(string livestreamId, string geoAllowed, string ipCountryCode)
        {
            if (string.IsNullOrEmpty(livestreamId))
                return false;

            if (!string.IsNullOrEmpty(livestreamId) && !string.IsNullOrEmpty(ipCountryCode) && _countryWhiteList.Contains(ipCountryCode.ToUpper()))
                return true;

            if (!string.IsNullOrEmpty(geoAllowed) && !string.IsNullOrEmpty(ipCountryCode))
            {
                if (!geoAllowed.Split(",").Any(g => g.ToUpper() == ipCountryCode.ToUpper()))
                    return false;
            }

            return true;

        }

        //private decimal GetClientOdds(IEnumerable<SelectionFilter> selections, SelectionFilter currentSelection, string clientMarketConfig, string clientCode)
        //{
        //    if (!IsOEEnable(clientCode))
        //        return currentSelection.Od;

        //    if (string.IsNullOrEmpty(clientMarketConfig))
        //        return currentSelection.Od;

        //    var activeSelections = selections.Where(c => c.IsAc == true && c.Od > 0m).ToList();
        //    int activeSelectionCount = activeSelections.Count();
        //    decimal margin = (Math.Round(activeSelections.Sum(s => 100 / s.Od)) - 100) / 100;

        //    var clients = !string.IsNullOrEmpty(clientMarketConfig) ?
        //        clientMarketConfig.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).ToList()
        //        : new List<Tuple<string, decimal>> { new Tuple<string, decimal>(clientCode, 0) };

        //    var clientMargin = clients.Where(c => c.Item1 == clientCode).FirstOrDefault();


        //    var result = new List<ClientOdds>();

        //    foreach (var selection in selections)
        //    {
        //        if (selection.IsAc == true && selection.Od > 0m)
        //        {
        //            var targetMargin = clientMargin.Item2;

        //            var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Od);

        //            if (fairOddsInitialDivideBy <= 0)
        //            {
        //                result.Add(new ClientOdds
        //                {
        //                    ClientCode = clientMargin.Item1,
        //                    Odds = selection.Od,
        //                    SelectionTemplateId = selection.SeTeId
        //                });

        //                continue;
        //            }

        //            var fairOdds = (activeSelectionCount * selection.Od) / fairOddsInitialDivideBy;

        //            if (fairOdds <= 0)
        //            {
        //                result.Add(new ClientOdds
        //                {
        //                    ClientCode = clientMargin.Item1,
        //                    Odds = selection.Od,
        //                    SelectionTemplateId = selection.SeTeId
        //                });

        //                continue;
        //            }

        //            var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

        //            if (finalOddsInitialDivideBy <= 0)
        //            {
        //                result.Add(new ClientOdds
        //                {
        //                    ClientCode = clientMargin.Item1,
        //                    Odds = selection.Od,
        //                    SelectionTemplateId = selection.SeTeId
        //                });

        //                continue;
        //            }

        //            var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

        //            if (odds < 1.00m)
        //            {
        //                result.Add(new ClientOdds
        //                {
        //                    ClientCode = clientMargin.Item1,
        //                    Odds = selection.Od,
        //                    SelectionTemplateId = selection.SeTeId
        //                });

        //                continue;
        //            }

        //            result.Add(new ClientOdds
        //            {
        //                ClientCode = clientMargin.Item1,
        //                Odds = odds,
        //                SelectionTemplateId = selection.SeTeId
        //            });
        //        }
        //        else
        //        {
        //            result.Add(new ClientOdds
        //            {
        //                ClientCode = clientMargin.Item1,
        //                Odds = selection.Od,
        //                SelectionTemplateId = selection.SeTeId
        //            });
        //        }
        //    }

        //    return result.Where(f => f.SelectionTemplateId == currentSelection.SeTeId).FirstOrDefault().Odds;
        //}

        private static string GetClientBoostedOdds(string clientBoostedOdds, string clientCode, bool isBoosted, string odds)
        {
            var boostedOdds = string.Empty;

            if (isBoosted)
            {
                if (string.IsNullOrEmpty(clientBoostedOdds))
                    return odds;

                var split = clientBoostedOdds.Split("|").ToList();
                boostedOdds = split.Where(s => s.Contains(clientCode)).FirstOrDefault();

                if (string.IsNullOrEmpty(boostedOdds))
                    return string.Empty;

                boostedOdds = boostedOdds.Split(":")[1];
                var cBoostedOdds = Convert.ToDecimal(boostedOdds);

                return cBoostedOdds.ToString("0.00");
            }
            else
            {
                return odds;
            }
        }

        private static List<ClientOdds> GetClientOdds(int activeSelectionCount, decimal margin, List<SelectionFilter> selections, string clientMarketConfig, string clientCode)
        {
            if (string.IsNullOrEmpty(clientMarketConfig))
                return new List<ClientOdds>();

            if (!clientMarketConfig.Contains(clientCode))
                return new List<ClientOdds>();

            var clients = clientMarketConfig.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).Where(c => c.Item1 == clientCode).ToList();

            var result = new List<ClientOdds>();
            var isBinary = selections.Count > 2 ? false : true;

            foreach (var client in clients)
            {
                foreach (var selection in selections)
                {

                    if (selection.IsAc == true && selection.Od > 0m)
                    {
                        var targetMargin = client.Item2;

                        var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Od);

                        if (fairOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        var fairOdds = (activeSelectionCount * selection.Od) / fairOddsInitialDivideBy;

                        if (fairOdds <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

                        if (finalOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

                        if (odds < 1.00m)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        if (odds > selection.Od)
                            odds = GetMasterOddsWithAddedMargin(selection.Od);

                        result.Add(new ClientOdds
                        {
                            ClientCode = client.Item1,
                            DecimalOdds = odds,
                            MalaysianOdds = GetMalayOdds(odds, isBinary),
                            HongkongOdds = GetHongkongOdds(odds, isBinary),
                            IndonesianOdds = GetIndoOdds(odds, isBinary),
                            SelectionTemplateId = selection.SeTeId,
                            SelectionId = selection.SeId
                        });
                    }
                    else
                    {
                        result.Add(new ClientOdds
                        {
                            ClientCode = client.Item1,
                            DecimalOdds = selection.Od,
                            MalaysianOdds = GetMalayOdds(selection.Od, isBinary),
                            HongkongOdds = GetHongkongOdds(selection.Od, isBinary),
                            IndonesianOdds = GetIndoOdds(selection.Od, isBinary),
                            SelectionTemplateId = selection.SeTeId,
                            SelectionId = selection.SeId
                        });
                    }
                }
            }

            return result;
        }
        private static List<ClientOdds> GetClientOdds(int activeSelectionCount, decimal margin, List<Selection> selections, string clientMarketConfig, string clientCode)
        {
            if (string.IsNullOrEmpty(clientMarketConfig))
                return new List<ClientOdds>();

            if (!clientMarketConfig.Contains(clientCode))
                return new List<ClientOdds>();

            var clients = clientMarketConfig.Split('|').Select(s => new Tuple<string, decimal>(s.Split(':').First(), Convert.ToDecimal(s.Split(':').Last()))).Where(c => c.Item1 == clientCode).ToList();

            var result = new List<ClientOdds>();
            var isBinary = selections.Count > 2 ? false : true;

            foreach (var client in clients)
            {
                foreach (var selection in selections)
                {
                    if (selection.IsAc == true && selection.Od > 0m)
                    {
                        var targetMargin = client.Item2;

                        var fairOddsInitialDivideBy = activeSelectionCount - (margin * selection.Od);

                        if (fairOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        var fairOdds = (activeSelectionCount * selection.Od) / fairOddsInitialDivideBy;

                        if (fairOdds <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        var finalOddsInitialDivideBy = ((fairOdds * targetMargin) / activeSelectionCount) + 1;

                        if (finalOddsInitialDivideBy <= 0)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        var odds = Math.Round(fairOdds / finalOddsInitialDivideBy, 2);

                        if (odds < 1.00m)
                        {
                            var tempOdds = GetMasterOddsWithAddedMargin(selection.Od);

                            result.Add(new ClientOdds
                            {
                                ClientCode = client.Item1,
                                DecimalOdds = tempOdds,
                                MalaysianOdds = GetMalayOdds(tempOdds, isBinary),
                                HongkongOdds = GetHongkongOdds(tempOdds, isBinary),
                                IndonesianOdds = GetIndoOdds(tempOdds, isBinary),
                                SelectionTemplateId = selection.SeTeId,
                                SelectionId = selection.SeId
                            });

                            continue;
                        }

                        if (odds > selection.Od)
                            odds = GetMasterOddsWithAddedMargin(selection.Od);

                        result.Add(new ClientOdds
                        {
                            ClientCode = client.Item1,
                            DecimalOdds = odds,
                            MalaysianOdds = GetMalayOdds(odds, isBinary),
                            HongkongOdds = GetHongkongOdds(odds, isBinary),
                            IndonesianOdds = GetIndoOdds(odds, isBinary),
                            SelectionTemplateId = selection.SeTeId,
                            SelectionId = selection.SeId
                        });
                    }
                    else
                    {
                        result.Add(new ClientOdds
                        {
                            ClientCode = client.Item1,
                            DecimalOdds = selection.Od,
                            MalaysianOdds = GetMalayOdds(selection.Od, isBinary),
                            HongkongOdds = GetHongkongOdds(selection.Od, isBinary),
                            IndonesianOdds = GetIndoOdds(selection.Od, isBinary),
                            SelectionTemplateId = selection.SeTeId,
                            SelectionId = selection.SeId
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

        private static decimal GetMalayOdds(decimal odds, bool isBinary)
        {
            // add binary condition if more than 2 value is decimal
            if (isBinary == false)
                return odds;

            if (odds == 1m)
                return 0;

            if (odds <= 2.00m)
                return odds - 1m;

            var retval = -1m / (odds - 1m);

            return Math.Truncate(retval * 100) / 100;
        }

        private static decimal GetHongkongOdds(decimal odds, bool isBinary)
        {
            // add binary condition if more than 2 value is decimal
            if (isBinary == false)
                return odds;

            if (odds == 1m)
                return 0;

            return odds - 1m;
        }

        private static decimal GetIndoOdds(decimal odds, bool isBinary)
        {
            // add binary condition if more than 2 value is decimal
            if (isBinary == false)
                return odds;

            if (odds == 1m)
                return 0;

            if (odds >= 2.00m)
                return odds - 1m;

            var retval = -1m / (odds - 1m);

            return Math.Truncate(retval * 100) / 100;
        }

        private List<BetBuilderMarket> GetBetBuilderMarketsXmlFiltered(XmlNodeList list, List<MarketMappingFilter> marketMappings, List<MarketFilter> markets, List<SelectionFilter> selections, LanguageCode languageCode)
        {
            var response = new List<BetBuilderMarket>();

            foreach (XmlElement marketNode in list)
            {
                var marketTemplateId = marketNode.Attributes["id"].Value;
                var specifiers = marketNode.HasAttribute("specifiers") ? marketNode.Attributes["specifiers"].Value : string.Empty;

                var marketMapped = marketMappings.FirstOrDefault(m => m.MaTeId == marketTemplateId && m.MaSp == specifiers);
                if (marketMapped == null)
                    continue;

                var market = markets.FirstOrDefault(m => marketMapped.MaId == m.MaId);
                if (market == null)
                    continue;

                var selectionResponse = new List<BetBuilderSelection>();
                var selectionIds = new List<long>();

                foreach (XmlElement selectionNode in marketNode.ChildNodes)
                {
                    var selectionTemplateId = selectionNode.Attributes["id"].Value;

                    var sconflict = selectionNode.HasAttribute("conflict") ? Convert.ToBoolean(selectionNode.Attributes["conflict"].Value) : false;
                    if (sconflict == false)
                        continue;

                    var selection = selections.FirstOrDefault(m => marketMapped.MaId == m.MaId && !selectionIds.Contains(m.SeId));
                    if (selection == null)
                        continue;

                    if (selectionResponse.Any(s => s.SelectionId == selection.SeId.ToString()))
                        continue;

                    selectionResponse.Add(new BetBuilderSelection
                    {
                        SelectionId = selection.SeId.ToString(),
                        IsActive = false
                    });
                    selectionIds.Add(selection.SeId);
                }

                if (!selectionResponse.Any())
                    continue;

                response.Add(new BetBuilderMarket
                {
                    MarketId = market.MaId.ToString(),
                    Selections = selectionResponse
                });

            }

            return response;
        }

        private List<BetBuilderMarketTemplates> GetBetBuilderMarketTemplates()
        {
            var result = _memCache.GetCache("bbmt", () =>
            {
                return _sportsbookContext.BetBuilderMarketTemplates.AsNoTracking().ToList();
            }, 24);

            return result;
        }

        private bool IsBetBuilder(int sportId, int fixtureTypeId, int fixtureStatusId, List<BetBuilderMarketTemplates> betBuilderMarketTemplates)
        {
            if(fixtureTypeId == (int)FixtureType.Match)
                return true;
            else if (fixtureStatusId == (int)FixtureStatus.Prematch && betBuilderMarketTemplates.Any(c => c.SportId == sportId && c.Prematch == true))
                return true;
            else if (fixtureStatusId == (int)FixtureStatus.Live && betBuilderMarketTemplates.Any(c => c.SportId == sportId && c.Live == true))
                return true;

            return false;
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
                    result = string.Empty;
                    break;
            }

            return result;
        }
    }
}
