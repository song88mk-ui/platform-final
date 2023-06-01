using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace Platform.QTech.API.Repository
{
    public class OperatorRepository : IOperatorRepository
    {
        private readonly HttpClient _httpClient;
        public OperatorRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
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

                            //var properties = new Dictionary<string, string>
                            //{
                            //    { "x-operator-token", operatorToken },
                            //    { "endpoint", endpoint },
                            //    { "response", result.StatusId.ToString() }
                            //};

                            //_telemetryClient.TrackEvent("Authenticate", properties);
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
        public async Task<BaseResponseDTO<OperatorBetResponseDTO>> PlaceBet(string clientId, string operatorToken, string endpoint, bool isAgent, OperatorCasinoBetPlacementRequestDTO request)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<OperatorBetResponseDTO>
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
                var response = await _httpClient.PostAsync($"{endpoint}/v1/casino/bet/placement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
                        try
                        {
                            var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                            switch (result.StatusId)
                            {
                                case 1001:
                                    return new BaseResponseDTO<OperatorBetResponseDTO>
                                    {
                                        StatusCode = StatusCode.Insufficient_Funds,
                                        Message = StatusCode.Insufficient_Funds.ToString().Replace('_', ' '),
                                    };

                                case 1002:
                                    return new BaseResponseDTO<OperatorBetResponseDTO>
                                    {
                                        StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                                        Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                                    };

                                default:
                                    return new BaseResponseDTO<OperatorBetResponseDTO>
                                    {
                                        StatusCode = StatusCode.Operator_Error,
                                        Message = StatusCode.Operator_Error.ToString().Replace('_', ' '),
                                    };
                            }
                        }
                        catch (Exception)
                        {
                            return new BaseResponseDTO<OperatorBetResponseDTO>
                            {
                                StatusCode = StatusCode.Operator_Error,
                                Message = StatusCode.Operator_Error.ToString().Replace('_', ' '),
                            };
                        }
                    }
                    else
                    {
                        return new BaseResponseDTO<OperatorBetResponseDTO>
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        };
                    }
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
