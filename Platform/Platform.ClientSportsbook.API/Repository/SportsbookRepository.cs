using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Net;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Platform.Shared.Data.Platform;
using Platform.ClientSportsbook.API.Models;

namespace Platform.ClientSportsbook.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly HttpClient _httpClient;
        public SportsbookRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(ClientKeyFilter clientKeyFilter)
        {
            //var clientCode = await GetClientCodeById(clientId);

            //if (string.IsNullOrEmpty(clientCode))
            //{
            //    return new BaseResponseDTO<ActiveFixturesResponseDTO>
            //    {
            //        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
            //        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
            //    };
            //}

            var request = new ActiveFixturesRequestDTO
            {
                ClientId = clientKeyFilter.ClientId,
                ClientCode = clientKeyFilter.ClientCode,
                LanguageCode = clientKeyFilter.LanguageCode,
                IPCountryCode = clientKeyFilter.IPCountryCode
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
        public async Task<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>> GetActiveFixtureDetails(ClientKeyFilter clientKeyFilter, string fixtureId)
        {
            //var clientCode = await GetClientCodeById(clientId);

            //if (string.IsNullOrEmpty(clientCode))
            //{
            //    return new BaseResponseDTO<ActiveFixturesResponseDTO>
            //    {
            //        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
            //        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
            //    };
            //}

            var request = new ActiveFixtureDetailsRequestDTO
            {
                ClientId = clientKeyFilter.ClientId,
                ClientCode = clientKeyFilter.ClientCode,
                LanguageCode = clientKeyFilter.LanguageCode,
                IpCountryCode = clientKeyFilter.IPCountryCode,
                FixtureId = fixtureId
            };

            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/fixtures/details/limited", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
    }
}
