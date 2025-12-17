using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public class GTSAuthRepository : IGTSAuthRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;

        public GTSAuthRepository(HttpClient httpClient, IConfiguration configuration, IMemCache memCache)
        {
            _configuration = configuration;
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
                var formContent = new FormUrlEncodedContent(new[]
                {
                  new KeyValuePair<string, string>("grant_type", "client_credentials"),
                  new KeyValuePair<string, string>("client_auth_method", "https://api.geniussports.com"),
                  new KeyValuePair<string, string>("client_id", _configuration["Custom:GTSClientID"]),
                  new KeyValuePair<string, string>("client_secret",  _configuration["Custom:GTSClientSecret"])
                });

                var response = await _httpClient.PostAsync($"oauth/token", formContent);

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
