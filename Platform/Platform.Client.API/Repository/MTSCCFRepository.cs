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

        private string GetDoubleDigit(int number)
        {
            if (number < 10)
                return $"0{number}";
            else
                return number.ToString();
        }

        public async Task<BaseResponseDTO<CustomerConfidenceFactorResponseDTO>> GetCustomerConfidenceFactor(string authToken, string userId)
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            var year = DateTime.UtcNow.Year;
            var startDate = $"{year}{GetDoubleDigit(DateTime.UtcNow.Month)}01000000";
            var endDate = $"{year}1231000000";
            var response = await _httpClient.GetAsync($"edge/proxy/ReportingCcf/external/api/report/export/history/ccf/changes/client/api?startDatetime={startDate}&endDatetime={endDate}&bookmakerId=32290&sourceId=10C{userId.Replace("-", string.Empty)}");

            if (response.IsSuccessStatusCode)
            {
                var initialResult = await response.Content.ReadAsStringAsync();
                var result = initialResult.Split("\n");
                var length = result.Length;

                var result2 = result[length - 2];

                var split = result2.Split(",");

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
