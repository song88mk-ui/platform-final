using Platform.API.Data;
using Platform.API.Models;
using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
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
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var authenticate = await Authenticate(operatorToken, client.Endpoint);

            if (authenticate == null)
            {
                return new BaseResponse<WalletBalanceResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            return new BaseResponse<WalletBalanceResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WalletBalanceResponse
                {
                    UserId = authenticate.UserId,
                    Balance = authenticate.Balance
                }
            };
        }
        public async Task<BaseResponse<BetResponse>> PlaceBet(string clientId, string operatorToken, BetRequest request)
        {
            var client = await GetClient(clientId);

            if (client == null)
            {
                return new BaseResponse<BetResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var authenticate = await Authenticate(operatorToken, client.Endpoint);

            if (authenticate == null)
            {
                return new BaseResponse<BetResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            if (authenticate.Balance < request.Stake)
            {
                return new BaseResponse<BetResponse>
                {
                    StatusCode = StatusCode.Insufficient_Funds,
                    Message = StatusCode.Insufficient_Funds.ToString().Replace('_', ' '),
                };
            }

            //TODO: call Betting API for bet placement
            //TODO: return the proper status from betting api
            return new BaseResponse<BetResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString().Replace('_', ' '),
                Result = new BetResponse
                {
                    BetId = Guid.NewGuid().ToString().ToUpper(),
                    UserId = authenticate.UserId,
                    BetStatus = BetStatus.Waiting
                }
            };
        }
        public async Task<BaseResponse<BetHistoryResponse>> GetBetHistory(string clientId, string operatorToken, BetHistoryRequest request)
        {
            var client = await GetClient(clientId);

            if (client == null)
            {
                return new BaseResponse<BetHistoryResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var authenticate = await Authenticate(operatorToken, client.Endpoint);

            if (authenticate == null)
            {
                return new BaseResponse<BetHistoryResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            //TODO: call Betting API for bet history
            //TODO: return the proper data from betting api
            return new BaseResponse<BetHistoryResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString().Replace('_', ' '),
                Result = new BetHistoryResponse
                {
                    CurrentPage = 1,
                    PageCount = 10,
                    PageSize = 20,
                    RowCount = 20,
                    Bets = new List<BetHistory>()
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
                    UserId = val.UserId,
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
