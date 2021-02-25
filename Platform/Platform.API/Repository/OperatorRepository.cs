using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class OperatorRepository : IOperatorRepository
    {
        private readonly HttpClient _httpClient;

        public OperatorRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }
        public async Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string operatorToken, string endpoint)
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
            _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            try
            {
                var response = await _httpClient.PostAsync($"/v1/authentication", new StringContent(JsonSerializer.Serialize(new { Token = operatorToken }), System.Text.Encoding.UTF8, "application/json"));

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
