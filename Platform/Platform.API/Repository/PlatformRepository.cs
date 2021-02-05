using Platform.API.Data;
using Platform.API.Models;
using Platform.Shared.Enums;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly HttpClient _httpClient;
        private readonly PlatformContext _platformContext;

        public PlatformRepository(HttpClient httpClient, PlatformContext platformContext)
        {
            _httpClient = httpClient;
            _platformContext = platformContext;
        }
        public async Task<BaseResponse<WalletBalanceResponse>> GetWalletBalance(string clientId, string operatorToken)
        {
            var client = await GetClient(clientId);

            if (client == null)
            {
                return new BaseResponse<WalletBalanceResponse>
                {
                    StatusCode = StatusCode.Missing_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var authenticate = await Authenticate(operatorToken, client.Endpoint);

            if (authenticate == null)
            {
                return new BaseResponse<WalletBalanceResponse>
                {
                    StatusCode = StatusCode.Missing_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            return new BaseResponse<WalletBalanceResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WalletBalanceResponse
                {
                    Balance = authenticate.Balance
                }
            };
        }
        private async Task<ClientResponse> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid)
            {
                var result = await _platformContext.Client.FindAsync(guid);

                if (result == null) return null;
                if (result.IsActive == false) return null;

                return new ClientResponse
                {
                    ClientId = result.ClientId,
                    ClientName = result.ClientName,
                    ClientCode = result.ClientCode,
                    LimitId = result.LimitId,
                    IsActive = result.IsActive,
                    Endpoint = result.Endpoint
                };
            }
            else
                return null;
        }
        private async Task<AuthenticationResponse> Authenticate(string operatorToken, string endpoint)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken)) return null;

            _httpClient.BaseAddress = new Uri(endpoint);
            _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            var response = await _httpClient.PostAsync($"/v1/authentication", new StringContent(JsonSerializer.Serialize(new { }), System.Text.Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var val = JsonSerializer.Deserialize<AuthenticationResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                return new AuthenticationResponse
                {
                    Balance = val.Balance
                };
            }
            else
            {
                return null;
            }
        }
    }
}
