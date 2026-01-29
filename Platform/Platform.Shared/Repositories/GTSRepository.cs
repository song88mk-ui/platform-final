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

namespace Platform.Shared.Repositories
{
    public class GTSRepository : IGTSRepository
    {
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
            using(var activity = _activitySource.StartActivity("gts.maxstake.old"))
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
                        var initialResult = JsonSerializer.Deserialize<GTSMaxStakeResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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
            using(var activity = _activitySource.StartActivity("gts.maxstake"))
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
                    var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

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
                        var initialResult = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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
                    _logger.LogError("GetMaxStake Error: {@exception}", ex);

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

        public async Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> Assessment(string authToken, OnBetPlacement request)
        {
            using(var activity = _activitySource.StartActivity("gts.assessment"))
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
                    var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var totalStake = request.Bet.TrueStake;

                    var payload = new
                    {
                        betId = request.Bet.BetId.ToString().ToUpper(),
                        playerId = request.Bet.UserId,
                        totalStake = totalStake,
                        currencyCode = request.Bet.CurrencyCode,
                        legs = request.BetSelections.Select(s => new
                        {
                            selectionId = s.BetSelection.SelectionSpecifier.Split(':').LastOrDefault(),
                            gameState = GetGameState(s.BetSelection.FixtureStatusId),
                            price = s.BetSelection.Odds
                        }).ToList(),
                        priceChangeRule = "AcceptNone"
                    };

                    activity?.SetTag("gts.assessment.payload", payload);
                    _logger.LogInformation($"Assessment Payload: {JsonSerializer.Serialize(payload)}");

                    var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/assessment", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        if(initialResult.IsBetAllowed == false)
                            _logger.LogError($"Assessment Result: {initialResult}");

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
                                    initialResult.Legs?.FirstOrDefault(l => l.RejectReason != null).RejectReason.ReasonMessage :
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
                                Message = $"1026 - Failed sending Bet to GTS ({responseError})"
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Assessment Exception: {JsonSerializer.Serialize(ex)}");

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = 0,
                            Message = "1026 - Failed sending Bet to GTS"
                        }
                    };
                }
            } // activity
        }


        public async Task<BaseResponseDTO<string>> SendBet(string authToken, SendBet request)
        {
            using(var activity = _activitySource.StartActivity("gts.bet.placement"))
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);
                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var totalStake = request.Bet.TrueStake;

                    var payload = new
                    {
                        id = request.Bet.BetId.ToString().ToUpper(),
                        betPlacedTimestampUTC = DateTime.UtcNow,
                        betUpdatedTimestampUTC = DateTime.UtcNow,
                        bookMakerName = "Song88",
                        playerId = request.Bet.UserId,
                        totalStake = totalStake,
                        currencyCode = request.Bet.CurrencyCode,
                        status = GetGtsBetStatus(request.Bet.BetStatusId),
                        //	Represents a bet with any legs that are currently Open
                        legs = request.BetSelections.Select(s => new
                        {
                            status = GetGtsBetStatus(s.BetSelectionStatusId),
                            gameState = GetGameState((int)s.FixtureId),
                            price = s.Odds,
                            betGeniusContent = new
                            {
                                selectionId = s.SelectionSpecifier.Split(':').LastOrDefault()
                            }
                        }).ToList(),
                        priceChangeRule = "AcceptNone"
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
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
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
                        _logger.LogError("SendBet Error Response: {@response}", await response.Content.ReadAsStringAsync());

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

        public async Task<BaseResponseDTO<Task>> Cancellation(string authToken, Guid betId)
        {
            using(var activity = _activitySource.StartActivity("gts.bet.cancellation"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                try
                {
                    var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

                    activity?.SetTag("gts.bookmaker.id", bookmakerId);

                    var payload = new
                    {
                        betId = betId.ToString().ToUpper()
                    };

                    activity?.SetTag("gts.bet.cancellation.payload", payload);

                    var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/bet-reservation-cancellations", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        var initialResult = JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        return new BaseResponseDTO<Task>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString()
                        };
                    }
                    else
                    {
                        _logger.LogError("Cancellation Error: {@response}", await response.Content.ReadAsStringAsync());

                        activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                        return new BaseResponseDTO<Task>
                        {
                            StatusCode = StatusCode.Error,
                            Message = StatusCode.Error.ToString(),
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Cancellation Exception: {@exception}", ex);

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                    return new BaseResponseDTO<Task>
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
            var s when s == (int)BetStatus.Void || s == (int)BetStatus.Cancelled || s == (int)BetStatus.Rejected => Enums.Provider.GTS.BetStatus.Cancelled.ToString(),
            _ => Enums.Provider.GTS.BetStatus.Settled.ToString()
        };

        private string GetGameState(int fixtureStatusId) => fixtureStatusId switch
        {
            var s when s == 1 => "InPlay",
            _ => "PreMatch"
        };
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
        public List<BetLeg> Legs { get; set; }
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
