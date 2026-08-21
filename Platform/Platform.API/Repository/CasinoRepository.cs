using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;

namespace Platform.API.Repository
{
    public class CasinoRepository : ICasinoRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public CasinoRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:CasinoAPI"]);
        }
        public async Task<BaseResponseDTO<GameLobbyResponseDTO>> GetLobbyURL(GameLobbyRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/lobby", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<GameLobbyResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<GameLobbyResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<GameLobbyResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<GameLobbyResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<BaseResponseDTO<CasinoBetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/bet/history", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return JsonSerializer.Deserialize<BaseResponseDTO<CasinoBetHistoryResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                }
                else
                {
                    return new BaseResponseDTO<CasinoBetHistoryResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<CasinoBetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_',' ')} ({ex.Message})"
                };
            }
        }
    }
}
