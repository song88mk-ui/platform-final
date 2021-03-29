using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public class MTSMaxStakeRepository : IMTSMaxStakeRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public MTSMaxStakeRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:MTSMaxStakeAPI"]);
        }

        public async Task<BaseResponseDTO<MTSStakeResponseDTO>> GetMTSMaxStake(string authToken, MTSConfigRequestDTO request)
        {
            if (request.BetSelections.FirstOrDefault().SportId >= 1000)
            {
                return new BaseResponseDTO<MTSStakeResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new MTSStakeResponseDTO
                    {
                        Stake = 0m
                    }
                };
            }

            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) == false)
            {
                return new BaseResponseDTO<MTSStakeResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new MTSStakeResponseDTO
                    {
                        Stake = 0m
                    }
                };
            }

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

            try
            {
                var betId = Guid.NewGuid().ToString();

                var bets = new List<MTSBetDTO>();
                bets.Add(new MTSBetDTO
                {
                    Id = betId,
                    Stake = new MTSStakeDTO { Value = 0m }
                });

                var betSelections = new List<MTSBetSelectionDTO>();
                foreach (var req in request.BetSelections)
                {
                    betSelections.Add(new MTSBetSelectionDTO
                    {
                        EventId = req.FixtureSpecifier,
                        Id = req.SelectionSpecifier,
                        Odds = req.Odds * 10000
                    });
                }

                if (request.Language.Contains("ZH"))
                    request.Language = "ZH";

                var maxStakePayload = new MTSMaxStakeRequestDTO
                {
                    TimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
                    Bets = bets,
                    TicketId = betId,
                    Selections = betSelections,
                    Sender = new SenderDTO
                    {
                        BookmakerId = int.Parse(_configuration["Custom:MTSBookmakerId"]),
                        LimitId = request.LimitId,
                        Currency = request.Currency.ToUpper(),
                        EndCustomer = new EndCustomerDTO
                        {
                            LanguageId = request.Language,
                            Id = request.UserId,
                            DeviceId = request.DeviceId
                        }
                    }
                };

                var response = await _httpClient.PostAsync($"edge/proxy/ClientApi/api/maxStake/v1", new StringContent(JsonSerializer.Serialize(maxStakePayload), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var initialResult = JsonSerializer.Deserialize<MTSMaxStakeResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    long.TryParse(initialResult.MaxStake, out long maxStakeReturn);

                    return new BaseResponseDTO<MTSStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MTSStakeResponseDTO
                        {
                            Stake = maxStakeReturn / 10000
                        }
                    };
                }
                else
                {
                    return new BaseResponseDTO<MTSStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MTSStakeResponseDTO
                        {
                            Stake = 0
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<MTSStakeResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MTSStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }
        }
    }
}
