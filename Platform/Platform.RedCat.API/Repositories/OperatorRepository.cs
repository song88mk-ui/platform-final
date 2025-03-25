using Platform.RedCat.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.RedCat.API.Repositories
{
    public class OperatorRepository : IOperatorRepository
    {
        //private readonly HttpClient _httpClient;
        //public OperatorRepository(HttpClient httpClient)
        //{
        //    _httpClient = httpClient;
        //}

        public async Task<RedCatBaseResponse<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new RedCatBaseResponse<AuthenticationResponseDTO>
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                };
            }

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(endpoint)
            };

            httpClient.DefaultRequestHeaders.Add("x-is-agent", isAgent.ToString());
            httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);
            httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);

            try
            {
                var response = await httpClient.PostAsync($"{endpoint}/v1/authentication", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<AuthenticationResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new RedCatBaseResponse<AuthenticationResponseDTO>
                    {
                        StatusCode = RedCatStatusCode.Success,
                        Message = RedCatStatusCode.Success.GetDescription(),
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
                        }
                        catch (Exception)
                        {
                        }

                        return new RedCatBaseResponse<AuthenticationResponseDTO>
                        {
                            StatusCode = RedCatStatusCode.Operator_Error,
                            Message = RedCatStatusCode.Operator_Error.GetDescription(),
                        };
                    }
                    else
                    {
                        return new RedCatBaseResponse<AuthenticationResponseDTO>
                        {
                            StatusCode = RedCatStatusCode.Operator_Error,
                            Message = $"{RedCatStatusCode.Operator_Error.GetDescription()} ({response.ReasonPhrase})",
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RedCatBaseResponse<AuthenticationResponseDTO>
                {
                    StatusCode = RedCatStatusCode.Error,
                    Message = $"{RedCatStatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                };
            }
        }
    }
}
