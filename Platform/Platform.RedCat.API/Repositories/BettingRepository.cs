using Microsoft.AspNetCore.Http;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.RedCat.API.Repositories
{
    public class BettingRepository : IBettingRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;

        public BettingRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> PlaceBet(RedCatBetRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v1/vs/bet/placement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.GetDescription()} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.GetDescription()}"
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> SettleBet(RedCatBetRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v1/vs/bet/settle", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.GetDescription()} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.GetDescription()}"
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidStake(RedCatVoidBetRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v1/vs/bet/void/placement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.GetDescription()} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.GetDescription()}"
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettle(RedCatVoidBetRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v1/vs/bet/void/settlement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<RedCatBetTransactionResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.GetDescription()} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.GetDescription()}"
                };
            }
        }
    }
}
