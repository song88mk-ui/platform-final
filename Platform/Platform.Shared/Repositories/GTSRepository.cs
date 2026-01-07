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

namespace Platform.Shared.Repositories
{
    public class GTSRepository : IGTSRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly ILogger<GTSRepository> _logger;

        public GTSRepository(HttpClient httpClient, IConfiguration configuration, ILogger<GTSRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:GTSAPI"]);
        }

        [Obsolete("Please use GetMaxStake2 since getmaxstake doesnt support currency")]
        public async Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStakeOld(string authToken, BetRequestDTO request)
        {

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

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

                var maxStakePayload = new
                {
                    playerId = request.UserId,
                    legs = request.Selections.Select(s => new
                    {
                        selectionId = s.SelectionTemplateId, //TODO: double check the value
                        gameState = "prematch", //TODO: get the gameState value,
                        price = s.DecimalOdds
                    }).ToList()
                };

                var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/max-allowed-stake", new StringContent(JsonSerializer.Serialize(maxStakePayload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<GTSMaxStakeResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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
        }

        public async Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStake(string authToken, BetRequestDTO request)
        {

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

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

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
                        gameState = "prematch", //TODO: get the gameState value,
                        price = s.DecimalOdds
                    }).ToList(),
                    priceChangeRule = "AcceptNone"
                };

                var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/assessment", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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
                _logger.LogError($"GetMaxStake Error: {JsonSerializer.Serialize(ex)}");

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
        }

        public async Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> Assessment(string authToken, OnBetPlacement request)
        {
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
                var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

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
                        gameState = "prematch", //TODO: get the gameState value,
                        price = s.BetSelection.Odds
                    }).ToList(),
                    priceChangeRule = "AcceptNone"
                };

                var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/assessment", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<GTSAssessmentResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = initialResult.MaxAllowedStake,
                            IsBetAllowed = initialResult.IsBetAllowed,
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
                    _logger.LogError($"Payload: {JsonSerializer.Serialize(payload)}");

                    return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({responseError})",
                        Result = new ProviderAssessmentResponseDTO
                        {
                            Stake = 0
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Assessment Error: {JsonSerializer.Serialize(ex)}");

                return new BaseResponseDTO<ProviderAssessmentResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new ProviderAssessmentResponseDTO
                    {
                        Stake = 0
                    }
                };
            }
        }


        public async Task<BaseResponseDTO<string>> SendBet(string authToken, SendBet request)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

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
                        status = GetGtsBetStatus(s.BetSelectionStatusId), //TODO: get the status value
                        gameState = "prematch", //TODO: get the gameState value,
                        price = s.Odds,
                        betGeniusContent = new
                        {
                            selectionId = s.SelectionSpecifier.Split(':').LastOrDefault()
                        }
                    }).ToList(),
                    priceChangeRule = "AcceptNone"
                };

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

                    return new BaseResponseDTO<string>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = initialResult
                    };
                }
                else
                {
                    _logger.LogError($"SendBet Error: {JsonSerializer.Serialize(await response.Content.ReadAsStringAsync())}");
                    _logger.LogError($"SendBet Payload: {JsonSerializer.Serialize(payload)}");

                    return new BaseResponseDTO<string>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendBet Error: {JsonSerializer.Serialize(ex.Message)}");
                _logger.LogError($"SendBet StackTrace: {JsonSerializer.Serialize(ex.StackTrace)}");

                return new BaseResponseDTO<string>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }

        public async Task<BaseResponseDTO<Task>> Cancellation(string authToken, Guid betId)
        {

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                var bookmakerId = int.Parse(_configuration["Custom:GTSBookmakerId"]);

                var payload = new
                {
                    betId = betId.ToString().ToUpper()
                };

                var response = await _httpClient.PostAsync($"v1/platforms/song88/bookmakers/{bookmakerId}/bet-reservation-cancellations", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<Task>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString()
                    };
                }
                else
                {
                    _logger.LogError($"Cancellation Error: {JsonSerializer.Serialize(await response.Content.ReadAsStringAsync())}");

                    return new BaseResponseDTO<Task>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cancellation Error: {JsonSerializer.Serialize(ex)}");

                return new BaseResponseDTO<Task>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }

        private string GetGtsBetStatus(int statusId) => statusId switch
        {
            var s when s <= (int)BetStatus.Running => Enums.Provider.GTS.BetStatus.Open.ToString(),
            var s when s == (int)BetStatus.Void || s == (int)BetStatus.Cancelled || s == (int)BetStatus.Rejected => Enums.Provider.GTS.BetStatus.Cancelled.ToString(),
            _ => Enums.Provider.GTS.BetStatus.Settled.ToString()
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
