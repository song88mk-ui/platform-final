using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Job.CCF.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly HttpClient _httpClient;
        public PlatformRepository(IConfiguration configuration, IMemCache memCache, HttpClient httpClient)
        {
            _configuration = configuration;
            _memCache = memCache;

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
        public async Task<BaseResponseDTO<MTSAuthResponseDTO>> GetMTSAuthorizationToken()
        {
            var cacheKey = $"mts:auth";

            var cache = _memCache.GetCache<BaseResponseDTO<MTSAuthResponseDTO>>(cacheKey);

            if (cache != null)
                return cache;

            try
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                  new KeyValuePair<string, string>("client_id", "mts-edge-ext"),
                  new KeyValuePair<string, string>("client_secret",  _configuration["Custom:MTSClientSecret"]),
                  new KeyValuePair<string, string>("username",  _configuration["Custom:MTSUsername"]),
                  new KeyValuePair<string, string>("password",  _configuration["Custom:MTSPassword"]),
                  new KeyValuePair<string, string>("grant_type", "password"),
                  new KeyValuePair<string, string>("client_auth_method", "client-secret")
                });

                var response = await _httpClient.PostAsync($"auth/realms/mts/protocol/openid-connect/token", formContent);

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<MTSMaxStakeAuthResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    var result = new BaseResponseDTO<MTSAuthResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MTSAuthResponseDTO
                        {
                            Token = initialResult.AccessToken
                        }
                    };

                    _memCache.AddOrUpdateCache(cacheKey, result, DateTimeOffset.UtcNow.AddMinutes(55));

                    return result;
                }
                else
                {
                    _memCache.DeleteCache(cacheKey);

                    return new BaseResponseDTO<MTSAuthResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                        Result = new MTSAuthResponseDTO
                        {
                            Token = string.Empty
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _memCache.DeleteCache(cacheKey);

                return new BaseResponseDTO<MTSAuthResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MTSAuthResponseDTO
                    {
                        Token = string.Empty
                    }
                };
            }
        }
        public async Task ProcessCCF()
        {
            var authToken = await GetMTSAuthorizationToken();

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            var year = DateTime.UtcNow.Year;
            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            var startDate = $"{year}{GetDoubleDigit(lastMonth.Month)}{GetDoubleDigit(lastMonth.Day)}000000";
            var endDate = $"{year}1231000000";
            var response = await _httpClient.GetAsync($"edge/proxy/ReportingCcf/external/api/report/export/history/ccf/changes/client/api?startDatetime={startDate}&endDatetime={endDate}&bookmakerId=32290");

            if (response.IsSuccessStatusCode)
            {
                var initialResult = await response.Content.ReadAsStringAsync();
                var result = initialResult.Split("\n");
                var length = result.Length;

                if (length <= 2)
                    return;

                var final = new List<CustomerConfidenceFactorTemp>();

                for (int i = 1; i < length - 1; i++)
                {
                    var temp = result[i];
                    var split = temp.Split(",");

                    final.Add(new CustomerConfidenceFactorTemp
                    {
                        Timestamp = Convert.ToDouble(split[0]),
                        UserId = split[3],
                        CurrentCCF = Convert.ToDouble(split[5]),
                        PreviousCCF = string.IsNullOrEmpty(split[6]) ? 0 : Convert.ToDouble(split[6]),
                        IsSpecificSport = string.IsNullOrEmpty(split[7]) ? false : true,
                        SportId = string.IsNullOrEmpty(split[7]) ? string.Empty : split[7].Split(":").Last(),
                        SportName = string.IsNullOrEmpty(split[8]) ? string.Empty : split[8],
                        AppliedTo = string.IsNullOrEmpty(split[9]) ? "Both" : split[9].ToLower() == "true" ? "Live" : "Prematch"
                    });
                }

                foreach (var ccf in final)
                {
                    try
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                        using (var context = new PlatformContext(optionsBuilder.Options))
                        {
                            var currency = context.ConfidenceFactor.Find(ccf.Timestamp, ccf.UserId);

                            if (currency == null)
                            {
                                context.ConfidenceFactor.Add(new Shared.Data.Platform.Entities.ConfidenceFactor
                                {
                                    Timestamp = Convert.ToInt64(ccf.Timestamp),
                                    UserId = ccf.UserId,
                                    CurrentCCF = ccf.CurrentCCF,
                                    PreviousCCF = ccf.PreviousCCF,
                                    IsSpecificSport = ccf.IsSpecificSport,
                                    SportId = Convert.ToInt32(ccf.SportId),
                                    SportName = ccf.SportName,
                                    AppliedTo = ccf.AppliedTo
                                });
                            }

                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        var message = ex.Message;
                    }
                }
            }
        }
    }
}
