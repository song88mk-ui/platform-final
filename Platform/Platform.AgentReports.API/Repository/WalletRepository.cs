using Microsoft.Extensions.Configuration;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Repository
{
    public class WalletRepository : IWalletRepository
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        #endregion

        #region Ctor

        public WalletRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _httpClient.BaseAddress = new Uri(_configuration["PlatformWallet:Uri"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["PlatformWallet:ApiKey"]);
        }

        #endregion

        #region Methods

        public async Task<WalletResponse> Credit(CreditOrDebitRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v1/credit", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<WalletResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<WalletResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new WalletResponse
                        {
                            StatusCode = (int)StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new WalletResponse
                {
                    StatusCode = (int)StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public async Task<WalletResponse> Debit(CreditOrDebitRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v1/debit", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<WalletResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<WalletResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new WalletResponse
                        {
                            StatusCode = (int)StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new WalletResponse
                {
                    StatusCode = (int)StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        #endregion
    }
}
