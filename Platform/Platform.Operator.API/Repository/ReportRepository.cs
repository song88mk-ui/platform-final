using Microsoft.Extensions.Configuration;
using Platform.Operator.API.Models;
using Platform.Shared.Enums;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public class ReportRepository : IReportsRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ReportRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformAgentReportsAPI"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["Custom:PlatformAgentReportsAPIKey"]);
        }

        public async Task<RunningBalanceResponse> GetRunningBalance(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"v1/reports/runningbalance/{userId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<RunningBalanceResponse>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<RunningBalanceResponse>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new RunningBalanceResponse
                        {
                            StatusCode = (int)StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RunningBalanceResponse
                {
                    StatusCode = (int)StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}
