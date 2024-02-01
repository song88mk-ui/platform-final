using Microsoft.Extensions.Configuration;
using Platform.ClientSportsbook.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.ClientSportsbook.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IRabbitMQRepository _rabbitMQRepository;
        private readonly RedisCache _redisCache;
        private readonly List<string> _countryWhiteList;
        public SportsbookRepository(IConfiguration configuration, HttpClient httpClient, IRabbitMQRepository rabbitMQRepository, RedisCache redisCache)
        {
            _httpClient = httpClient;
            _rabbitMQRepository = rabbitMQRepository;
            _redisCache = redisCache;
            _countryWhiteList = configuration["Custom:CountryWhiteList"].Split(',').ToList();
        }
        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(ClientKeyFilter clientKeyFilter, string participantName)
        {
            //var clientCode = await GetClientCodeById(clientId);

            //if (string.IsNullOrEmpty(clientCode))
            //{
            //    return new BaseResponseDTO<ActiveFixturesResponseDTO>
            //    {
            //        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
            //        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
            //    };
            //}

            var request = new ActiveFixturesRequestDTO
            {
                ClientId = clientKeyFilter.ClientId,
                ClientCode = clientKeyFilter.ClientCode,
                LanguageCode = clientKeyFilter.LanguageCode,
                IPCountryCode = clientKeyFilter.IpCountryCode
            };

            //if (!string.IsNullOrEmpty(participantName))
            //{
            //    request.Home = participantName;
            //    request.Away = participantName;
            //}

            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/activefixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixturesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixturesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ActiveFixturesResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ActiveFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWLimitedResponseDTO>> GetActiveFixtureDetails(ClientKeyFilter clientKeyFilter, string fixtureId)
        {
            if(!long.TryParse(fixtureId, out var id))
            {
                return new BaseResponseDTO<TWLimitedResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} (Invalid ID)"
                };
            }

            var request = new ActiveFixtureDetailsRequestDTO
            {
                ClientId = clientKeyFilter.ClientId,
                ClientCode = clientKeyFilter.ClientCode,
                LanguageCode = clientKeyFilter.LanguageCode,
                IpCountryCode = clientKeyFilter.IpCountryCode,
                FixtureId = fixtureId
            };

            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/fixtures/details/limited", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWLimitedResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWLimitedResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWLimitedResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWLimitedResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<bool>> SubscribeToMarkets(ClientKeyFilter clientKeyFilter, MarketsSubscriptionRequestDTO request)
        {
            try
            {
                var isValidFixtureId = long.TryParse(request.FixtureId, out var fixtutreIds);
                var isValidIds = request.MarketIds.All(x => long.TryParse(x, out var id)) && isValidFixtureId;

                if (!isValidIds)
                {
                    return new BaseResponseDTO<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} (Invalid IDs)"
                    };
                }
                var payload = new
                {
                    FixtureId = fixtutreIds,
                    MarketIds = request.MarketIds.Select(x => long.Parse(x))
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(payload),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnSubscribeMarket",
                    RoutingKey = $"r.oe.sb.engine.onsubscribemarket.{clientKeyFilter.ClientCode.ToLower()}"
                });

                return new BaseResponseDTO<bool>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success.ToString().Replace('_', ' ')}",
                    Result = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<bool>> UnsubscribeToMarkets(ClientKeyFilter clientKeyFilter, MarketsSubscriptionRequestDTO request)
        {
            try
            {
                var isValidFixtureId = long.TryParse(request.FixtureId, out var fixtutreIds);
                var isValidIds = request.MarketIds.All(x => long.TryParse(x, out var id)) && isValidFixtureId;

                if (!isValidIds)
                {
                    return new BaseResponseDTO<bool>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} (Invalid IDs)"
                    };
                }
                var payload = new
                {
                    FixtureId = fixtutreIds,
                    MarketIds = request.MarketIds.Select(x => long.Parse(x))
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(payload),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnUnsubscribeMarket",
                    RoutingKey = $"r.oe.sb.engine.onunsubscribemarket.{clientKeyFilter.ClientCode.ToLower()}"
                });

                return new BaseResponseDTO<bool>
                {
                    StatusCode = StatusCode.Success,
                    Message = $"{StatusCode.Success.ToString().Replace('_', ' ')}",
                    Result = true
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<TWLimitedResponseDTO>> GetMarketsSubscription(ClientKeyFilter clientKeyFilter, string languageCode)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), languageCode);

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWLimitedResponseDTO>()
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var fixtureIds = await _redisCache.GetAsync<List<long>>($"subscribed-fixture-ids:{clientKeyFilter.ClientCode}");


            var result = new TWLimitedResponseDTO();
            if (fixtureIds != null)
            {
                foreach (var fixtureId in fixtureIds)
                {
                    var payload = await _redisCache.GetAsync<FixtureLimited>($"subscribed-fixture-markets:{clientKeyFilter.ClientCode}:{fixtureId}");
                    
                    result.Fixtures.Add(new FixtureLimitedDTO()
                    {
                        SportId = payload.SpId,
                        SportName = GetSportName(payload.SpNa, payload.SpNaTr, clientKeyFilter.LanguageCode),
                        SegmentId = payload.SegId,
                        SegmentName = GetSegmentName(payload.SegNa, payload.SegNaTr, clientKeyFilter.LanguageCode),
                        LeagueId = payload.LeId,
                        LeagueName = GetLeagueName(payload.LeNa, payload.LeNaTr, clientKeyFilter.LanguageCode),
                        FixtureId = payload.FiId.ToString(),
                        ProviderFixtureId = payload.PrFiId,
                        FixtureName = GetFixtureName(payload.FiNa, payload.FiNaTr, clientKeyFilter.LanguageCode),
                        Home = GetHomeName(payload.FiNa, payload.FiNaTr, clientKeyFilter.LanguageCode),
                        Away = GetAwayName(payload.FiNa, payload.FiNaTr, clientKeyFilter.LanguageCode),
                        FixtureType = ((FixtureType)payload.FiTyId).ToString(),
                        FixtureStartDate = payload.FiStDa,
                        FixtureStatus = payload.FiStNa,
                        IsLive = payload.SpId == 21 ? payload.FiStId == (int)FixtureStatus.Live : payload.IsOfLi,
                        IsLiveStream = IsLiveStream(payload.LiId, payload.GeAl, clientKeyFilter.IpCountryCode),
                        Markets = payload.Ma.Select(market => new MarketLimitedDTO()
                        {
                            MarketTemplateId = market.MaTeId,
                            MarketId = market.MaId,
                            MarketName = GetMarketName(market.MaNa, market.MaNaTr, clientKeyFilter.LanguageCode),
                            Line = market.Li,
                            MarketStatus = market.MaStNa,
                            Selections = market.Se.Select(selection => new SelectionLimitedDTO()
                            {
                                SelectionId = selection.SeId,
                                SelectionName = GetSelectionName(selection.SeNa, selection.SeNaTr, clientKeyFilter.LanguageCode),
                                Odds = selection.Od
                            }).ToList()
                        }).ToList()
                    });
                }
            }

            return new BaseResponseDTO<TWLimitedResponseDTO>()
            {
                StatusCode = StatusCode.Success,
                Message = $"{StatusCode.Success.ToString().Replace('_', ' ')}",
                Result = result
            };
        }
        private string GetSportName(string name, List<SportTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (result != null)
                return result.SpNa;
            else
                return name;
        }
        private string GetSegmentName(string name, List<SegmentTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (result != null)
                return result.SegNa;
            else
                return name;
        }
        private string GetLeagueName(string name, List<LeagueTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (result != null)
                return result.LeNa;
            else
                return name;
        }
        private string GetFixtureName(string name, List<FixtureTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (result != null)
                return result.FiNa;
            else
                return name;
        }
        private string GetHomeName(string name, List<FixtureTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name.Split(" vs. ").First().Trim();

            var translation = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (translation != null)
                return translation.FiNa.Split(" vs. ").First().Trim();
            else
                return name.Split(" vs. ").First().Trim();
        }
        private string GetAwayName(string name, List<FixtureTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name.Split(" vs. ").Last().Trim();

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (result != null)
                return result.FiNa.Split(" vs. ").Last().Trim();
            else
                return name.Split(" vs. ").Last().Trim();
        }
        private string GetMarketName(string name, List<MarketTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (result != null)
                return result.MaNa;
            else
                return name;
        }
        private string GetSelectionName(string name, List<SelectionTranslation> translations, LanguageCode languageCode)
        {
            if (translations == null)
                return name;

            var result = translations.Where(c => c.LaCo == languageCode.ToString()).LastOrDefault();

            if (result != null)
                return result.SeNa;
            else
                return name;
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
    }
}
