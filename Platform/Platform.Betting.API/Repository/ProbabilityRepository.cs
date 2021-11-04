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

namespace Platform.Betting.API.Repository
{
    public class ProbabilityRepository : IProbabilityRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ProbabilityRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformSportsbookAPI"]);
        }

        private async Task<BaseResponseDTO<BetSelectionProbabilityResponseDTO>> GetSelectionProbabilities(BetSelectionProbabilityRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/betselectionprobability", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetSelectionProbabilityResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetSelectionProbabilityResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetSelectionProbabilityResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetSelectionProbabilityResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<List<CashOutDetailsReponse>>> GetCashOut(List<Bet> bets, List<BetSelection> betSelections)
        {
            var result = new List<CashOutDetailsReponse>();
            var tempSelections = betSelections.Where(c => c.FixtureTypeId == (int)FixtureType.Match && c.SportId < 1000 && c.BetSelectionStatusId == (int)BetStatus.Running).Select(s => new Tuple<long, long, long>(s.FixtureId, s.MarketId, s.SelectionId)).ToList();
            
            if (tempSelections.Count == 0)
            {
                foreach (var bet in bets)
                {
                    result.Add(new CashOutDetailsReponse
                    {
                        BetId = bet.BetId,
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    });
                }

                return new BaseResponseDTO<List<CashOutDetailsReponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace('_', ' '),
                    Result = result
                };
            }

            var probabilityResult = await GetSelectionProbabilities(new BetSelectionProbabilityRequestDTO { Selections = tempSelections });

            if (probabilityResult.StatusCode != StatusCode.Success)
            {
                foreach (var bet in bets)
                {
                    result.Add(new CashOutDetailsReponse
                    {
                        BetId = bet.BetId,
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    });
                }

                return new BaseResponseDTO<List<CashOutDetailsReponse>>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace('_', ' '),
                    Result = result
                };
            }

            var selectionProbabilities = probabilityResult.Result.Selections;

            foreach (var bet in bets)
            {
                if (bet.BetStatusId != (int)BetStatus.Running)
                {
                    result.Add(new CashOutDetailsReponse
                    {
                        BetId = bet.BetId,
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    });

                    continue;
                }

                if (bet.IsFreeBet)
                {
                    result.Add(new CashOutDetailsReponse
                    {
                        BetId = bet.BetId,
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    });

                    continue;
                }

                if (bet.BetStatusId == (int)BetStatus.Running)
                {
                    var selections = betSelections.Where(c => c.BetId == bet.BetId).ToList();

                    if (selections.Count == 0)
                    {
                        result.Add(new CashOutDetailsReponse
                        {
                            BetId = bet.BetId,
                            CashOutStatus = CashOutStatus.Unavailable,
                            CashOutStake = 0m
                        });

                        continue;
                    }

                    var isBreak = false;                   

                    foreach (var selection in selections)
                    {
                        if (selection.SportId > 1000 || selection.FixtureTypeId == (int)FixtureType.Outright)
                        {
                            isBreak = true;
                            break;
                        }
                    }

                    if (isBreak)
                    {
                        result.Add(new CashOutDetailsReponse
                        {
                            BetId = bet.BetId,
                            CashOutStatus = CashOutStatus.Unavailable,
                            CashOutStake = 0m
                        });
                        isBreak = false;
                        continue;
                    }

                    try
                    {
                        //var taxedStake = bet.TrueStake - (bet.TrueStake / (1 + bet.StakeTax));

                        var finalStake = bet.TrueStake - bet.StakeBonus;

                        var taxedStake = finalStake - (finalStake / (1 + bet.StakeTax));
                        var stake = bet.TrueStake - taxedStake;

                        var probabilites = new List<Probabilities>();
                        var hasProbabilityChanged = false;
                        var hasLive = false;

                        foreach (var selection in selections)
                        {
                            var selectionProbability = selectionProbabilities.Where(c => c.SelectionId == selection.SelectionId).FirstOrDefault();

                            if (selectionProbability == null || selectionProbability.MarketStatusId != (int)MarketStatus.Active || selectionProbability.IsActive == false || selectionProbability.IsSuspended == true)
                            {
                                isBreak = true;
                                break;
                            }

                            if (selectionProbability.FixtureStatusId == (int)FixtureStatus.Live)
                                hasLive = true;

                            if (selection.Probability != selectionProbability.Probability)
                                hasProbabilityChanged = true;

                            probabilites.Add(new Probabilities
                            {
                                BetSelectionId = selection.BetSelectionId,
                                CashOutStatusId = 1,
                                Probability = selectionProbability.Probability,
                                WonProbability = selectionProbability.WonProbability,
                                LostProbability = selectionProbability.LostProbability,
                                VoidProbability = selectionProbability.VoidProbability,
                                HalfWonProbability = selectionProbability.HalfWonProbability,
                                HalfLostProbability = selectionProbability.HalfLostProbability
                            });
                        }

                        if (isBreak)
                        {
                            result.Add(new CashOutDetailsReponse
                            {
                                BetId = bet.BetId,
                                CashOutStatus = CashOutStatus.Unavailable,
                                CashOutStake = 0m
                            });
                            isBreak = false;
                            continue;
                        }

                        if (selections.Count == 1)
                        {
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

                            var taxedWinnings = (cashOutStake - finalStake) * bet.WinningsTax;

                            if (cashOutStake > stake)
                                cashOutStake = cashOutStake - taxedWinnings;
                            else
                                taxedWinnings = 0;

                            var finalCashOutStake = cashOutStake;

                            if (!hasProbabilityChanged)
                                finalCashOutStake = stake - (stake * 0.10m);

                            if (finalCashOutStake <= stake * bet.Odds)
                            {
                                result.Add(new CashOutDetailsReponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStatus = CashOutStatus.Available,
                                    CashOutStake = finalCashOutStake,
                                    TaxedStake = taxedStake,
                                    TaxedWinnings = taxedWinnings,
                                    HasLive = hasLive,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        BetSelectionId = s.BetSelectionId,
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                });

                                continue;
                            }
                            else
                            {
                                result.Add(new CashOutDetailsReponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStatus = CashOutStatus.Unavailable,
                                    CashOutStake = 0m
                                });

                                continue;
                            }
                        }

                        if (selections.Count > 1)
                        {
                            var finalProbability = probabilites.Where(c => c.Probability > 0).Select(s => s.Probability).Aggregate((a, b) => a * b);

                            var cashOutStake = (stake * bet.Odds) * Convert.ToDecimal(finalProbability);

                            cashOutStake = cashOutStake - (cashOutStake * 0.10m);

                            var taxedWinnings = (cashOutStake - finalStake) * bet.WinningsTax;

                            if (cashOutStake > stake)
                                cashOutStake = cashOutStake - taxedWinnings;
                            else
                                taxedWinnings = 0;

                            var finalCashOutStake = cashOutStake;

                            if (!hasProbabilityChanged)
                                finalCashOutStake = stake - (stake * 0.10m);

                            if (finalCashOutStake == 0 || finalCashOutStake > stake * bet.Odds)
                            {
                                result.Add(new CashOutDetailsReponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStatus = CashOutStatus.Unavailable,
                                    CashOutStake = 0m
                                });

                                continue;
                            }
                            else
                            {
                                result.Add(new CashOutDetailsReponse
                                {
                                    BetId = bet.BetId,
                                    CashOutStatus = CashOutStatus.Available,
                                    CashOutStake = finalCashOutStake,
                                    TaxedStake = taxedStake,
                                    TaxedWinnings = taxedWinnings,
                                    HasLive = hasLive,
                                    Probabilities = probabilites.Select(s => new Probabilities
                                    {
                                        BetSelectionId = s.BetSelectionId,
                                        CashOutStatusId = s.CashOutStatusId,
                                        Probability = s.Probability,
                                        WonProbability = s.WonProbability,
                                        LostProbability = s.LostProbability,
                                        VoidProbability = s.VoidProbability,
                                        HalfWonProbability = s.HalfWonProbability,
                                        HalfLostProbability = s.HalfLostProbability
                                    }).ToList()
                                });
                            }
                        }
                    }
                    catch (Exception)
                    {
                        result.Add(new CashOutDetailsReponse
                        {
                            BetId = bet.BetId,
                            CashOutStatus = CashOutStatus.Unavailable,
                            CashOutStake = 0m
                        });
                    }
                }
            }

            return new BaseResponseDTO<List<CashOutDetailsReponse>>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString().Replace('_', ' '),
                Result = result
            };
        }
    }
}
