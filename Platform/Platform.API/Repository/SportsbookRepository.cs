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

        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<LivestreamResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<LivestreamResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopFixturesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopFixturesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<MarketTemplateGroupResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<MarketTemplateGroupResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
        public async Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates(SelectorMarketTemplatesRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/selectormarkettemplates", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetFixtureResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetFixtureResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetFixtureTranslationResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetFixtureTranslationResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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

        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(ActiveFixturesRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/activefixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ActiveFixturesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ActiveFixturesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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

        public async Task<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>> GetActiveFixtureDetails(ActiveFixtureDetailsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/fixtures/details/limited", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<ActiveFixtureDetailsResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ActiveFixtureDetailsResponseDTO>
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopFixturesIdsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TopFixturesIdsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixturesByLeague(TWLeagueUpcomingFixtureRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/sports/segments/leagues/upcomingfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<SportsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<SportsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWSearchResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWSearchResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<UserFavoriteResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BackofficeSportsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BackofficeSportsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopLeaguesResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<SportsLookupResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<SportsLookupResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<SportGroupResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<SportGroupResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BoostedResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BoostedResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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

        public async Task<BaseResponseDTO<BetBuilderMarketTemplatesDTO>> GetBetBuilderMarketTemplates()
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/betbuilder/markettemplates", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetBuilderMarketTemplatesDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetBuilderMarketTemplatesDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<BetBuilderMarketTemplatesDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetBuilderMarketTemplatesDTO>
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
                var response = await _httpClient.PostAsync($"v1/sportsbook/betbuilder/fixtures/totalodds", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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

        public async Task<BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>> GetMarketTemplatePlayerProps(MarketTemplatePlayerPropsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/markettemplateplayerprops", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<List<MarketTemplatePlayerPropsResponseDTO>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<List<TranslationResponseDTO>>> GetTranslationByLanguageCode(TranslationRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/translations", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<TranslationResponseDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<TranslationResponseDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<List<TranslationResponseDTO>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<List<TranslationResponseDTO>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<List<LeagueHighlightsResponseDTO>>> GetLeagueHighlights(LeagueHighlightsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/highlights", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<LeagueHighlightsResponseDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<List<LeagueHighlightsResponseDTO>>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<List<LeagueHighlightsResponseDTO>>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<List<LeagueHighlightsResponseDTO>>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLeagueHighlightsFixtures(LeagueHighlightsRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/highlights/fixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
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

        public async Task<BaseResponseDTO<bool>> TranslationRecovery(TranslationRecoveryRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/translation/recovery", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<bool>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<bool>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    }
                    else
                    {
                        return new BaseResponseDTO<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
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

        public async Task<BaseResponseDTO<AnnouncementResponseDTO>> GetAnnouncements(AnnouncementRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/announcements", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<AnnouncementResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<AnnouncementResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<AnnouncementResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<AnnouncementResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<BackofficeAnnouncementResponseDTO>> GetAnnouncements(BackofficeAnnouncementRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/announcements", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BackofficeAnnouncementResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BackofficeAnnouncementResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<BackofficeAnnouncementResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BackofficeAnnouncementResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<BackofficeAnnouncementResponseDTO>> AddOrUpdateAnnouncements(AddOrUpdateBackofficeAnnouncementRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/announcements/post", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BackofficeAnnouncementResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<BackofficeAnnouncementResponseDTO>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<BackofficeAnnouncementResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BackofficeAnnouncementResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<bool>> DeleteClientAnnouncements(DeleteBackofficeAnnouncementDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/client/announcements/delete", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return await JsonSerializer.DeserializeAsync<BaseResponseDTO<bool>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return await JsonSerializer.DeserializeAsync<BaseResponseDTO<bool>>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
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
    }
}
