using System.Net;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Platform.Operator.API.Models;
using Platform.Operator.API.Enums;
using Microsoft.Extensions.Configuration;

namespace Platform.Operator.API.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AuthRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(_configuration["API:AuthAPI"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", "02a8035d376ab8e3a0333eb363770584");
        }

        public async Task<BaseResponse<AuthResponse>> Authenticate(string operatorToken)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponse<AuthResponse>
                {
                    StatusCode = AuthStatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = AuthStatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            if (_httpClient.DefaultRequestHeaders.Contains("x-operator-token"))
            {
                _httpClient.DefaultRequestHeaders.Remove("x-operator-token");
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);
            }
            else
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            try
            {
                var response = await _httpClient.PostAsync($"v1/authentication/operator", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BaseResponse<AuthResponse>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return result;
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return new BaseResponse<AuthResponse>
                        {
                            StatusCode = AuthStatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                            Message = AuthStatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                        };
                    }
                    else
                    {
                        return new BaseResponse<AuthResponse>
                        {
                            StatusCode = AuthStatusCode.Error,
                            Message = $"{AuthStatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<AuthResponse>
                {
                    StatusCode = AuthStatusCode.Error,
                    Message = $"{AuthStatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }
    }
}
