using Microsoft.Extensions.Configuration;
using Platform.API.Data;
using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly HttpClient _httpClient;
        private readonly PlatformContext _platformContext;
        private readonly IConfiguration _configuration;

        public PlatformRepository(HttpClient httpClient, PlatformContext platformContext, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _platformContext = platformContext;
            _configuration = configuration;
        }
        public async Task<BaseResponseDTO<WalletBalanceResponseDTO>> GetWalletBalance(string clientId, string operatorToken)
        {
            var client = await GetClient(clientId);

            if (client == null)
            {
                return new BaseResponseDTO<WalletBalanceResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var authenticate = await Authenticate(operatorToken, client.Endpoint);

            if (authenticate == null)
            {
                return new BaseResponseDTO<WalletBalanceResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            return new BaseResponseDTO<WalletBalanceResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WalletBalanceResponseDTO
                {
                    UserId = authenticate.UserId,
                    Balance = authenticate.Balance
                }
            };
        }
        public async Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(string apiKey, string clientId, string operatorToken, BetRequest request)
        {
            var client = await GetClient(clientId);

            if (client == null)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var authenticate = await Authenticate(operatorToken, client.Endpoint);

            if (authenticate == null)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            if (authenticate.Balance < request.Stake)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Insufficient_Funds,
                    Message = StatusCode.Insufficient_Funds.ToString().Replace('_', ' '),
                };
            }

            var payload = new BetRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientId,
                UserId = authenticate.UserId,
                Stake = request.Stake,
                TransactionDate = request.TransactionDate,
                OddsType = request.OddsType,
                CurrencyCode = request.CurrencyCode,
                LanguageCode = request.LanguageCode,
                Channel = request.Channel,
                IPAddress = request.IPAddress,
                Selections = request.Selections.Select(s => new BetSelectionRequestDTO
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    FixtureId = s.FixtureId,
                    FixtureName = s.FixtureName,
                    MarketTemplateId = s.MarketTemplateId,
                    MarketId = s.MarketId,
                    MarketName = s.MarketName,
                    Line = s.Line,
                    SelectionTemplateId = s.SelectionTemplateId,
                    SelectionId = s.SelectionId,
                    SelectionName = s.SelectionName,
                    Odds = s.Odds,
                    FixtureStatus = s.FixtureStatus,
                    FixtureStartDate = s.FixtureStartDate,
                    BetHomeScore = s.BetHomeScore,
                    BetAwayScore = s.BetAwayScore,
                    FixtureSpecifier = s.FixtureSpecifier,
                    SelectionSpecifier = s.SelectionSpecifier,
                    CorrelationId = s.CorrelationId
                }).ToList()
            };

            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformBettingAPI"]);

            var response = await _httpClient.PostAsync($"/v1/bet/placement", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

            var result = JsonSerializer.Deserialize<BaseResponseDTO<BetResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            return result;
        }
        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string clientId, string operatorToken, BetHistoryRequest request)
        {
            var client = await GetClient(clientId);

            if (client == null)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var authenticate = await Authenticate(operatorToken, client.Endpoint);

            if (authenticate == null)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            //TODO: call Betting API for bet history
            //TODO: return the proper data from betting api
            return new BaseResponseDTO<BetHistoryResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString().Replace('_', ' '),
                Result = new BetHistoryResponseDTO
                {
                    CurrentPage = 1,
                    PageCount = 10,
                    PageSize = 20,
                    RowCount = 20,
                    Bets = new List<BetHistoryDTO>()
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
        private async Task<AuthenticationResponseDTO> Authenticate(string operatorToken, string endpoint)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken)) return null;

            _httpClient.BaseAddress = new Uri(endpoint);
            _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            var response = await _httpClient.PostAsync($"/v1/authentication", new StringContent(JsonSerializer.Serialize(new { }), System.Text.Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<AuthenticationResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            else
                return null;
        }
    }
}
