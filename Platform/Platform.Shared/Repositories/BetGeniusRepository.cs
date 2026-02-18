using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Enums;
using Platform.Shared.Utility;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Platform.Shared.Models.Provider.BetGenius;
using Platform.Shared.Models;
using System.Net;

namespace Platform.Shared.Repositories
{
    public class BetGeniusRepository : IBetGeniusRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly ILogger<BetGeniusRepository> _logger;
        private readonly ActivitySource _activitySource;


        public BetGeniusRepository(HttpClient httpClient,
            IConfiguration configuration,
            IMemCache memCache,
            ILogger<BetGeniusRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _memCache = memCache;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["Custom:BetGenius:Uri"]);
            _activitySource = new ActivitySource("BetGenius.API");
        }

        public async Task<BaseResponseDTO<BGAuth>> GetBetGeniusAuthorizationToken()
        {
            using (var activity = _activitySource.StartActivity("betgenius.authorization"))
            {
                var cacheKey = $"bg:auth";
                var cache = _memCache.GetCache<BaseResponseDTO<BGAuth>>(cacheKey);

                if (cache != null)
                {
                    activity?.SetTag("bg.auth.cached", true);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return cache;
                }

                try
                {
                    var payload = new
                    {
                        user = _configuration["Custom:BetGenius:User"],
                        password = _configuration["Custom:BetGenius:Password"]
                    };

                    activity?.SetTag("bg.auth.payload", payload);

                    var jsonPayload = JsonSerializer.Serialize(payload);

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"Auth-v1/PROD/login", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<BGAuth>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        var result = new BaseResponseDTO<BGAuth>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = initialResult
                        };

                        _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddSeconds(initialResult.ExpiresIn));

                        return result;
                    }
                    else
                    {
                        _logger.LogError($"GetBetGeniusAuthorizationToken Error: {await response.Content.ReadAsStringAsync()}");
                        _memCache.DeleteCache(cacheKey);

                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        return new BaseResponseDTO<BGAuth>
                        {
                            StatusCode = StatusCode.Error,
                            Message = StatusCode.Error.ToString(),
                            Result = new BGAuth
                            {
                                IdToken = string.Empty
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError($"GetBetGeniusAuthorizationToken Error: {ex.Message}-{ex.StackTrace}");

                    _memCache.DeleteCache(cacheKey);

                    return new BaseResponseDTO<BGAuth>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new BGAuth
                        {
                            IdToken = string.Empty
                        }
                    };
                }
            } // activity
        }


        public async Task<BaseResponseDTO<BGEvent>> GetBetGeniusFixture(string token, string fixtureId)
        {
            using (var activity = _activitySource.StartActivity("betgenius.fixture"))
            {
                var cacheKey = $"bg:fixture:{fixtureId}";
                var cache = _memCache.GetCache<BaseResponseDTO<BGEvent>>(cacheKey);

                if (cache != null)
                {
                    activity?.SetTag("bg.fixture.cached", true);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return cache;
                }

                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", $"{_configuration["Custom:BetGenius:Key"]}");

                    var env = _configuration["Custom:BetGenius:Env"];

                    var response = await _httpClient.GetAsync($"Video-v3/{env}/fixtures/{fixtureId}");

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<BGEvent>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        var result = new BaseResponseDTO<BGEvent>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = initialResult
                        };

                        _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddHours(5));

                        return result;
                    }
                    else
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            var result = new BaseResponseDTO<BGEvent>
                            {
                                StatusCode = StatusCode.Bet_Not_Found,
                                Message = StatusCode.Bet_Not_Found.ToString(),
                                Result = new BGEvent()
                            };

                            _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddHours(5));
                            return result;
                        }
                        var responseMessage = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"GetBetGeniusFixture Error: {responseMessage}");

                        if (responseMessage.Contains("Fixture not found"))
                        {
                            var result = new BaseResponseDTO<BGEvent>
                            {
                                StatusCode = StatusCode.Bet_Not_Found,
                                Message = StatusCode.Bet_Not_Found.ToString(),
                                Result = new BGEvent()
                            };

                            _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddHours(5));
                            return result;
                        }

                        _memCache.DeleteCache(cacheKey);

                        return new BaseResponseDTO<BGEvent>
                        {
                            StatusCode = StatusCode.Error,
                            Message = StatusCode.Error.ToString(),
                            Result = new BGEvent()
                        };
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError($"GetBetGeniusFixture Error: {ex.Message}-{ex.StackTrace}");

                    _memCache.DeleteCache(cacheKey);

                    return new BaseResponseDTO<BGEvent>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new BGEvent()
                    };
                }
            } // activity
        }

        public async Task<BaseResponseDTO<BGLiveStreamResponseDto>> GetLiveStreamToken(string token, string fixtureId, string liveStreamId, string deliveryId, string region, string device)
        {
            using (var activity = _activitySource.StartActivity("betgenius.livestream"))
            {
                var cacheKey = $"bg:livestream:{fixtureId}";
                var cache = _memCache.GetCache<BaseResponseDTO<BGLiveStreamResponseDto>>(cacheKey);

                if (cache != null)
                {
                    activity?.SetTag("bg.fixture.cached", true);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return cache;
                }

                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"{token}");
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", $"{_configuration["Custom:BetGenius:Key"]}");

                    var env = _configuration["Custom:BetGenius:Env"];
                    var payload = new
                    {
                        endUserSessionId = Guid.NewGuid().ToString(),
                        region = region,
                        device = device
                    };

                    var jsonPayload = JsonSerializer.Serialize(payload);

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"Video-v3/{env}/fixtures/{fixtureId}/live-streams/{liveStreamId}/deliveries/hls/{deliveryId}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<BGLiveStreamResponseDto>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        var result = new BaseResponseDTO<BGLiveStreamResponseDto>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = initialResult
                        };

                        _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddHours(5));

                        return result;
                    }
                    else
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            var result = new BaseResponseDTO<BGLiveStreamResponseDto>
                            {
                                StatusCode = StatusCode.Livestream_Is_Not_Available_In_Your_Region,
                                Message = StatusCode.Livestream_Is_Not_Available_In_Your_Region.ToString(),
                                Result = new BGLiveStreamResponseDto()
                            };

                            _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddHours(5));
                            return result;
                        }
                        var responseMessage = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"GetLiveStream Error: {responseMessage}");

                        _memCache.DeleteCache(cacheKey);

                        return new BaseResponseDTO<BGLiveStreamResponseDto>
                        {
                            StatusCode = StatusCode.Error,
                            Message = StatusCode.Error.ToString(),
                            Result = new BGLiveStreamResponseDto()
                        };
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError($"GetLiveStream Error: {ex.Message}-{ex.StackTrace}");

                    _memCache.DeleteCache(cacheKey);

                    return new BaseResponseDTO<BGLiveStreamResponseDto>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new BGLiveStreamResponseDto()
                    };
                }
            } // activity
        }

    }
}
