using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Providers.GTS;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public class GTSAuthRepository : IGTSAuthRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly ILogger<GTSAuthRepository> _logger;

        public GTSAuthRepository(HttpClient httpClient, 
            IConfiguration configuration, 
            IMemCache memCache,
            ILogger<GTSAuthRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _memCache = memCache;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:GTSAuthAPI"]);
        }

        public async Task<BaseResponseDTO<ProviderAuthResponseDTO>> GetGTSAuthorizationToken()
        {
            if (Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]) == false)
            {
                return new BaseResponseDTO<ProviderAuthResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ProviderAuthResponseDTO
                    {
                        Token = string.Empty
                    }
                };
            }

            var cacheKey = $"gts:auth";

            var cache = _memCache.GetCache<BaseResponseDTO<ProviderAuthResponseDTO>>(cacheKey);

            if (cache != null)
                return cache;

            try
            {
                var payload = new
                {
                    grant_type = "client_credentials",
                    audience = "https://api.geniussports.com",
                    client_id = _configuration["Custom:GTSClientID"],
                    client_secret = _configuration["Custom:GTSClientSecret"]
                };

                var jsonPayload = JsonSerializer.Serialize(payload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"oauth/token", content);

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<MTSMaxStakeAuthResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    var result = new BaseResponseDTO<ProviderAuthResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderAuthResponseDTO
                        {
                            Token = initialResult.AccessToken
                        }
                    };

                    _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddSeconds(initialResult.ExpiresInSeconds));

                    return result;
                }
                else
                {
                    _memCache.DeleteCache(cacheKey);

                    return new BaseResponseDTO<ProviderAuthResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                        Result = new ProviderAuthResponseDTO
                        {
                            Token = string.Empty
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetGTSAuthorizationToken Error: {JsonSerializer.Serialize(ex)}");

                _memCache.DeleteCache(cacheKey);

                return new BaseResponseDTO<ProviderAuthResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new ProviderAuthResponseDTO
                    {
                        Token = string.Empty
                    }
                };
            }
        }
    }
}
