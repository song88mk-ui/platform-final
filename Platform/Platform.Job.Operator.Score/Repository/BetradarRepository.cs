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
    public class BetradarRepository : IBetradarRepository
    {
        private readonly HttpClient _httpClient;

        public BetradarRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.betradar.com");
            _httpClient.DefaultRequestHeaders.Add("x-access-token", "wLCaM7vc80CADU33rz");

        }

        public async Task<string> GetScoreSummary(string fixtureSpecifier)
        {
            var settlementScore = string.Empty;
            try
            {
                var response = await _httpClient.GetAsync($"/v1/sports/en/sport_events/{fixtureSpecifier}/summary.xml");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    XmlDocument doc = new XmlDocument();

                    doc.LoadXml(result);

                    #region Validate feed

                    var sportStatusNode = doc.ChildNodes[1].ChildNodes[2];

                    var _endMatchStatus = new[] { (int)MatchStatus.Ended, (int)MatchStatus.AET, (int)MatchStatus.AOT, (int)MatchStatus.AP, (int)MatchStatus.Retired };

                    // applicable to all sports
                    // if there are still no final home or away score yet skip the current iteration.
                    if (sportStatusNode.Attributes["home_score"] == null || sportStatusNode.Attributes["away_score"] == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        settlementScore = $"{sportStatusNode.Attributes["home_score"].Value} - {sportStatusNode.Attributes["away_score"].Value}";
                    }

                    if (sportStatusNode.ChildNodes.Count > 0)
                    {
                        //get from result node
                        var resultsNode = sportStatusNode.ChildNodes[1];
                        if (resultsNode.ChildNodes.Count > 0)
                        {
                            var resultNode = resultsNode.ChildNodes[0];
                            var resultMatchStatusCode = resultNode.Attributes["match_status_code"] == null ? 0 : Convert.ToInt32(resultNode.Attributes["match_status_code"].Value);
                            if (_endMatchStatus.Contains(resultMatchStatusCode))
                            {
                                if (resultNode.Attributes["home_score"] == null || resultNode.Attributes["away_score"] == null)
                                {
                                    return string.Empty;
                                }
                                settlementScore = $"{resultNode.Attributes["home_score"].Value} - {resultNode.Attributes["away_score"].Value}";
                            }
                        }
                    }

                    #endregion
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
