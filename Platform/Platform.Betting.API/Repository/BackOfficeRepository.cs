using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public class BackOfficeRepository : IBackOfficeRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public BackOfficeRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:BackOfficeAPI"]);
        }
        public async Task<BaseResponseDTO<BetConfigLeagueResponse>> GetTierMaxStake(BetConfigLeagueRequestDTO request)
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["Custom:BackOfficeAPIKey"].ToString());

            try
            {
                var response = await _httpClient.GetAsync($"api/v1/tier/maxstake?SegmentId=" + request.SegmentId + "&LeagueId=" + request.LeagueId + "&SportId=" + request.SportId);

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetConfigLeagueResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    return new BaseResponseDTO<BetConfigLeagueResponse>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        Result = new BetConfigLeagueResponse
                        {
                            MaxPerGame = 0,
                            MaxStake = 0
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetConfigLeagueResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new BetConfigLeagueResponse
                    {
                        MaxPerGame = 0,
                        MaxStake = 0
                    }
                };
            }
        }
    }
}
