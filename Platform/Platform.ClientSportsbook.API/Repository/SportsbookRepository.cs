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
        public SportsbookRepository(HttpClient httpClient, IRabbitMQRepository rabbitMQRepository, RedisCache redisCache)
        {
            _httpClient = httpClient;
            _rabbitMQRepository = rabbitMQRepository;
            _redisCache = redisCache;
        }
        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(ClientKeyFilter clientKeyFilter)
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
                IPCountryCode = clientKeyFilter.IPCountryCode
            };

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
            //var clientCode = await GetClientCodeById(clientId);

            //if (string.IsNullOrEmpty(clientCode))
            //{
            //    return new BaseResponseDTO<ActiveFixturesResponseDTO>
            //    {
            //        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
            //        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
            //    };
            //}

            var request = new ActiveFixtureDetailsRequestDTO
            {
                ClientId = clientKeyFilter.ClientId,
                ClientCode = clientKeyFilter.ClientCode,
                LanguageCode = clientKeyFilter.LanguageCode,
                IpCountryCode = clientKeyFilter.IPCountryCode,
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
            if (fixtureIds != null)
            {
                foreach (var fixtureId in fixtureIds)
                {
                    var payload = await _redisCache.GetAsync<FixtureLimited>($"subscribed-fixture-markets:{clientKeyFilter.ClientCode}:{fixtureId}");

                }
            }

            var result = new TWLimitedResponseDTO();
            return new BaseResponseDTO<TWLimitedResponseDTO>()
            {
                StatusCode = StatusCode.Success,
                Message = $"{StatusCode.Success.ToString().Replace('_', ' ')}",
                Result = result
            };
        }
    }
}
