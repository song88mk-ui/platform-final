using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Platform.API.Repository
{
    public class OperatorRepository : IOperatorRepository
    {
        private readonly HttpClient _httpClient;
        private readonly TelemetryClient _telemetryClient;
        private readonly IConfiguration _configuration;

        private static List<string> _ogHighCurrencies = new List<string> { "IDRK", "VNDK", "MMKK", "INRK" };

        public OperatorRepository(HttpClient httpClient, TelemetryClient telemetryClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _telemetryClient = telemetryClient;
            _configuration = configuration;
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<BaseResponseDTO<LoginResponseDTO>> Login(string userName, string password, string endpoint, Dictionary<string, string> headers = null)
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
                foreach (var item in headers)
                {
                    if (_httpClient.DefaultRequestHeaders.Contains(item.Key))
                        _httpClient.DefaultRequestHeaders.Remove(item.Key);
                    if (!string.IsNullOrEmpty(item.Value))
                        _httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
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

        public async Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent, OGClientRequest request)
        {
            if (request != null)
                return await AuthenticateOGPlayer(operatorToken, endpoint, request);

            return await AuthenticatePlayer(clientId, operatorToken, endpoint, isAgent);
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

        private async Task<BaseResponseDTO<AuthenticationResponseDTO>> AuthenticateOGPlayer(string operatorToken, string endpoint, OGClientRequest request)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<AuthenticationResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            if (string.IsNullOrEmpty(request.PlayerCode) || string.IsNullOrWhiteSpace(request.PlayerCode))
            {
                return new BaseResponseDTO<AuthenticationResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace('_', ' '),
                };
            }

            var key = _configuration["Custom:OrientalGaming:PublicAPIKey"].ToString();
            var vendorName = _configuration["Custom:OrientalGaming:VendorName"].ToString();

            _httpClient.BaseAddress = new Uri(endpoint);

            if (_httpClient.DefaultRequestHeaders.Contains("key"))
            {
                _httpClient.DefaultRequestHeaders.Remove("key");
                _httpClient.DefaultRequestHeaders.Add("key", key);
            }
            else
                _httpClient.DefaultRequestHeaders.Add("key", key);

            if (_httpClient.DefaultRequestHeaders.Contains("vendor-name"))
            {
                _httpClient.DefaultRequestHeaders.Remove("vendor-name");
                _httpClient.DefaultRequestHeaders.Add("vendor-name", vendorName);
            }
            else
                _httpClient.DefaultRequestHeaders.Add("vendor-name", vendorName);

            try
            {
                var privateKey = _configuration["Custom:OrientalGaming:PrivateAPIKey"].ToString();

                // authenticate endpoint
                var authenticationPayload = new OGAuthenticationRequest
                {
                    Token = operatorToken,
                    PlayerCode = request.PlayerCode,
                    GameCode = request.GameCode,
                    PoolId = request.PoolId,
                    BetLimit = request.BetLimit,
                    Signature = string.Empty,
                    Timestamp = request.Timestamp,
                };

                authenticationPayload.Signature = authenticationPayload.ToSignOG(privateKey);

                var authenticationResponse = await _httpClient.PostAsync($"{endpoint}/api/v2/platform/security/authenticate", new StringContent(JsonSerializer.Serialize(authenticationPayload), System.Text.Encoding.UTF8, "application/json"));

                var authenticationResult = JsonSerializer.Deserialize<OGAuthenticationResponse>(await authenticationResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = false });

                if (authenticationResult.Code != "S-100")
                {
                    return new BaseResponseDTO<AuthenticationResponseDTO>
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({authenticationResult.Code}: {authenticationResult.Message})",
                    };
                }

                var balance = 0m;
                var currencyCode = string.Empty; 

                if (!request.IsSBLaunch)
                {
                    // wallet endpoint
                    var walletRequest = new
                    {
                        player_code = request.PlayerCode,
                        timestamp = request.Timestamp
                    };

                    var signature = walletRequest.ToSignOG(privateKey);

                    var walletResponse = await _httpClient.GetAsync($"{endpoint}/api/v2/platform/player/balance?player_code={walletRequest.player_code}&timestamp={walletRequest.timestamp}&signature={signature}");

                    var walletResult = JsonSerializer.Deserialize<OGWalletResponseDTO>(await walletResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = false });
                    
                    if (walletResult.Code != "S-100")
                    {
                        return new BaseResponseDTO<AuthenticationResponseDTO>
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({authenticationResult.Code}: {authenticationResult.Message})",
                        };
                    }

                    balance = Convert.ToDecimal(walletResult.Balance);
                    currencyCode = walletResult.CurrencyCode;

                    if (currencyCode.ToUpper() != request.CurrencyCode.ToUpper())
                    {
                        return new BaseResponseDTO<AuthenticationResponseDTO>
                        {
                            StatusCode = StatusCode.CurrencyCode_Mismatch,
                            Message = StatusCode.CurrencyCode_Mismatch.ToString().Replace('_', ' '),
                        };
                    }
                }

                // combine result
                return new BaseResponseDTO<AuthenticationResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new AuthenticationResponseDTO
                    {
                        UserId = request.PlayerCode,
                        Balance = balance,
                        CurrencyCode = _ogHighCurrencies.Contains(currencyCode) ? currencyCode.Substring(0, currencyCode.Length - 1) : currencyCode,
                        RefCurrencyCode = currencyCode,
                        StatusId = 1000,
                        LanguageCode = request.LanguageCode,
                    }
                };
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

        private async Task<BaseResponseDTO<AuthenticationResponseDTO>> AuthenticatePlayer(string clientId, string operatorToken, string endpoint, bool isAgent)
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
    }
}