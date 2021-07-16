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

        public async Task<BaseResponseDTO<CashOutResponse>> GetCashOut(Bet bet, List<BetSelection> betSelections)
        {
            if (bet.BetStatusId == (int)BetStatus.Running)
            {
                var taxedStake = bet.TrueStake - (bet.TrueStake / (1 + bet.StakeTax));
                var stake = bet.TrueStake - taxedStake;

                try
                {
                    if (betSelections.Count == 0)
                    {
                        return new BaseResponseDTO<CashOutResponse>
                        {
                            StatusCode = StatusCode.CashOut_Is_Unavailable,
                            Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' '),
                            Result = new CashOutResponse
                            {
                                BetId = bet.BetId,
                                CashOutStake = 0m
                            }
                        };
                    }

                    var probabilites = new List<Probabilities>();
                    var hasProbabilityChanged = false;

                    foreach (var betSelection in betSelections)
                    {
                        var isExisting = false;
                        HttpResponseMessage response;

                        if (betSelection.SelectionSpecifier.Contains("?"))
                            response = await _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}/{betSelection.SelectionSpecifier.Split('?').Last().Replace('&', '|')}");
                        else
                            response = await _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}");

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadAsStringAsync();

                            XmlDocument doc = new XmlDocument();

                            doc.LoadXml(result);

                            var node = doc.ChildNodes[1].ChildNodes[1].ChildNodes[0];

                            var cashOutStatusId = Convert.ToInt32(node.Attributes["cashout_status"].Value);

                            if (cashOutStatusId == 1)
                            {
                                foreach (XmlElement selection in node.ChildNodes)
                                {
                                    var sel = selection.Attributes["id"].Value;

                                    if (sel == betSelection.SelectionTemplateId)
                                    {
                                        isExisting = true;
                                        var isActive = Convert.ToBoolean(Convert.ToInt32(selection.Attributes["active"].Value));

                                        double wonProbability = 0;
                                        double lostProbability = 0;
                                        double voidProbability = 0;
                                        double halfWonProbability = 0;
                                        double halfLostProbability = 0;

                                        try
                                        {
                                            wonProbability = Convert.ToDouble(selection.Attributes["win_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            lostProbability = Convert.ToDouble(selection.Attributes["lose_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            voidProbability = Convert.ToDouble(selection.Attributes["refund_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            halfWonProbability = Convert.ToDouble(selection.Attributes["half_win_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            halfLostProbability = Convert.ToDouble(selection.Attributes["half_lose_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        if (Math.Round(Convert.ToDecimal(betSelection.Probability), 2) != Math.Round(Convert.ToDecimal(selection.Attributes["probabilities"].Value), 2))
                                            hasProbabilityChanged = true;

                                        var probability = Convert.ToDouble(selection.Attributes["probabilities"].Value);

                                        if (isActive)
                                        {
                                            probabilites.Add(new Probabilities
                                            {
                                                CashOutStatusId = 1,
                                                Probability = probability,
                                                WonProbability = wonProbability,
                                                LostProbability = lostProbability,
                                                VoidProbability = voidProbability,
                                                HalfWonProbability = halfWonProbability,
                                                HalfLostProbability = halfLostProbability
                                            });
                                        }
                                        else
                                        {
                                            probabilites.Add(new Probabilities
                                            {
                                                CashOutStatusId = -1,
                                                Probability = 0
                                            });
                                        }
                                        break;
                                    }
                                }

                                if (isExisting == false)
                                {
                                    probabilites.Add(new Probabilities
                                    {
                                        CashOutStatusId = -1,
                                        Probability = 0
                                    });
                                }
                            }
                            else
                            {
                                probabilites.Add(new Probabilities
                                {
                                    CashOutStatusId = -1,
                                    Probability = 0
                                });
                            }
                        }
                        else
                        {
                            probabilites.Add(new Probabilities
                            {
                                CashOutStatusId = -1,
                                Probability = 0
                            });
                        }
                    }

                    if (betSelections.Count > 1)
                    {
                        if (probabilites.Any(c => c.CashOutStatusId == (int)CashOutStatus.Unavailable))
                        {
                            return new BaseResponseDTO<CashOutResponse>
                            {
                                StatusCode = StatusCode.CashOut_Is_Unavailable,
                                Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' '),
                                Result = new CashOutResponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStake = 0m,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                }
                            };
                        }

                        var finalProbability = probabilites.Where(c => c.Probability > 0).Select(s => s.Probability).Aggregate((a, b) => a * b);

                        var cashOutStake = (stake * bet.Odds) * Convert.ToDecimal(finalProbability);

                        cashOutStake = cashOutStake - (cashOutStake * 0.10m);

                        var taxedWinnings = (cashOutStake - stake) * bet.WinningsTax;

                        if (cashOutStake > stake)
                            cashOutStake = cashOutStake - taxedWinnings;
                        else
                            taxedWinnings = 0;

                        var finalCashOutStake = cashOutStake;

                        if (!hasProbabilityChanged)
                            finalCashOutStake = stake - (stake * 0.10m);

                        if (finalCashOutStake == 0 || finalCashOutStake > stake * bet.Odds)
                        {
                            return new BaseResponseDTO<CashOutResponse>
                            {
                                StatusCode = StatusCode.CashOut_Is_Unavailable,
                                Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' '),
                                Result = new CashOutResponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStake = 0m,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                }
                            };
                        }
                        else
                        {
                            return new BaseResponseDTO<CashOutResponse>
                            {
                                StatusCode = StatusCode.Success,
                                Message = StatusCode.Success.ToString().Replace('_', ' '),
                                Result = new CashOutResponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStake = finalCashOutStake,
                                    TaxedStake = taxedStake,
                                    TaxedWinnings = taxedWinnings,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                }
                            };
                        }
                    }
                    else
                    {
                        if (probabilites.Any(c => c.CashOutStatusId == (int)CashOutStatus.Unavailable))
                        {
                            return new BaseResponseDTO<CashOutResponse>
                            {
                                StatusCode = StatusCode.CashOut_Is_Unavailable,
                                Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' '),
                                Result = new CashOutResponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStake = 0m,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                }
                            };
                        }

                        decimal cashOutStake = 0m;

                        if (probabilites.First().WonProbability > 0 ||
                            probabilites.First().LostProbability > 0 ||
                            probabilites.First().VoidProbability > 0 ||
                            probabilites.First().HalfWonProbability > 0 ||
                            probabilites.First().HalfLostProbability > 0)
                        {
                            var won = Convert.ToDecimal(probabilites.First().WonProbability) * (stake * bet.Odds);
                            var lost = Convert.ToDecimal(probabilites.First().LostProbability) * (stake * 0);
                            var voidd = Convert.ToDecimal(probabilites.First().VoidProbability) * (stake * 1);
                            var halfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * (stake * ((bet.Odds + 1) / 2));
                            var halfLost = Convert.ToDecimal(probabilites.First().HalfLostProbability) * (stake / 2);

                            cashOutStake = won + lost + voidd + halfWon + halfLost;
                        }
                        else
                            cashOutStake = (stake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);

                        cashOutStake = cashOutStake - (cashOutStake * 0.10m);

                        var taxedWinnings = (cashOutStake - stake) * bet.WinningsTax;

                        if (cashOutStake > stake)
                            cashOutStake = cashOutStake - taxedWinnings;
                        else
                            taxedWinnings = 0;

                        var finalCashOutStake = cashOutStake;

                        if (!hasProbabilityChanged)
                            finalCashOutStake = stake - (stake * 0.10m);

                        if (finalCashOutStake <= stake * bet.Odds)
                        {
                            return new BaseResponseDTO<CashOutResponse>
                            {
                                StatusCode = StatusCode.Success,
                                Message = StatusCode.Success.ToString().Replace('_', ' '),
                                Result = new CashOutResponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStake = finalCashOutStake,
                                    TaxedStake = taxedStake,
                                    TaxedWinnings = taxedWinnings,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                }
                            };
                        }
                        else
                        {
                            return new BaseResponseDTO<CashOutResponse>
                            {
                                StatusCode = StatusCode.CashOut_Is_Unavailable,
                                Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' '),
                                Result = new CashOutResponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStake = 0m,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                }
                            };
                        }
                    }
                }
                catch (Exception)
                {
                    return new BaseResponseDTO<CashOutResponse>
                    {
                        StatusCode = StatusCode.CashOut_Is_Unavailable,
                        Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' '),
                        Result = new CashOutResponse
                        {
                            BetId = bet.BetId,
                            CashOutStake = 0m
                        }
                    };
                }
            }
            else
            {
                return new BaseResponseDTO<CashOutResponse>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' '),
                    Result = new CashOutResponse
                    {
                        BetId = bet.BetId,
                        CashOutStake = 0m
                    }
                };
            }
        }
    }
}
