using Microsoft.Extensions.Configuration;
using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SportsbookRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformSportsbookAPI"]);
            //_httpClient.Timeout = TimeSpan.FromSeconds(5);
        }
        public async Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(LivestreamRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/livestream", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<LivestreamResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<LivestreamResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<LivestreamResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
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
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/topleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TopLeaguesResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(TopLeaguesRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/allleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TopLeaguesResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(TopLeaguesRequest2DTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/allleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TopLeaguesResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(TopFixturesRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TopFixturesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TopFixturesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TopFixturesResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TopFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups()
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/markettemplategroups", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<MarketTemplateGroupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<MarketTemplateGroupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    { 
                        return new BaseResponseDTO<MarketTemplateGroupResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<MarketTemplateGroupResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates()
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/selectormarkettemplates", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>> GetMarketSortOrder(MarketSortOrderRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/marketsortorder", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>> GetFixtureSortOrder(FixtureSortOrderRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/fixturesortorder", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientFixtureSortOrderResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientFixtureSortOrderResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<BetFixtureResponseDTO>> GetBetFixture(BetFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/betfixture", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetFixtureResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetFixtureResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetFixtureResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetFixtureResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<BetFixtureTranslationResponseDTO>> GetBetFixtureTranslation(BetFixtureTranslationRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/betfixture/translation", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetFixtureTranslationResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetFixtureTranslationResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetFixtureTranslationResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetFixtureTranslationResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures2(TWRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(TWRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/topfixtures/fixtureId", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TopFixturesIdsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TopFixturesIdsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TopFixturesIdsResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TopFixturesIdsResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(TWFLiveFixtureCountRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/livefixturecount", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWFixtureCountResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(TWFTodayFixtureCountRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/todayfixturecount", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWFixtureCountResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(TWLiveFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/livefixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(TWLeagueLiveFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/sports/segments/leagues/livefixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(TWTodayFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/todayfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(TWLeagueTodayFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/sports/segments/leagues/todayfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(TWUpcomingFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/upcomingfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(TWDailyFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/dailyfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(TWLeagueDailyFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/sports/segments/leagues/dailyfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(TWTopLeagueFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/topleaguefixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(TWSegmentFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/segments/fixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(TWLeagueFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/fixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<SportsResponseDTO>> GetSports(SportsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/sports", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<SportsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<SportsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<SportsResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<SportsResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(TWFixtureDetailsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/fixtures/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails (TWFixtureMarketSelectionDetailsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/fixtures/markets/selections/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(TWOutrightFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/outrightfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(TWLeagueOutrightFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/sports/segments/leagues/outrightfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(UserFavoriteRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/userfavorites/get/all", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(TWSearchRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/search", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWSearchResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWSearchResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWSearchResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWSearchResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(UserFavoriteRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/userfavorites/get", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<UserFavoriteResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(UserFavoriteInsertDeleteRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/userfavorites/insert", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<UserFavoriteResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(UserFavoriteInsertDeleteRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/userfavorites/remove", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<UserFavoriteResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(ClientTopFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientTopFixtureResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopFixtureResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures2(ClientTopFixtureRequestDTO2 request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/client/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientTopFixtureResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopFixtureResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(BackofficeSportsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/sports/details", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BackofficeSportsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BackofficeSportsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BackofficeSportsResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BackofficeSportsResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(PostClientTopFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/topfixtures/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> CopyClientTopFixtures(CopyClientTopFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/topfixtures/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<ClientTopLeaguesResponseDTO>> GetClientTopLeagues(ClientTopLeaguesRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/topleagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopLeaguesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientTopLeaguesResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(ClientTopLeagueDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/topleagues/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> CopyClientTopLeagues(CopyClientTopLeagueRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/topleagues/copy", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(SportsLookupRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/sports", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<SportsLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<SportsLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<SportsLookupResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<SportsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        
        public async Task<BaseResponseDTO<SportGroupResponseDTO>> GetSportGroups()
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/sportgroups", new StringContent(string.Empty));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<SportGroupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<SportGroupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<SportGroupResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<SportGroupResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<BoostedResponseDTO>> GetBoostedFixtures(BoostedFixturesRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/boostedfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BoostedResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BoostedResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BoostedResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BoostedResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>> GetBetBuilderSelectionsOdds(BetBuilderSelectionsOddsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/betbuilder/selections/odds", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}
