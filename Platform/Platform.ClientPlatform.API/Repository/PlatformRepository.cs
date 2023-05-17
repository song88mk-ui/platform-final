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
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IReadOnlyRepository _readOnlyRepository;

        public PlatformRepository(IConfiguration configuration, HttpClient httpClient, IReadOnlyRepository readOnlyRepository)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformAPI"]);
            _readOnlyRepository = readOnlyRepository;
        }

        public async Task<BaseResponseDTO<TWResponseDTO>> GetClientTopFixtures(string apiKey, string clientId, string countryCode, string languageCode)
        {
            return await _readOnlyRepository.GetClientTopFixtures(apiKey, clientId, countryCode, languageCode);
        }

        public async Task<BaseResponseDTO<TWActiveFixtureResponseDTO>> GetClientActiveFixtures(string apiKey, string clientId, string countryCode, string languageCode)
        {
            return await _readOnlyRepository.GetClientActiveFixtures(apiKey, clientId, countryCode, languageCode);
        }

        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string languageCode, string operatorToken, BetHistoryRequest request)
        {
            return await _readOnlyRepository.GetBetHistory(apiKey, clientId, languageCode, operatorToken, request);
        }
        public async Task<BaseResponseDTO<SettlementHistoryResponseDTO>> GetBetHistorySettled(string apiKey, string clientId, string languageCode, string ipAddress, BetHistorySettledRequest request)
        {
            return await _readOnlyRepository.GetBetHistorySettled(apiKey, clientId, languageCode, ipAddress, request);
        }
        public async Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(string apiKey, string clientId, string operatorToken, string ipAddress)
        {
            return await _readOnlyRepository.GetBetHistoryRunning(apiKey, clientId, operatorToken, ipAddress);
        }
        public async Task<BaseResponseDTO<BetCashOutResponseFilteredDTO>> CashoutBet(string apiKey, string clientId, string operatorToken, string ipAddress, CashoutBetRequest request)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                var response = await _httpClient.PostAsync($"v1/bet/cashout", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetCashOutResponseFilteredDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetCashOutResponseFilteredDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetCashOutResponseFilteredDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetCashOutResponseFilteredDTO>
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
