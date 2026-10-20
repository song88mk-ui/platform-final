using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.GTS;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using Platform.Shared.Data.Betting.Entities;

namespace Platform.Shared.Repositories
{
    public class GTSRepository : IGTSRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly ILogger<GTSRepository> _logger;
        private readonly ActivitySource _activitySource;

        public GTSRepository(HttpClient httpClient, IConfiguration configuration, ILogger<GTSRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:GTSAPI"]);
            _activitySource = new ActivitySource("GTS.Validation");
        }

        [Obsolete("Please use GetMaxStake2 since getmaxstake doesnt support currency")]
        public async Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStakeOld(string authToken, BetRequestDTO request)
        {
            using (var activity = _activitySource.StartActivity("gts.maxstake.old"))
            {
                activity?.SetTag("provider.enabled", _configuration["Custom:IsProviderEnabled"]);

                if (Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]) == false)
                {
                    return new BaseResponseDTO<ProviderStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderStakeResponseDTO
                        {
                            Stake = 0m
                        }
                    };
                }

                if (request.Selections.Any(c => c.SportId.IsVirtualSportId()))
                {
                    activity?.SetTag("gts.virtual.sports", true);
                    return new BaseResponseDTO<ProviderStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderStakeResponseDTO
                        {
                            Stake = 0m
                        }
                    };
                }

                activity?.SetTag("gts.auth.token", authToken);

                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var maxStakePayload = new
                    {
                        playerId = request.UserId,
                        legs = request.Selections.Select(s => new
                        {
                            selectionId = s.SelectionTemplateId, //TODO: double check the value
                            gameState = GetGameState((int)s.FixtureId),
                            price = s.DecimalOdds
                        }).ToList()
                    };

                    activity?.SetTag("gts.maxstake.payload", maxStakePayload);

                    var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/max-allowed-stake", new StringContent(JsonSerializer.Serialize(maxStakePayload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<GTSMaxStakeResponseDTO>(await response.Content.ReadAsStringAsync(), _jsonOptions);

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        return new BaseResponseDTO<ProviderStakeResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ProviderStakeResponseDTO
                            {
                                Stake = initialResult.MaxAllowedStake, //TODO: convert based on currency
                            }
                        };
                    }
                    else
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        return new BaseResponseDTO<ProviderStakeResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ProviderStakeResponseDTO
                            {
                                Stake = 0
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                    return new BaseResponseDTO<ProviderStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new ProviderStakeResponseDTO
                        {
                            Stake = 0
                        }
                    };
                }
            } // activity
        }

        public async Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStake(string authToken, BetRequestDTO request)
        {
            using (var activity = _activitySource.StartActivity("gts.maxstake"))
            {
                activity?.SetTag("provider.enabled", _configuration["Custom:IsProviderEnabled"]);

                if (Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]) == false)
                {
                    return new BaseResponseDTO<ProviderStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderStakeResponseDTO
                        {
                            Stake = 0m
                        }
                    };
                }

                if (request.Selections.Any(c => c.SportId.IsVirtualSportId()))
                {
                    activity?.SetTag("gts.virtual.sports", true);
                    return new BaseResponseDTO<ProviderStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderStakeResponseDTO
                        {
                            Stake = 0m
                        }
                    };
                }

                activity?.SetTag("gts.auth.token", authToken);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = request.LimitId; //int.Parse(_configuration["Custom:GTSBookmakerId"]);

                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var totalStake = request.Stake; //TODO: compute true stake

                    var payload = new
                    {
                        betId = request.BetId.ToString().ToUpper(),
                        playerId = request.UserId,
                        totalStake = totalStake,
                        currencyCode = request.CurrencyCode.ToString(),
                        legs = request.Selections.Select(s => new
                        {
                            selectionId = s.SelectionSpecifier,
                            gameState = GetGameState((int)s.FixtureId),
                            price = s.DecimalOdds
                        }).ToList(),
                        priceChangeRule = "AcceptNone"
                    };

                    activity?.SetTag("gts.maxstake.payload", payload);

                    var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/assessment", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(await response.Content.ReadAsStringAsync(), _jsonOptions);

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        return new BaseResponseDTO<ProviderStakeResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ProviderStakeResponseDTO
                            {
                                Stake = initialResult.MaxAllowedStake
                            }
                        };
                    }
                    else
                    {
                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        return new BaseResponseDTO<ProviderStakeResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ProviderStakeResponseDTO
                            {
                                Stake = 0
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError($"GetMaxStake Error: {ex.Message}-{ex.StackTrace}");

                    return new BaseResponseDTO<ProviderStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new ProviderStakeResponseDTO
                        {
                            Stake = 0
                        }
                    };
                }
            } // activity
        }

        public async Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> Assessment(string authToken, OnBetPlacement request, string testPrefix = "")
        {
            using (var activity = _activitySource.StartActivity("gts.assessment"))
            {
                activity?.SetTag("provider.enabled", _configuration["Custom:IsProviderEnabled"]);

                if (Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]) == false)
                {
                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = 0m,
                            IsBetAllowed = true
                        }
                    };
                }

                if (request.BetSelections.Any(c => c.BetSelection.SportId.IsVirtualSportId()))
                {
                    activity?.SetTag("gts.virtual.sports", true);
                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = 0m,
                            IsBetAllowed = true
                        }
                    };
                }

                activity?.SetTag("gts.auth.token", authToken);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = request.Bet.LimitId; //int.Parse(_configuration["Custom:GTSBookmakerId"]);

                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var totalStake = ConvertToEur(request.Bet.TrueStake, request.Bet.ExchangeRate);

                    var payload = new
                    {
                        betId = $"{testPrefix}{request.Bet.BetId.ToString().ToUpper()}",
                        playerId = request.Bet.UserId,
                        totalStake = totalStake,
                        currencyCode = "EUR",
                        legs = request.BetSelections.Select(s => new
                        {
                            selectionId = s.BetSelection.SelectionSpecifier.Split(':').LastOrDefault(),
                            gameState = GetGameState(s.BetSelection.FixtureStatusId),
                            price = s.BetSelection.Odds
                        }).ToList(),
                        priceChangeRule = GetPriceChangeRule(request.OddsChangeType)
                    };

                    activity?.SetTag("gts.assessment.payload", payload);
                    _logger.LogInformation($"Assessment Payload: {JsonSerializer.Serialize(payload)}");

                    var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/assessment", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(await response.Content.ReadAsStringAsync(), _jsonOptions);

                        if (initialResult.IsBetAllowed == false)
                            _logger.LogWarning($"Assessment Result: {JsonSerializer.Serialize(initialResult)}");

                        activity?.SetTag("gts.assessment.result", initialResult);
                        activity?.SetStatus(ActivityStatusCode.Ok);

                        return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ProviderAssessmentResponseDTO
                            {
                                Stake = initialResult.MaxAllowedStake,
                                IsBetAllowed = initialResult.IsBetAllowed,
                                IsPending = initialResult.BetAssessmentResult == "AssessmentDelayed",
                                Message = initialResult.IsBetAllowed == false ?
                                    initialResult.Legs?.FirstOrDefault(l => l.RejectReason != null)?.RejectReason?.ReasonMessage ?? string.Empty :
                                    string.Empty
                            }
                        };
                    }
                    else
                    {
                        var responseError = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Assessment Error: {responseError}");

                        activity?.SetStatus(ActivityStatusCode.Error, responseError);

                        return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({responseError})",
                            Result = new ProviderAssessmentResponseDTO
                            {
                                Stake = 0,
                                Message = $"1027 - Failed sending Bet to GTS ({responseError})"
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Assessment Exception: {ex.Message}-{ex.StackTrace}");

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = 0,
                            Message = $"1025 - Failed sending Bet to GTS ({ex.Message})"
                        }
                    };
                }
            } // activity
        }


        // Cash-out eligibility via Bet Assessment API (GTS Back-End Integration doc):
        // same message as a bet assessment, but betId gets a "cashout-" prefix, totalStake is
        // a default unit of 1, and legs settled before the cashout are excluded.
        // gameState per leg: the stored BetSelection.FixtureStatusId is placement-time and goes
        // stale when a prematch fixture goes live, so prefer the quote-time per-leg liveness in
        // `probabilities` (matched by BetSelectionId); fall back to the bet-level isInPlay flag,
        // then to the stored fixture status.
        // price per leg: same preference — the quote-time current price from `probabilities`
        // (what the confirmed offer was computed from), falling back to placement-time odds.
        public async Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> CashoutAssessment(string authToken, OnBetSettlement request, string testPrefix = "", bool isInPlay = false, List<CashedOutProbabilities> probabilities = null)
        {
            using (var activity = _activitySource.StartActivity("gts.cashout.assessment"))
            {
                activity?.SetTag("provider.enabled", _configuration["Custom:IsProviderEnabled"]);

                if (Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]) == false)
                {
                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = 0m,
                            IsBetAllowed = true
                        }
                    };
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = request.Bet.LimitId;

                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    // Legs already settled (won/lost/void/...) before the cashout must be excluded;
                    // legs cashed out now were Running until this cashout, so they are included.
                    var activeSelections = request.BetSelections
                        .Where(s => s.BetSelectionStatusId == (int)BetStatus.Running
                                 || s.BetSelectionStatusId == (int)BetStatus.CashedOut)
                        .ToList();

                    var payload = new
                    {
                        betId = $"cashout-{testPrefix}{request.Bet.BetId.ToString().ToUpper()}",
                        playerId = request.Bet.UserId,
                        totalStake = 1m,
                        currencyCode = "EUR",
                        legs = activeSelections.Select(s => new
                        {
                            selectionId = s.SelectionSpecifier.Split(':').LastOrDefault(),
                            gameState = GetCashoutGameState(s, isInPlay, probabilities),
                            price = GetCashoutPrice(s, probabilities)
                        }).ToList(),
                        priceChangeRule = "AcceptAny"
                    };

                    activity?.SetTag("gts.cashout.assessment.payload", payload);
                    _logger.LogInformation($"CashoutAssessment Payload: {JsonSerializer.Serialize(payload)}");

                    var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/assessment", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(await response.Content.ReadAsStringAsync(), _jsonOptions);

                        if (initialResult.IsBetAllowed == false)
                            _logger.LogWarning($"CashoutAssessment Result: {JsonSerializer.Serialize(initialResult)}");

                        activity?.SetTag("gts.cashout.assessment.result", initialResult);
                        activity?.SetStatus(ActivityStatusCode.Ok);

                        return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new ProviderAssessmentResponseDTO
                            {
                                Stake = initialResult.MaxAllowedStake,
                                IsBetAllowed = initialResult.IsBetAllowed,
                                IsPending = initialResult.BetAssessmentResult == "AssessmentDelayed",
                                Message = initialResult.IsBetAllowed == false ?
                                    initialResult.Legs?.FirstOrDefault(l => l.RejectReason != null)?.RejectReason?.ReasonMessage ?? string.Empty :
                                    string.Empty
                            }
                        };
                    }
                    else
                    {
                        var responseError = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"CashoutAssessment Error: {responseError}");

                        activity?.SetStatus(ActivityStatusCode.Error, responseError);

                        return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({responseError})",
                            Result = new ProviderAssessmentResponseDTO
                            {
                                Stake = 0,
                                Message = $"1027 - Failed sending Cashout Assessment to GTS ({responseError})"
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"CashoutAssessment Exception: {ex.Message}-{ex.StackTrace}");

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = 0,
                            Message = $"1025 - Failed sending Cashout Assessment to GTS ({ex.Message})"
                        }
                    };
                }
            } // activity
        }

        public async Task<BaseResponseDTO<string>> SendBet(string authToken, SendBet request, string testPrefix = "", bool isSettled = false, GtsCashout cashout = null)
        {
            using (var activity = _activitySource.StartActivity("gts.bet.placement"))
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = request.Bet.LimitId; //int.Parse(_configuration["Custom:GTSBookmakerId"]);
                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var totalStake = ConvertToEur(request.Bet.TrueStake, request.Bet.ExchangeRate);
                    var updateDate = isSettled ? DateTime.UtcNow : request.Bet.BetDate;

                    var legs = request.BetSelections.Select(s => new
                    {
                        status = GetGtsSelectionStatus(s.BetSelectionStatusId),
                        gameState = GetGameState((int)s.FixtureStatusId),
                        price = s.Odds,
                        payoutPrice = isSettled ? GetSelectionPayout(s.BetSelectionStatusId, s.Odds, request.Bet, request.BetSelections.Count == 1) : null,
                        betGeniusContent = new
                        {
                            selectionId = s.SelectionSpecifier.Split(':').LastOrDefault()
                        }
                    }).ToList();

                    object payload = cashout == null
                        ? (object)new
                        {
                            id = $"{testPrefix}{request.Bet.BetId.ToString().ToUpper()}",
                            betPlacedTimestampUTC = request.Bet.BetDate,
                            betUpdatedTimestampUTC = DateTime.UtcNow,
                            bookMakerName = request.Bet.ClientCode,
                            playerId = request.Bet.UserId,
                            totalStake = totalStake,
                            currencyCode = "EUR",
                            status = GetGtsBetStatus(request.Bet.BetStatusId),
                            payout = isSettled ? (decimal?)(ConvertToEur(request.Bet.Payout, request.Bet.ExchangeRate)) : null,
                            legs = legs,
                        }
                        : new
                        {
                            id = $"{testPrefix}{request.Bet.BetId.ToString().ToUpper()}",
                            betPlacedTimestampUTC = request.Bet.BetDate,
                            betUpdatedTimestampUTC = DateTime.UtcNow,
                            bookMakerName = request.Bet.ClientCode,
                            playerId = request.Bet.UserId,
                            totalStake = totalStake,
                            currencyCode = "EUR",
                            status = GetGtsBetStatus(request.Bet.BetStatusId),
                            payout = isSettled ? (decimal?)(ConvertToEur(request.Bet.Payout, request.Bet.ExchangeRate)) : null,
                            legs = legs,
                            cashedOutTotalStake = ConvertToEur(cashout.CashedOutTotalStake, request.Bet.ExchangeRate),
                            cashedOutAmount = ConvertToEur(cashout.CashedOutAmount, request.Bet.ExchangeRate),
                        };

                    activity?.SetTag("gts.bet.placement.payload", payload);
                    _logger.LogInformation($"SendBet Payload: {payload}");

                    var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/bets", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        var initialResult = string.Empty;
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            // Handle empty response explicitly
                            initialResult = null;
                        }
                        else
                        {
                            initialResult = JsonSerializer.Deserialize<string>(
                                content,
                                _jsonOptions
                            );
                        }

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        return new BaseResponseDTO<string>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = initialResult
                        };
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"SendBet Error Response: {error}");

                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        return new BaseResponseDTO<string>
                        {
                            StatusCode = StatusCode.Error,
                            Message = StatusCode.Error.ToString(),
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("SendBet Exception: {@exception}", ex);
                    _logger.LogError("SendBet StackTrace: {@stackTrace}", ex.StackTrace);

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                    return new BaseResponseDTO<string>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    };
                }
            } // activity
        }

        public async Task<BaseResponseDTO<string>> Cancellation(string authToken, Guid betId, string limitId, string clientCode, string testPrefix)
        {
            _logger.LogInformation($"Cancellation Start: {betId}");

            using (var activity = _activitySource.StartActivity("gts.bet.cancellation"))
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = limitId; //int.Parse(_configuration["Custom:GTSBookmakerId"]);

                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var payload = new
                    {
                        betId = betId.ToString().ToUpper()
                    };

                    activity?.SetTag("gts.bet.cancellation.payload", payload);

                    var response = await _httpClient.PostAsync($"v1/platforms/{clientCode}/bookmakers/{bookmakerId}/bet-reservation-cancellations", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        var initialResult = string.Empty;
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            // Handle empty response explicitly
                            initialResult = null;
                        }
                        else
                        {
                            initialResult = JsonSerializer.Deserialize<string>(
                                content,
                                _jsonOptions
                            );
                        }
                        activity?.SetStatus(ActivityStatusCode.Ok);

                        _logger.LogInformation($"Cancellation Success: {initialResult}");
                        return new BaseResponseDTO<string>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = initialResult
                        };
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Cancellation Error: {error}");

                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        return new BaseResponseDTO<string>
                        {
                            StatusCode = StatusCode.Error,
                            Message = StatusCode.Error.ToString(),
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Cancellation Exception: {ex.Message}-{ex.StackTrace}");

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                    return new BaseResponseDTO<string>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    };
                }
            } // using
        }

        private string GetGtsBetStatus(int statusId) => statusId switch
        {
            var s when s <= (int)BetStatus.Running => Enums.Provider.GTS.BetStatus.Open.ToString(),
            var s when s == (int)BetStatus.Cancelled || s == (int)BetStatus.Rejected => Enums.Provider.GTS.BetStatus.Cancelled.ToString(),
            _ => Enums.Provider.GTS.BetStatus.Settled.ToString()
        };

        // Leg-level status: per Genius Sports guidance, cashed-out legs are not updated —
        // they stay Open (with payoutPrice null); the cashout is carried by the bet-level
        // status/cashedOutTotalStake/cashedOutAmount/payout fields.
        private string GetGtsSelectionStatus(int statusId) => statusId == (int)BetStatus.CashedOut
            ? Enums.Provider.GTS.BetStatus.Open.ToString()
            : GetGtsBetStatus(statusId);

        private string GetGameState(int fixtureStatusId) => fixtureStatusId switch
        {
            var s when s == 1 => "InPlay",
            _ => "PreMatch"
        };

        // Per-leg gameState for the cashout assessment: quote-time liveness (exact, per leg)
        // wins; without it, fall back to the bet-level in-play flag, then the stored
        // placement-time fixture status.
        private string GetCashoutGameState(BetSelection selection, bool isInPlay, List<CashedOutProbabilities> probabilities)
        {
            var probability = probabilities?.FirstOrDefault(p => p.BetSelectionId == selection.BetSelectionId);

            if (probability != null)
                return probability.IsLive ? "InPlay" : "PreMatch";

            return isInPlay ? "InPlay" : GetGameState(selection.FixtureStatusId);
        }

        // Per-leg price for the cashout assessment: the quote-time current price (the price
        // the confirmed offer was computed from) wins; without it, fall back to the stored
        // placement-time odds. GTS validates the submitted prices against its own current
        // prices (bookmaker price-variation limit), so placement odds structurally fail for
        // in-play legs once the market has drifted past the limit.
        private decimal GetCashoutPrice(BetSelection selection, List<CashedOutProbabilities> probabilities)
        {
            var probability = probabilities?.FirstOrDefault(p => p.BetSelectionId == selection.BetSelectionId);

            return probability?.Price > 0 ? probability.Price.Value : selection.Odds;
        }

        private string GetPriceChangeRule(OddsChangeType oddsChangeType)
        {
            return oddsChangeType switch
            {
                OddsChangeType.None => "AcceptNone",
                OddsChangeType.Any => "AcceptAny",
                OddsChangeType.Higher => "AcceptHigher",
                _ => "AcceptNone" // safe default
            };
        }

        private decimal? GetSelectionPayout(int status, decimal odds, Bet bet, bool isSingleBet)
        {
            var stakePart = bet.Stake;
            var isBoosted = bet.BetTypeId == (int)BetType.PriceBoost;
            var trueStake = 1;
            var oddsType = bet.OddsTypeId;
            var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder;

            if (isBetBuilder)
                return trueStake;

            return status switch
            {
                (int)BetStatus.Lost => 0m,
                (int)BetStatus.HalfWon => ((odds - 1) / 2) + 1,
                (int)BetStatus.HalfLost => 0.5m,
                (int)BetStatus.Won => odds,
                (int)BetStatus.Void => 1,
                (int)BetStatus.Cancelled => 1,
                // Cashout: per Genius Sports guidance, cashed-out legs keep payoutPrice null —
                // the cashout is reported only through the bet-level fields.
                (int)BetStatus.CashedOut => (decimal?)null,
                _ => 0m
            };
        }

        private decimal ConvertToEur(decimal amount, decimal exchangeRate)
        {
            if (exchangeRate == 0)
                exchangeRate = 1;

            return amount / exchangeRate;
        }
    }

    public class GTSMaxStakeResponseDTO
    {
        [JsonPropertyName("maxAllowedStake")]
        public decimal MaxAllowedStake { get; set; }
    }

    public class GTSAssessmentResponseDTO
    {
        [JsonPropertyName("isBetAllowed")]
        public bool IsBetAllowed { get; set; }

        [JsonPropertyName("maxAllowedStake")]
        public decimal MaxAllowedStake { get; set; }

        [JsonPropertyName("betDelay")]
        public string? BetDelay { get; set; }

        [JsonPropertyName("betId")]
        public string BetId { get; set; }

        [JsonPropertyName("betAssessmentResult")]
        public string BetAssessmentResult { get; set; }

        [JsonPropertyName("legs")]
        public List<BetLeg> Legs { get; set; } = new List<BetLeg>();
    }

    public class BetLeg
    {
        [JsonPropertyName("selectionId")]
        public string SelectionId { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("selectionStatus")]
        public string SelectionStatus { get; set; }

        [JsonPropertyName("rejectReason")]
        public RejectReason RejectReason { get; set; }
    }

    public class RejectReason
    {
        [JsonPropertyName("reasonCode")]
        public string ReasonCode { get; set; }

        [JsonPropertyName("reasonMessage")]
        public string ReasonMessage { get; set; }
    }
}
