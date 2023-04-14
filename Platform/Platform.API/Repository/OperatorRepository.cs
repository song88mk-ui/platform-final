using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient.Server;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class OperatorRepository : IOperatorRepository
    {
        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetryClient;

        public OperatorRepository(HttpClient httpClient, TelemetryClient telemetryClient)
        {
            _httpClient = httpClient;
            _telemetryClient = telemetryClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
        }

        public async Task<BaseResponseDTO<LoginResponseDTO>> Login(string userName, string password, string endpoint, string userAgent = "")
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                return new BaseResponseDTO<LoginResponseDTO>
                {
                    StatusCode = StatusCode.Not_Authorized,
                    Message = StatusCode.Not_Authorized.ToString().Replace('_', ' ')
                };
            }

            try
            {
                if(_httpClient.DefaultRequestHeaders.Contains("user-agent") && !string.IsNullOrWhiteSpace(userAgent))
                {
                    _httpClient.DefaultRequestHeaders.Remove("user-agent");
                    _httpClient.DefaultRequestHeaders.Add("user-agent", userAgent);
                }

                var response = await _httpClient.PostAsync($"{endpoint}/v1/authentication/login", new StringContent(JsonSerializer.Serialize(new { UserName = userName, Password = password }), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<LoginResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<LoginResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new LoginResponseDTO
                        {
                            OperatorToken = result.OperatorToken
                        }
                    };
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        try
                        {
                            var properties = new Dictionary<string, string>
                            {
                                { "userName", userName },
                                { "password", password },
                                { "endpoint", endpoint }
                            };

                            _telemetryClient.TrackEvent("Login", properties);
                        }
                        catch (Exception)
                        {
                        }

                        var result = JsonSerializer.Deserialize<int>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        return new BaseResponseDTO<LoginResponseDTO>
                        {
                            StatusCode = (StatusCode)result,
                            Message = ((StatusCode)result).ToString().Replace('_', ' '),
                        };
                    }
                    else
                    {
                        return new BaseResponseDTO<LoginResponseDTO>
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<LoginResponseDTO>
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }
        public async Task<BaseResponseDTO<object>> Logout(string operatorToken, string endpoint)
        {
            if (string.IsNullOrEmpty(operatorToken))
            {
                return new BaseResponseDTO<object>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace('_', ' ')
                };
            }

            _httpClient.DefaultRequestHeaders.Remove("x-operator-token");
            _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            try
            {
                var response = await _httpClient.PostAsync($"{endpoint}/v1/authentication/logout", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<LogoutResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<object>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new LogoutResponseDTO
                        {
                            StatusId = result.StatusId
                        }
                    };
                }
                else
                {
                    return new BaseResponseDTO<object>
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<object>
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }
        public async Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<AuthenticationResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            _httpClient.BaseAddress = new Uri(endpoint);

            if (_httpClient.DefaultRequestHeaders.Contains("x-is-agent"))
            {
                _httpClient.DefaultRequestHeaders.Remove("x-is-agent");

                _httpClient.DefaultRequestHeaders.Add("x-is-agent", isAgent.ToString());
            }
            else
                _httpClient.DefaultRequestHeaders.Add("x-is-agent", isAgent.ToString());

            if (_httpClient.DefaultRequestHeaders.Contains("x-operator-token"))
            {
                _httpClient.DefaultRequestHeaders.Remove("x-operator-token");
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);
            }
            else
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            if (_httpClient.DefaultRequestHeaders.Contains("x-client-id"))
            {
                _httpClient.DefaultRequestHeaders.Remove("x-client-id");
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
            }
            else
                _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);

            try
            {
                var response = await _httpClient.PostAsync($"{endpoint}/v1/authentication", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<AuthenticationResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<AuthenticationResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new AuthenticationResponseDTO
                        {
                            UserId = result.UserId,
                            Balance = result.Balance,
                            Payable = result.Payable,
                            CurrencyCode = result.CurrencyCode,
                            StatusId = result.StatusId,
                            LanguageCode = result.LanguageCode
                        }
                    };
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        try
                        {
                            var result = JsonSerializer.Deserialize<AuthenticationResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                            var properties = new Dictionary<string, string>
                            {
                                { "x-operator-token", operatorToken },
                                { "endpoint", endpoint },
                                { "response", result.StatusId.ToString() }
                            };

                            _telemetryClient.TrackEvent("Authenticate", properties);
                        }
                        catch (Exception)
                        {
                        }

                        return new BaseResponseDTO<AuthenticationResponseDTO>
                        {
                            StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                            Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                        };
                    }
                    else
                    {
                        return new BaseResponseDTO<AuthenticationResponseDTO>
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<AuthenticationResponseDTO>
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }
        public async Task<BaseResponseDTO<OperatorBetResponseDTO>> ResendCancellation(string endpoint, string info)
        {
            _httpClient.BaseAddress = new Uri(endpoint);

            try
            {
                var response = await _httpClient.PostAsync($"{endpoint}", new StringContent(info, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new BaseResponseDTO<OperatorBetResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = result
                    };
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        return new BaseResponseDTO<OperatorBetResponseDTO>
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.ToString(),
                            Result = result
                        };
                    }

                    return new BaseResponseDTO<OperatorBetResponseDTO>
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<OperatorBetResponseDTO>
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }
    }
}
