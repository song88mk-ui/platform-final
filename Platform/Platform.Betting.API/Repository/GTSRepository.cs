using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Platform.Shared.Utility;
using System.Text.Json.Serialization;

namespace Platform.Betting.API.Repository
{
    public class GTSRepository : IGTSRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;

        public GTSRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:GTSAPI"]);
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
                        gameState = "prematch" //TODO: get the gameState value
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


    }

    public class GTSMaxStakeResponseDTO
    {
        [JsonPropertyName("maxAllowedStake")]
        public decimal MaxAllowedStake { get; set; }
    }

}
