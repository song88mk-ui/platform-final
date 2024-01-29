using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Net;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Platform.Shared.Data.Platform;

namespace Platform.ClientSportsbook.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly HttpClient _httpClient;
        private readonly PlatformContext _platformContext;
        public SportsbookRepository(IConfiguration configuration, HttpClient httpClient, PlatformContext platformContext)
        {
            _httpClient = httpClient;
            _platformContext = platformContext;
        }
        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(Guid clientId, string ipCountryCode, LanguageCode languageCode)
        {
            var clientCode = await GetClientCodeById(clientId);

            if (string.IsNullOrEmpty(clientCode))
            {
                return new BaseResponseDTO<ActiveFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var request = new ActiveFixturesRequestDTO
            {
                ClientId = clientId,
                LanguageCode = languageCode,
                ClientCode = clientCode,
                IPCountryCode = ipCountryCode
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


        private async Task<string> GetClientCodeById(Guid clientId)
        {
            var result = await _platformContext.Client.FindAsync(clientId);

            if (result != null && result.IsActive)
            {
                return result.ClientCode;
            }

            return string.Empty;
        }
    }
}
