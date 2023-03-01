using Microsoft.Extensions.Configuration;
using Platform.Operator.API.Enums;
using Platform.Operator.API.Models;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Operator.API.Repository
{
    public class WalletRepository : IWalletRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public WalletRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(_configuration["API:WalletAPI"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", configuration["Custom:WalletAPIKey"]);
        }

        public async Task<WalletResponse> GetUserWallet(UserWalletRequest request)
        {
            try
            {
                var response = await _httpClient.GetAsync($"v1/userwallet?clientId={request.ClientId}&userId={request.UserId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<WalletResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
                else
                {
                    var result = JsonSerializer.Deserialize<WalletResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new WalletResponse
                {
                    StatusCode = 1000,
                    Message = $"Error ({ex.Message})",
                };
            }
        }

        public async Task<BetResponse> PlaceBet(CreditOrDebitRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/debit", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
                else
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new BetResponse
                {
                    StatusCode = 1000,
                    Message = $"Error ({ex.Message})",
                };
            }
        }

        public async Task<BetResponse> SettleBet(CreditOrDebitRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/credit", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
                else
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new BetResponse
                {
                    StatusCode = 1000,
                    Message = $"Error ({ex.Message})",
                };
            }
        }

        public async Task<BetResponse> CashOutBet(CreditOrDebitRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/credit", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
                else
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new BetResponse
                {
                    StatusCode = 1000,
                    Message = $"Error ({ex.Message})",
                };
            }
        }

        public async Task<BetResponse> CancelBet(CreditOrDebitRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/credit", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
                else
                {
                    var result = JsonSerializer.Deserialize<BetResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new BetResponse
                {
                    StatusCode = 1000,
                    Message = $"Error ({ex.Message})",
                };
            }
        }
    }
}
