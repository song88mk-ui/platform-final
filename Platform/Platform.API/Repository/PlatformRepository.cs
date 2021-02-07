using Microsoft.Extensions.Configuration;
using Platform.API.Data;
using Platform.API.Data.Entities;
using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IOperatorRepository _operatorRepository;
        private readonly HttpClient _httpClient;
        private readonly PlatformContext _platformContext;
        private readonly IConfiguration _configuration;
        public PlatformRepository(IOperatorRepository operatorRepository, HttpClient httpClient, PlatformContext platformContext, IConfiguration configuration)
        {
            _operatorRepository = operatorRepository;
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

            var authenticate = await _operatorRepository.Authenticate(operatorToken, client.Endpoint);

            if (!authenticate.IsSuccessStatusCode)
            {
                if (authenticate.StatusCode == HttpStatusCode.BadRequest)
                {
                    return new BaseResponseDTO<WalletBalanceResponseDTO>
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                    };
                }
                else
                {
                    return new BaseResponseDTO<WalletBalanceResponseDTO>
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({authenticate.ReasonPhrase})",
                    };
                }
            }

            var result = JsonSerializer.Deserialize<AuthenticationResponseDTO>(await authenticate.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            return new BaseResponseDTO<WalletBalanceResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WalletBalanceResponseDTO
                {
                    UserId = result.UserId,
                    Balance = result.Balance
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

            var authenticate = await _operatorRepository.Authenticate(operatorToken, client.Endpoint);

            if (!authenticate.IsSuccessStatusCode)
            {
                if (authenticate.StatusCode == HttpStatusCode.BadRequest)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                    };
                }
                else
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({authenticate.ReasonPhrase})",
                    };
                }
            }

            var result = JsonSerializer.Deserialize<AuthenticationResponseDTO>(await authenticate.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            if (result.Balance < request.Stake)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Insufficient_Funds,
                    Message = StatusCode.Insufficient_Funds.ToString().Replace('_', ' '),
                };
            }

            var rate = await GetExchangeRate(result.CurrencyCode);

            var payload = new BetRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientId,
                ClientCode = client.ClientCode,
                LimitId = client.LimitId,
                Endpoint = client.Endpoint,
                CompanyShare = client.CompanyShare,
                ExchangeRate = rate.ExchangeRate,
                UserId = result.UserId,
                Stake = request.Stake,
                BetDate = request.TransactionDate,
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
                    FixtureType = s.FixtureType,
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

            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<BaseResponseDTO<BetResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            else
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                };
            }
        }
        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string operatorToken, BetHistoryRequest request)
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

            var authenticate = await _operatorRepository.Authenticate(operatorToken, client.Endpoint);

            if (!authenticate.IsSuccessStatusCode)
            {
                if (authenticate.StatusCode == HttpStatusCode.BadRequest)
                {
                    return new BaseResponseDTO<BetHistoryResponseDTO>
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                    };
                }
                else
                {
                    return new BaseResponseDTO<BetHistoryResponseDTO>
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({authenticate.ReasonPhrase})",
                    };
                }
            }

            var result = JsonSerializer.Deserialize<AuthenticationResponseDTO>(await authenticate.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            var payload = new BetHistoryRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientId,
                UserId = result.UserId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                BetStatusFilter = request.BetStatusFilter,
                Page = request.Page,
                PageSize = request.PageSize
            };

            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformBettingAPI"]);

            var response = await _httpClient.PostAsync($"/v1/bet/history", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<BaseResponseDTO<BetHistoryResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            else
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                };
            }
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
                    Endpoint = result.Endpoint,
                    CompanyShare = result.CompanyShare
                };
            }
            else
                return null;
        }
        private async Task<ForexEURResponse> GetExchangeRate(string currencyCode)
        {
            var result = await _platformContext.ForexEUR.FindAsync(currencyCode);

            if (result == null)
                return new ForexEURResponse();

            return new ForexEURResponse
            {
                CurrencyCode = result.CurrencyCode,
                ExchangeRate = result.ExchangeRate
            };
        }
    }
}
