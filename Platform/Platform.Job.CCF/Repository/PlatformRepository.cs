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
        private readonly IMTSAuthRepository _mtsAuthRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        public PlatformRepository(IMTSAuthRepository mtsAuthRepository, IConfiguration configuration, HttpClient httpClient)
        {
            _mtsAuthRepository = mtsAuthRepository;
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

        public async Task ProcessCCF()
        {
            var authToken = await _mtsAuthRepository.GetMTSAuthorizationToken();

            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken.Result.Token}");

            var currentDate = DateTime.UtcNow;

            var year = currentDate.Year;
            var lastMonth = currentDate.AddMonths(-1);

            var dayAfter = lastMonth.AddDays(1);

            var startDate = $"{dayAfter.Year}{GetDoubleDigit(dayAfter.Month)}{GetDoubleDigit(dayAfter.Day)}000000";
            var endDate = $"{year}1231000000";
            var response = await _httpClient.GetAsync($"edge/proxy/ReportingCcf/external/api/report/export/history/ccf/changes/client/api?startDatetime={startDate}&endDatetime={endDate}&bookmakerId={_configuration["Custom:MTSBookmakerId"]}");

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
                    try
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
                    catch (Exception ex)
                    {
                    }
                }

                foreach (var ccf in final)
                {
                    try
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                        using (var context = new PlatformContext(optionsBuilder.Options))
                        {
                            var confidence = context.ConfidenceFactor.Find(Convert.ToInt64(ccf.Timestamp), ccf.UserId);

                            if (confidence == null)
                            {
                                context.ConfidenceFactor.Add(new Shared.Data.Platform.Entities.ConfidenceFactor
                                {
                                    Timestamp = Convert.ToInt64(ccf.Timestamp),
                                    UserId = ccf.UserId,
                                    CurrentCCF = ccf.CurrentCCF,
                                    PreviousCCF = ccf.PreviousCCF,
                                    IsSpecificSport = ccf.IsSpecificSport,
                                    SportId = ccf.SportId,
                                    SportName = ccf.SportName,
                                    AppliedTo = ccf.AppliedTo
                                });
                            }

                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message}");
                    }
                }
            }
        }
    }
}
