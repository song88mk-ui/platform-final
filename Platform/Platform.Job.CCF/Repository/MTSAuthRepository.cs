using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Job.CCF.Repository
{
    public class MTSAuthRepository : IMTSAuthRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;

        public MTSAuthRepository(HttpClient httpClient, IConfiguration configuration, IMemCache memCache)
        {
            _configuration = configuration;
            _memCache = memCache;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:MTSAuthAPI"]);
        }
        public async Task<BaseResponseDTO<MTSAuthResponseDTO>> GetMTSAuthorizationToken()
        {
            var cacheKey = $"mts:auth";

            var cache = _memCache.GetCache<BaseResponseDTO<MTSAuthResponseDTO>>(cacheKey);

            if (cache != null)
                return cache;

            try
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                  new KeyValuePair<string, string>("client_id", "mts-edge-ext"),
                  new KeyValuePair<string, string>("client_secret",  _configuration["Custom:MTSClientSecret"]),
                  new KeyValuePair<string, string>("username",  _configuration["Custom:MTSUsername"]),
                  new KeyValuePair<string, string>("password",  _configuration["Custom:MTSPassword"]),
                  new KeyValuePair<string, string>("grant_type", "password"),
                  new KeyValuePair<string, string>("client_auth_method", "client-secret")
                });

                var response = await _httpClient.PostAsync($"auth/realms/mts/protocol/openid-connect/token", formContent);

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<MTSMaxStakeAuthResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    var result = new BaseResponseDTO<MTSAuthResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MTSAuthResponseDTO
                        {
                            Token = initialResult.AccessToken
                        }
                    };

                    _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddMinutes(55));

                    return result;
                }
                else
                {
                    _memCache.DeleteCache(cacheKey);

                    return new BaseResponseDTO<MTSAuthResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                        Result = new MTSAuthResponseDTO
                        {
                            Token = string.Empty
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _memCache.DeleteCache(cacheKey);

                return new BaseResponseDTO<MTSAuthResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MTSAuthResponseDTO
                    {
                        Token = string.Empty
                    }
                };
            }
        }
    }
}
