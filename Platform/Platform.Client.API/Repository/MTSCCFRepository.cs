using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Client.API.Repository
{
    public class MTSCCFRepository : IMTSCCFRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public MTSCCFRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:MTSCCFAPI"]);
        }

        public async Task<BaseResponseDTO<CustomerConfidenceFactorResponseDTO>> GetCustomerConfidenceFactor(string authToken, string userId)
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            var response = await _httpClient.GetAsync($"edge/proxy/ReportingCcf/external/api/report/export/history/ccf/changes/client/api?bookmakerId=32290&sourceId={userId.Replace("-", string.Empty)}");

            if (response.IsSuccessStatusCode)
            {
                var initialResult = JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                var split = initialResult.Split(",");

                return new BaseResponseDTO<CustomerConfidenceFactorResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CustomerConfidenceFactorResponseDTO
                    {
                        Timestamp = Convert.ToDouble(split[0]),
                        CurrentCCF = Convert.ToDouble(split[5]),
                        PreviousCCF = string.IsNullOrEmpty(split[6]) ? 0 : Convert.ToDouble(split[6]),
                        IsSpecificSport = string.IsNullOrEmpty(split[7]) ? false : true,
                        SportId = string.IsNullOrEmpty(split[7]) ? string.Empty : split[7].Split(":").Last(),
                        SportName = string.IsNullOrEmpty(split[8]) ? string.Empty : split[8],
                        AppliedTo = string.IsNullOrEmpty(split[9]) ? "Both" : split[9].ToLower() == "true" ? "Live" : "Prematch"
                    }
                };
            }

            return new BaseResponseDTO<CustomerConfidenceFactorResponseDTO>
            {
                StatusCode = StatusCode.No_Record,
                Message = StatusCode.No_Record.ToString()
            };
        }
    }
}
