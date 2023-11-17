using Microsoft.Extensions.Configuration;
using Platform.ClientPlatform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Repository
{
    public class ReadOnlyRepository : IReadOnlyRepository
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ReadOnlyRepository(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformAPIReadOnly"]);
        }

        public async Task<BaseResponseDTO<TWResponseDTO>> GetClientTopFixtures(string apiKey, string clientId, string countryCode, string languageCode)
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

        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(string apiKey, string clientId, string countryCode, string languageCode, ActiveFixturesFilterRequestDTO request)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-country-code", countryCode);
                _httpClient.DefaultRequestHeaders.Add("x-language-code", languageCode);

                var queryString = string.Empty;
                if(request != null)
                {
                    if (!string.IsNullOrEmpty(request.Home))
                        queryString = queryString + $"home={request.Home}&";
                    if (!string.IsNullOrEmpty(request.Away))
                        queryString = queryString + $"away={request.Away}&";
                    if (!string.IsNullOrEmpty(request.FixtureStatus))
                        queryString = queryString + $"fixtureStatus={request.FixtureStatus}&";
                    if (request.FromFixtureStartDate != null)
                        queryString = queryString + $"fromFixtureStartDate={request.FromFixtureStartDate}&";
                    if (request.ToFixtureStartDate != null)
                        queryString = queryString + $"toFixtureStartDate={request.ToFixtureStartDate}&";

                    queryString = queryString.TrimEnd('&');
                }

                var response = await _httpClient.GetAsync($"v1/sportsbook/activefixtures?{queryString}");

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

        public async Task<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>> GetActiveFixtureDetails(string apiKey, string clientId, string fixtureId, string ipCountryCode, string languageCode)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-country-code", ipCountryCode);
                _httpClient.DefaultRequestHeaders.Add("x-language-code", languageCode);

                var response = await _httpClient.GetAsync($"v1/sportsbook/activefixtures/details?fixtureId={fixtureId}");

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

        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string languageCode, string operatorToken, BetHistoryRequest request)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-language-code", languageCode);
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                var queryString = await FormUrlEncoded(request);

                var response = await _httpClient.GetAsync($"v1/bet/history{queryString}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetHistoryResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetHistoryResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetHistoryResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<SettlementHistoryResponseDTO>> GetBetHistorySettled(string apiKey, string clientId, string languageCode, string ipAddress, BetHistorySettledRequest request)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-language-code", languageCode);
                _httpClient.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);

                var queryString = await FormUrlEncoded(request);

                var response = await _httpClient.GetAsync($"v1/bet/history/settled{queryString}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<SettlementHistoryResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<SettlementHistoryResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<SettlementHistoryResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<SettlementHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(string apiKey, string clientId, string operatorToken, string ipAddress)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                var response = await _httpClient.GetAsync($"v1/bet/history/running");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        private async Task<string> FormUrlEncoded<T>(T parameter)
        {
            var urlEncodedString = new StringBuilder();

            var parameters = new Dictionary<string, string>();

            var obj = parameter.GetType();

            foreach (var property in obj.GetProperties())
            {
                var data = property.GetValue(parameter, null);
                if (data != null)
                {
                    if (property.PropertyType == typeof(DateTimeOffset)
                        || property.PropertyType == typeof(DateTime)
                        || property.PropertyType == typeof(DateTime?))
                    {
                        var date = Convert.ToDateTime(data.ToString());
                        parameters.Add(property.Name, date.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                    else
                    {
                        parameters.Add(property.Name, data.ToString());
                    }
                }
            }

            if (!parameters.Any())
                return urlEncodedString.ToString();

            urlEncodedString.Append("?");

            var content = new FormUrlEncodedContent(parameters);
            urlEncodedString.Append(await content.ReadAsStringAsync());

            content.Dispose();

            return urlEncodedString.ToString();
        }
    }
}
