using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PlatformRepository(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformAPI"]);
        }

        public async Task<BaseResponseDTO<TWResponseDTO>> GetClientTopFixtures(string clientId, string apiKey, string countryCode, string languageCode)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-country-code", countryCode);
                _httpClient.DefaultRequestHeaders.Add("x-language-code", languageCode);

                var response = await _httpClient.GetAsync($"v2/sportsbook/topfixtures");

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
    }
}
