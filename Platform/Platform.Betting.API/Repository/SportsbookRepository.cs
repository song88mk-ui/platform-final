using Microsoft.Extensions.Configuration;
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
                        //var taxedStake = bet.TrueStake - (bet.TrueStake / (1 + bet.StakeTax));

                        var finalStake = bet.TrueStake - bet.StakeBonus;

                        var taxedStake = finalStake - (finalStake / (1 + bet.StakeTax));
                        var stake = bet.TrueStake - taxedStake;

                        var nonTaxedStake = bet.TrueStake;
                        var nonTaxedPayout = 0m;

                        var probabilites = new List<Probabilities>();
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

                            if ( selection.Probability != selectionProbability.Probability)
                                hasProbabilityChanged = true;


                            probabilites.Add(new Probabilities
                            {
                                BetSelectionId = selection.BetSelectionId,
                                IsLive = selectionProbability.FixtureStatusId == (int)FixtureStatus.Live,
                                CashOutStatusId = 1,
                                Probability = selectionProbability.Probability,
                                WonProbability = selectionProbability.WonProbability,
                                LostProbability = selectionProbability.LostProbability,
                                VoidProbability = selectionProbability.VoidProbability,
                                HalfWonProbability = selectionProbability.HalfWonProbability,
                                HalfLostProbability = selectionProbability.HalfLostProbability,
                                BetSelectionStatusId = selection.BetSelectionStatusId,
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
                                var won = 0m;
                                var halfWon = 0m;
                                var nonTaxedWon = 0m;
                                var nonTaxedHalfWon = 0m;

                                if (OddsTypeHelper.GetOddsByOddsType(bet.OddsTypeId, selections.First().Odds, selections.First().IsBinary) < 0 && (bet.OddsTypeId == (int)OddsType.Indonesian || bet.OddsTypeId == (int)OddsType.Malaysian))
                                {

                                    won = Convert.ToDecimal(probabilites.First().WonProbability) * (stake + bet.Stake);
                                    halfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * ((bet.Stake / 2) + stake);
                                    nonTaxedWon = Convert.ToDecimal(probabilites.First().WonProbability) * (bet.TrueStake + bet.Stake);
                                    nonTaxedHalfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * ((bet.Stake / 2) + stake);
                                }
                                else
                                {
                                    won = Convert.ToDecimal(probabilites.First().WonProbability) * (stake * bet.Odds);
                                    halfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * (stake * ((bet.Odds + 1) / 2));
                                    nonTaxedWon = Convert.ToDecimal(probabilites.First().WonProbability) * (nonTaxedStake * bet.Odds);
                                    nonTaxedHalfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * (nonTaxedStake * ((bet.Odds + 1) / 2));
                                }

                                var lost = Convert.ToDecimal(probabilites.First().LostProbability) * (stake * 0);
                                var voidd = Convert.ToDecimal(probabilites.First().VoidProbability) * (stake * 1);
                                var halfLost = Convert.ToDecimal(probabilites.First().HalfLostProbability) * (stake / 2);

                                cashOutStake = won + lost + voidd + halfWon + halfLost;

                                var nonTaxedLost = Convert.ToDecimal(probabilites.First().LostProbability) * (nonTaxedStake * 0);
                                var nonTaxedVoidd = Convert.ToDecimal(probabilites.First().VoidProbability) * (nonTaxedStake * 1);
                                var nonTaxedHalfLost = Convert.ToDecimal(probabilites.First().HalfLostProbability) * (nonTaxedStake / 2);

                                nonTaxedPayout = nonTaxedWon + nonTaxedLost + nonTaxedVoidd + nonTaxedHalfWon + nonTaxedHalfLost;

                            }
                            else
                            {
                                if (OddsTypeHelper.GetOddsByOddsType(bet.OddsTypeId, selections.First().Odds, selections.First().IsBinary) < 0 && (bet.OddsTypeId == (int)OddsType.Indonesian || bet.OddsTypeId == (int)OddsType.Malaysian))
                                {
                                    cashOutStake = (stake + bet.Stake) * Convert.ToDecimal(probabilites.First().Probability);
                                    nonTaxedPayout = (bet.TrueStake * bet.Stake) * Convert.ToDecimal(probabilites.First().Probability);
                                }
                                else
                                {
                                    cashOutStake = (stake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);
                                    nonTaxedPayout = (nonTaxedStake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);
                                }
                            }

                            cashOutStake = cashOutStake - (cashOutStake * 0.10m);

                            nonTaxedPayout = nonTaxedPayout - (nonTaxedPayout * 0.10m);

                            var taxedWinnings = (cashOutStake - finalStake) * bet.WinningsTax;

                            if (cashOutStake > stake)
                                cashOutStake = cashOutStake - taxedWinnings;
                            else
                                taxedWinnings = 0;

                            var finalCashOutStake = cashOutStake;

                            if (!hasProbabilityChanged)
                            {
                                finalCashOutStake = stake - (stake * 0.10m);

                                nonTaxedPayout = nonTaxedStake - (nonTaxedStake * 0.10m);
                            }

                            if (OddsTypeHelper.GetOddsByOddsType(bet.OddsTypeId, selections.First().Odds, selections.First().IsBinary) < 0 && (bet.OddsTypeId == (int)OddsType.Indonesian || bet.OddsTypeId == (int)OddsType.Malaysian))
                            {
                                if (finalCashOutStake <= stake + bet.Stake)
                                {
                                    result.Add(new CashOutDetailsReponse
                                    {
                                        BetId = bet.BetId,
                                        CashOutStatus = CashOutStatus.Available,
                                        CashOutStake = finalCashOutStake,
                                        TaxedStake = taxedStake,
                                        TaxedWinnings = taxedWinnings,
                                        NonTaxedPayout = nonTaxedPayout,
                                        HasLive = hasLive,
                                        Probabilities = probabilites.Select(s => new Probabilities
                                        {
                                            BetSelectionId = s.BetSelectionId,
                                            IsLive = s.IsLive,
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
                            else
                            {
                                if (finalCashOutStake <= stake * bet.Odds)
                                {
                                    result.Add(new CashOutDetailsReponse
                                    {
                                        BetId = bet.BetId,
                                        CashOutStatus = CashOutStatus.Available,
                                        CashOutStake = finalCashOutStake,
                                        TaxedStake = taxedStake,
                                        TaxedWinnings = taxedWinnings,
                                        NonTaxedPayout = nonTaxedPayout,
                                        HasLive = hasLive,
                                        Probabilities = probabilites.Select(s => new Probabilities
                                        {
                                            BetSelectionId = s.BetSelectionId,
                                            IsLive = s.IsLive,
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
                        }

                        if (selections.Count > 1)
                        {
                            var parlayProbabilities = new List<Probabilities>();
                            var validStatus = new List<int> { (int)BetStatus.Won, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost, (int)BetStatus.Void };
                            foreach (var probability in probabilites)
                            {
                                if (validStatus.Contains(probability.BetSelectionStatusId))
                                {
                                    probability.Probability = 1;
                                    parlayProbabilities.Add(probability);
                                }
                                else
                                {
                                    parlayProbabilities.Add(probability);
                                }

                            }

                            var finalProbability = parlayProbabilities.Where(c => c.Probability > 0).Select(s => s.Probability).Aggregate((a, b) => a * b);

                            var totalOdds = GetTotalOdds(selections.Select(s => new Tuple<BetStatus, decimal, decimal>((BetStatus)s.BetSelectionStatusId, s.Odds, s.DeadHeatFactor)).ToList());

                            var cashOutStake = (stake * totalOdds) * Convert.ToDecimal(finalProbability);

                            cashOutStake = cashOutStake - (cashOutStake * 0.10m);

                            nonTaxedPayout = (nonTaxedStake * totalOdds) * Convert.ToDecimal(finalProbability);

                            nonTaxedPayout = nonTaxedPayout - (nonTaxedPayout * 0.10m);

                            var taxedWinnings = (cashOutStake - finalStake) * bet.WinningsTax;

                            if (cashOutStake > stake)
                                cashOutStake = cashOutStake - taxedWinnings;
                            else
                                taxedWinnings = 0;

                            var finalCashOutStake = cashOutStake;

                            if (!hasProbabilityChanged)
                            {
                                finalCashOutStake = stake - (stake * 0.10m);

                                nonTaxedPayout = nonTaxedStake - (nonTaxedStake * 0.10m);
                            }

                            if (finalCashOutStake == 0 || finalCashOutStake > stake * totalOdds)
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
                                    NonTaxedPayout = nonTaxedPayout,
                                    HasLive = hasLive,
                                    Probabilities = parlayProbabilities,
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

        private decimal GetTotalOdds(List<Tuple<BetStatus, decimal, decimal>> selections)
        {
            var values = new List<decimal>();

            foreach (var selection in selections)
            {
                switch (selection.Item1)
                {
                    case BetStatus.Lost:
                        values.Add(0m);
                        break;
                    case BetStatus.HalfWon:
                        values.Add((selection.Item2 + 1) / 2);
                        break;
                    case BetStatus.HalfLost:
                        values.Add(0.5m);
                        break;
                    case BetStatus.Won:
                        values.Add(selection.Item2 * selection.Item3);
                        break;
                    case BetStatus.Void:
                        values.Add(1m);
                        break;
                    case BetStatus.Cancelled:
                        values.Add(1m);
                        break;
                    case BetStatus.Running:
                        values.Add(selection.Item2);
                        break;
                }
            }

            return values.Aggregate((a, b) => a * b);
        }
    }
}
