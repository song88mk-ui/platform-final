using Microsoft.Extensions.Configuration;
using Platform.Operator.API.Enums;
using Platform.Operator.API.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public class AuthRepository : IAuthRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AuthRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(_configuration["API:AuthAPI"]);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", configuration["Custom:AuthAPIKey"]);
        }
        public async Task<BaseResponse<LoginResponse>> Login(LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new BaseResponse<LoginResponse>
                {
                    StatusCode = AuthStatusCode.Not_Authorized,
                    Message = AuthStatusCode.Not_Authorized.ToString().Replace('_', ' '),
                    Result = new LoginResponse
                    {
                        OperatorToken = string.Empty
                    }
                };
            }

            try
            {
                var response = await _httpClient.PostAsync($"v1/login", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                var result = JsonSerializer.Deserialize<BaseResponse<LoginResponse>>(await response.Content.ReadAsStringAsync(), _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    return result;
                }
                else
                {
                    return new BaseResponse<LoginResponse>
                    {
                        StatusCode = result.StatusCode,
                        Message = result.Message,
                        Result = new LoginResponse
                        {
                            OperatorToken = string.Empty,
                            AccessToken = string.Empty
                        }
                    };

                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<LoginResponse>
                {
                    StatusCode = AuthStatusCode.Error,
                    Message = $"{AuthStatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new LoginResponse
                    {
                        OperatorToken = string.Empty
                    }
                };
            }
        }
        public async Task<BaseResponse<bool>> Logout(string operatorToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("x-operator-token");
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                var response = await _httpClient.PostAsync($"v1/logout", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<BaseResponse<bool>>(await response.Content.ReadAsStringAsync(), _jsonOptions);

                    return result;
                }
                else
                {
                    return new BaseResponse<bool>
                    {
                        StatusCode = AuthStatusCode.Error,
                        Message = AuthStatusCode.Error.ToString().Replace('_', ' '),
                        Result = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponse<bool>
                {
                    StatusCode = AuthStatusCode.Error,
                    Message = $"{AuthStatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = false
                };
            }
        }
        public async Task<BaseResponse<AuthResponse>> Authenticate(string operatorToken, string isAgent)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponse<AuthResponse>
                {
                    StatusCode = AuthStatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = AuthStatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            var isAgentTemp = false.ToString();

            if (!string.IsNullOrEmpty(isAgent))
                isAgentTemp = Convert.ToBoolean(isAgent).ToString();

            if (_httpClient.DefaultRequestHeaders.Contains("x-is-agent"))
            {
                _httpClient.DefaultRequestHeaders.Remove("x-is-agent");
                _httpClient.DefaultRequestHeaders.Add("x-is-agent", isAgentTemp);
            }
            else
                _httpClient.DefaultRequestHeaders.Add("x-is-agent", isAgentTemp);

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
                    var result = JsonSerializer.Deserialize<BaseResponse<AuthResponse>>(await response.Content.ReadAsStringAsync(), _jsonOptions);

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
