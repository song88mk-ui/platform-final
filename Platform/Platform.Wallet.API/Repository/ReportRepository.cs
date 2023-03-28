using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Wallet.API.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Wallet.API.Repository
{
    public class ReportRepository : IReportsRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ReportRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _httpClient.BaseAddress = new Uri(_configuration["PlatformAgentReports:Uri"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["PlatformAgentReports:ApiKey"]);
        }

        public async Task<RunningBalanceResponse> GetRunningBalance(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"v1/reports/runningbalance/{userId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<RunningBalanceResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<RunningBalanceResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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
