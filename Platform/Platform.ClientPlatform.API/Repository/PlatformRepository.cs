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
    public class PlatformRepository : IPlatformRepository
    {
        #region Fields

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        #endregion

        #region Ctor

        public PlatformRepository(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformAPI"]);
        }

        #endregion

        #region Methods

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
        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistorySettled(string apiKey, string clientId, string languageCode, string ipAddress, BetHistorySettledRequest request)
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
        public async Task<BaseResponseDTO<BetCashOutResponseDTO>> CashoutBet(string apiKey, string clientId, string operatorToken, CashoutBetRequest request)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                var response = await _httpClient.PostAsync($"v1/bet/cashout", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetCashOutResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetCashOutResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetCashOutResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion

        #region Utilities

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

        #endregion
    }
}
