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

namespace Platform.Betting.API.Repository
{
    public class MTSProbabilityRepository : IMTSProbabilityRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MTSProbabilityRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:MTSProbabilityAPI"]);
            _httpClient.DefaultRequestHeaders.Add("x-access-token", "bx8zTQrkEsbP0Gq7CS");
        }

        public async Task<BaseResponseDTO<CashoutResponse>> GetCashOut(Bet bet, List<BetSelection> betSelections, bool isCurrent)
        {
            //if (!isCurrent)
            //{
                return new BaseResponseDTO<CashoutResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CashoutResponse
                    {
                        CashOutStatusId = (int)CashOutStatus.Unavailable,
                        CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                        CashOutStake = 0m
                    }
                };
            //}

            if (bet.BetStatusId == (int)BetStatus.Running)
            {
                try
                {
                    var filteredBetSelections = betSelections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Running).ToList();

                    if (filteredBetSelections.Count == 0)
                    {
                        return new BaseResponseDTO<CashoutResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new CashoutResponse
                            {
                                CashOutStatusId = (int)CashOutStatus.Unavailable,
                                CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                                CashOutStake = 0m
                            }
                        };
                    }

                    var probabilites = new List<Probabilities>();

                    foreach (var betSelection in filteredBetSelections)
                    {
                        HttpResponseMessage response;

                        if (betSelection.SelectionSpecifier.Contains("?"))
                            response = await _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}/{betSelection.SelectionSpecifier.Split('?').Last()}");
                        else
                            response = await _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}");

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadAsStringAsync();

                            XmlDocument doc = new XmlDocument();

                            var node = doc.ChildNodes[1].ChildNodes[1].ChildNodes[0];

                            var cashOutStatusId = Convert.ToInt32(node.Attributes["cashout_status"].Value);

                            if (cashOutStatusId == 1)
                            {
                                foreach (XmlElement selection in node.ChildNodes)
                                {
                                    var sel = selection.Attributes["id"].Value;

                                    if (sel == betSelection.SelectionTemplateId)
                                    {
                                        var probability = Convert.ToDouble(selection.Attributes["probabilities"].Value);

                                        probabilites.Add(new Probabilities
                                        {
                                            CashOutStatusId = 1,
                                            Probability = probability
                                        });
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                probabilites.Add(new Probabilities
                                {
                                    CashOutStatusId = 0,
                                    Probability = 1
                                });
                            }
                        }
                        else
                        {
                            probabilites.Add(new Probabilities
                            {
                                CashOutStatusId = 0,
                                Probability = 1
                            });
                        }
                    }

                    if (betSelections.Count > 1)
                    {
                        var cashOutStake = (bet.TrueStake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);

                        //TODO:
                        return new BaseResponseDTO<CashoutResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new CashoutResponse
                            {
                                CashOutStatusId = (int)CashOutStatus.Unavailable,
                                CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                                CashOutStake = 0m
                            }
                        };
                    }
                    else
                    {
                        var cashOutStake = (bet.TrueStake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);

                        return new BaseResponseDTO<CashoutResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new CashoutResponse
                            {
                                CashOutStatusId = probabilites.First().CashOutStatusId,
                                CashOutStatusName = ((CashOutStatus)probabilites.First().CashOutStatusId).ToString(),
                                CashOutStake = cashOutStake - (cashOutStake * 0.10m)
                            }
                        };
                    }
                }
                catch (Exception)
                {
                    return new BaseResponseDTO<CashoutResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new CashoutResponse
                        {
                            CashOutStatusId = (int)CashOutStatus.Unavailable,
                            CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                            CashOutStake = 0m
                        }
                    };
                }
            }
            else
            {
                return new BaseResponseDTO<CashoutResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CashoutResponse
                    {
                        CashOutStatusId = (int)CashOutStatus.Unavailable,
                        CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                        CashOutStake = 0m
                    }
                };
            }
        }

        public async Task<BaseResponseDTO<CashoutResponse>> GetCashOut2(Bet bet, List<BetSelection> betSelections, bool isCurrent)
        {
            if (!isCurrent)
            {
                return new BaseResponseDTO<CashoutResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CashoutResponse
                    {
                        CashOutStatusId = (int)CashOutStatus.Unavailable,
                        CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                        CashOutStake = 0m
                    }
                };
            }

            if (bet.BetStatusId == (int)BetStatus.Running)
            {
                try
                {
                    var filteredBetSelections = betSelections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Running).ToList();

                    if (filteredBetSelections.Count == 0)
                    {
                        return new BaseResponseDTO<CashoutResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new CashoutResponse
                            {
                                CashOutStatusId = (int)CashOutStatus.Unavailable,
                                CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                                CashOutStake = 0m
                            }
                        };
                    }

                    var probabilites = new List<Probabilities>();

                    foreach (var betSelection in filteredBetSelections)
                    {
                        HttpResponseMessage response;

                        if (betSelection.SelectionSpecifier.Contains("?"))
                            response = await _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}/{betSelection.SelectionSpecifier.Split('?').Last()}");
                        else
                            response = await _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}");

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadAsStringAsync();

                            XmlDocument doc = new XmlDocument();

                            var node = doc.ChildNodes[1].ChildNodes[1].ChildNodes[0];

                            var cashOutStatusId = Convert.ToInt32(node.Attributes["cashout_status"].Value);

                            if (cashOutStatusId == 1)
                            {
                                foreach (XmlElement selection in node.ChildNodes)
                                {
                                    var sel = selection.Attributes["id"].Value;

                                    if (sel == betSelection.SelectionTemplateId)
                                    {
                                        var probability = Convert.ToDouble(selection.Attributes["probabilities"].Value);

                                        probabilites.Add(new Probabilities
                                        {
                                            CashOutStatusId = 1,
                                            Probability = probability
                                        });
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                probabilites.Add(new Probabilities
                                {
                                    CashOutStatusId = 0,
                                    Probability = 1
                                });
                            }
                        }
                        else
                        {
                            probabilites.Add(new Probabilities
                            {
                                CashOutStatusId = 0,
                                Probability = 1
                            });
                        }
                    }

                    if (betSelections.Count > 1)
                    {
                        var cashOutStake = (bet.TrueStake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);

                        //TODO:
                        return new BaseResponseDTO<CashoutResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new CashoutResponse
                            {
                                CashOutStatusId = (int)CashOutStatus.Unavailable,
                                CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                                CashOutStake = 0m
                            }
                        };
                    }
                    else
                    {
                        var cashOutStake = (bet.TrueStake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);

                        return new BaseResponseDTO<CashoutResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new CashoutResponse
                            {
                                CashOutStatusId = probabilites.First().CashOutStatusId,
                                CashOutStatusName = ((CashOutStatus)probabilites.First().CashOutStatusId).ToString(),
                                CashOutStake = cashOutStake - (cashOutStake * 0.10m)
                            }
                        };
                    }
                }
                catch (Exception)
                {
                    return new BaseResponseDTO<CashoutResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new CashoutResponse
                        {
                            CashOutStatusId = (int)CashOutStatus.Unavailable,
                            CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                            CashOutStake = 0m
                        }
                    };
                }
            }
            else
            {
                return new BaseResponseDTO<CashoutResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CashoutResponse
                    {
                        CashOutStatusId = (int)CashOutStatus.Unavailable,
                        CashOutStatusName = CashOutStatus.Unavailable.ToString(),
                        CashOutStake = 0m
                    }
                };
            }
        }
    }
    public class Probabilities
    {
        public double Probability { get; set; }
        public int CashOutStatusId { get; set; }
    }
}
