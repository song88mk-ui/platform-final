using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
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
        public async Task<BaseResponseDTO<MTSAuthResponse>> GetMTSAuthorizationToken()
        {
            var cacheKey = $"mts:auth";

            var cache = _memCache.GetCache<BaseResponseDTO<MTSAuthResponse>>(cacheKey);

            if (cache != null)
                return cache;

            try
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                  new KeyValuePair<string, string>("client_id", "mts-edge-ext"),
                  new KeyValuePair<string, string>("client_secret",  _configuration["MTSMaxBetApi:ClientSecret"]),
                  new KeyValuePair<string, string>("username",  _configuration["MTSMaxBetApi:Username"]),
                  new KeyValuePair<string, string>("password",  _configuration["MTSMaxBetApi:Password"]),
                  new KeyValuePair<string, string>("grant_type", "password"),
                  new KeyValuePair<string, string>("client_auth_method", "client-secret")
                });

                var response = await _httpClient.PostAsync($"auth/realms/mts/protocol/openid-connect/token", formContent);

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<MTSMaxStakeAuthResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    
                    var result = new BaseResponseDTO<MTSAuthResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MTSAuthResponse
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

                    return new BaseResponseDTO<MTSAuthResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };
                }

            }
            catch (Exception ex)
            {
                _memCache.DeleteCache(cacheKey);

                return new BaseResponseDTO<MTSAuthResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}
