using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Platform.Shared.Models.Provider.BetGenius;

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
            _httpClient.BaseAddress = new Uri(_configuration["API:BetGenius:Uri"]);
            _activitySource = new ActivitySource("BetGenius.API");
        }

        public async Task<BaseResponseDTO<BGAuth>> GetBetGeniusAuthorizationToken()
        {
            using(var activity = _activitySource.StartActivity("betgenius.authorization"))
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
                        user = _configuration["API:BetGenius:User"],
                        password = _configuration["API:BetGenius:Password"]
                    };

                    activity?.SetTag("bg.auth.payload", payload);

                    var jsonPayload = JsonSerializer.Serialize(payload);

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"oauth/token", content);

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
                var cacheKey = $"bg:fixture";
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
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", $"{_configuration["API:BetGenius:Key"]}");

                    var env = _configuration["API:BetGenius:Env"];

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

                        _logger.LogError($"GetBetGeniusFixture Error: {response.Content.ReadAsStringAsync()}");
                        _memCache.DeleteCache(cacheKey);

                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

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
    }
}
