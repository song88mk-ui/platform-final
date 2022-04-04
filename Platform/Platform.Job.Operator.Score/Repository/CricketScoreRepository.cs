using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace Platform.Job.Operator.Score.Repository
{
    public class CricketScoreRepository : ICricketScoreRepository
    {
        private readonly HttpClient _httpClient;

        public CricketScoreRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://cricketapi.sportradar.com");
            _httpClient.DefaultRequestHeaders.Add("x-access-token", "wLCaM7vc80CADU33rz");

        }

        public async Task<string> GetScoreSummary(string fixtureSpecifier)
        {
            var settlementScore = string.Empty;
            try
            {
                var response = await _httpClient.GetAsync($"/UnifiedFeedScoring/json/get_scorecard?event_Id={fixtureSpecifier}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<CricketScoreResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if(result != null && result.Score != null)
                    {
                        var inning1 = "0/0";
                        var inning2 = "0/0";
                        var inningOvers = string.Empty;
                        var innings = result.Score.Innings;
                        if(innings != null)
                        {
                            foreach(var inning in innings)
                            {
                                if (inning.InningsNumber == 1)
                                    inning1 = $"{inning.Runs}/{inning.Wickets}";
                                if (inning.InningsNumber == 2)
                                {
                                    inningOvers = $" ({inning.Overs} ov)";
                                    inning2 = $"{inning.Runs}/{inning.Wickets}";
                                }
                            }
                        }
                        settlementScore = $"{inning2} - {inning1}{inningOvers}";
                    }
                }

                return settlementScore;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
    }
}
