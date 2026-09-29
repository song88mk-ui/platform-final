using Microsoft.Extensions.Configuration;
using Platform.Shared.Calculators;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using Platform.Shared.Models;
using Platform.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SportsbookRepository(HttpClient httpClient, IConfiguration configuration)
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
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetSelectionProbabilityResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetSelectionProbabilityResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
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

        public async Task<BaseResponseDTO<List<CashOutDetailsReponse>>> GetCashOut(List<Bet> bets, List<BetSelection> betSelections, List<Guid> boostedList)
        {
            var result = new List<CashOutDetailsReponse>();

            var tempSelections = betSelections.Where(c => c.FixtureTypeId == (int)FixtureType.Match && !c.SportId.IsVirtualSportId()).Select(s => new Tuple<long, long, long>(s.FixtureId, s.MarketId, s.SelectionId)).ToList();

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

                if (bet.BetTypeId == (int)BetType.BetBuilder)
                {
                    result.Add(new CashOutDetailsReponse
                    {
                        BetId = bet.BetId,
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    });

                    continue;
                }

                if (boostedList != null && boostedList.Any(b => b == bet.BetId))
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
                        if (selection.SportId.IsVirtualSportId() || selection.FixtureTypeId == (int)FixtureType.Outright)
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
                        // The stake/tax breakdown and the value formula itself now live in
                        // CashOutCalculator (Platform.Shared) - see the call below. This block
                        // only determines eligibility and collects each selection's probability.
                        var probabilites = new List<CashOutSelectionInput>();
                        var hasProbabilityChanged = false;
                        var hasLive = false;

                        var marketStatus = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Settled };

                        var tempSelectionIds = selections.Select(s => s.SelectionId).ToList();
                        var betStopCount = selectionProbabilities.Where(c => tempSelectionIds.Contains(c.SelectionId) && c.IsSuspended == true).Count();
                        var inactiveFixtureCount = selectionProbabilities.Where(c => tempSelectionIds.Contains(c.SelectionId) && c.FixtureStatusId > 1).Count();

                        foreach (var selection in selections)
                        {
                            var selectionProbability = selectionProbabilities.Where(c => c.SelectionId == selection.SelectionId).FirstOrDefault();

                            if (selections.Count == betStopCount || selections.Count == inactiveFixtureCount)
                            {
                                isBreak = true;
                                break;
                            }

                            if (selections.Count == 1)
                            {
                                if (selectionProbability == null
                                    || selectionProbability.IsActive == false
                                    || selectionProbability.MarketStatusId != (int)MarketStatus.Active)
                                {
                                    isBreak = true;
                                    break;
                                }
                            }

                            if (selectionProbability == null
                                || selectionProbability.SelectionStatusId == (int)BetStatus.Lost
                                || selectionProbability.SelectionStatusId == (int)BetStatus.Cancelled
                                || (selectionProbability.IsActive == false && selectionProbability.MarketStatusId == (int)MarketStatus.Active)
                                || selectionProbability.IsGlobalBetStop == true
                                || (selectionProbability.IsSuspended == true && selectionProbability.FixtureStatusId < 2
                                || (selectionProbability.IsSuspended == true && (selectionProbability.FixtureStatusId < 2 || selectionProbability.FixtureStatusId == 4) && selectionProbability.MarketStatusId == (int)MarketStatus.Active)))
                            {
                                isBreak = true;
                                break;
                            }

                            if (!marketStatus.Contains(selectionProbability.MarketStatusId))
                            {
                                isBreak = true;
                                break;
                            }

                            if (selectionProbability.Probability == 0
                                && selectionProbability.WonProbability == 0
                                && selectionProbability.LostProbability == 0
                                && selectionProbability.VoidProbability == 0
                                && selectionProbability.HalfWonProbability == 0
                                && selectionProbability.HalfLostProbability == 0)
                            {
                                isBreak = true;
                                break;
                            }

                            if (selectionProbability.FixtureStatusId == (int)FixtureStatus.Live)
                                hasLive = true;

                            if (selection.Probability != selectionProbability.Probability)
                                hasProbabilityChanged = true;


                            probabilites.Add(new CashOutSelectionInput
                            {
                                BetSelectionId = selection.BetSelectionId,
                                IsLive = selectionProbability.FixtureStatusId == (int)FixtureStatus.Live,
                                Price = selectionProbability.Price,
                                Probability = selectionProbability.Probability,
                                WonProbability = selectionProbability.WonProbability,
                                LostProbability = selectionProbability.LostProbability,
                                VoidProbability = selectionProbability.VoidProbability,
                                HalfWonProbability = selectionProbability.HalfWonProbability,
                                HalfLostProbability = selectionProbability.HalfLostProbability,
                                BetSelectionStatusId = selection.BetSelectionStatusId,
                                Odds = selection.Odds,
                                DeadHeatFactor = selection.DeadHeatFactor,
                                IsBinary = selection.IsBinary
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

                        var calculation = CashOutCalculator.Calculate(new CashOutCalculationInput
                        {
                            TrueStake = bet.TrueStake,
                            Stake = bet.Stake,
                            StakeBonus = bet.StakeBonus,
                            StakeTax = bet.StakeTax,
                            WinningsTax = bet.WinningsTax,
                            OddsTypeId = bet.OddsTypeId,
                            Odds = bet.Odds,
                            HasProbabilityChanged = hasProbabilityChanged,
                            HasLive = hasLive,
                            Selections = probabilites
                        });

                        if (calculation.CashOutStatus != CashOutStatus.Available)
                        {
                            result.Add(new CashOutDetailsReponse
                            {
                                BetId = bet.BetId,
                                CashOutStatus = CashOutStatus.Unavailable,
                                CashOutStake = 0m
                            });
                        }
                        else
                        {
                            result.Add(new CashOutDetailsReponse
                            {
                                BetId = bet.BetId,
                                CashOutStatus = CashOutStatus.Available,
                                CashOutStake = calculation.CashOutStake,
                                TaxedStake = calculation.TaxedStake,
                                TaxedWinnings = calculation.TaxedWinnings,
                                NonTaxedPayout = calculation.NonTaxedPayout,
                                HasLive = calculation.HasLive,
                                Probabilities = calculation.Probabilities
                            });
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
        public async Task<BaseResponseDTO<BetSettlementScoreResponseDTO>> GetBetSettlementScore(BetSettlementScoreRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/betsettlementscore", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetSettlementScoreResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetSettlementScoreResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetSettlementScoreResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<BetRecommendationsResponseDTO>> GetBetRecommendationDetails(BetRecommendationDetailRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/betrecommendations", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetRecommendationsResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetRecommendationsResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<BetRecommendationsResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetRecommendationsResponseDTO>()
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}
