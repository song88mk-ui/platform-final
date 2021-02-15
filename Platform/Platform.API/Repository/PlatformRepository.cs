using Platform.API.Models;
using Platform.Shared.Data;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IOperatorRepository _operatorRepository;
        private readonly IBettingRepository _bettingRepository;
        private readonly PlatformContext _platformContext;
        public PlatformRepository(IOperatorRepository operatorRepository, IBettingRepository bettingRepository, PlatformContext platformContext)
        {
            _operatorRepository = operatorRepository;
            _bettingRepository = bettingRepository;
            _platformContext = platformContext;
        }
        public async Task<BaseResponseDTO<WalletBalanceResponseDTO>> GetWalletBalance(string clientId, string operatorToken)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<WalletBalanceResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<WalletBalanceResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            return new BaseResponseDTO<WalletBalanceResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new WalletBalanceResponseDTO
                {
                    UserId = authenticationResponse.Result.UserId,
                    Balance = authenticationResponse.Result.Balance,
                    CurrencyCode = authenticationResponse.Result.CurrencyCode
                }
            };
        }
        public async Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(string apiKey, string clientId, string operatorToken, BetRequest request)
        {
            if (request.Stake <= 0)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Stake_Must_Be_Greater_Than_Zero,
                    Message = StatusCode.Stake_Must_Be_Greater_Than_Zero.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidOddsType(request.OddsType))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_OddsType,
                    Message = StatusCode.Unsupported_OddsType.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidChannel(request.Channel))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_Channel,
                    Message = StatusCode.Unsupported_Channel.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidFixtureType(request.Selections.Select(s => s.FixtureType).ToList()))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_FixtureType,
                    Message = StatusCode.Unsupported_FixtureType.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidFixtureStatus(request.Selections.Select(s => s.FixtureStatus).ToList()))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_FixtureStatus,
                    Message = StatusCode.Unsupported_FixtureStatus.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidOdds(request.Selections.Select(s => s.Odds).ToList()))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Odds_Must_Be_Greater_Or_Higher_To_1,
                    Message = StatusCode.Odds_Must_Be_Greater_Or_Higher_To_1.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            if (authenticationResponse.Result.Balance < request.Stake)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Insufficient_Funds,
                    Message = StatusCode.Insufficient_Funds.ToString().Replace('_', ' '),
                };
            }

            var rate = await GetExchangeRate(authenticationResponse.Result.CurrencyCode);

            var payload = new BetRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientId,
                ClientCode = clientResponse.Result.ClientCode,
                LimitId = clientResponse.Result.LimitId,
                Endpoint = clientResponse.Result.Endpoint,
                CompanyShare = clientResponse.Result.CompanyShare,
                ExchangeRate = rate.ExchangeRate,
                UserId = authenticationResponse.Result.UserId,
                Stake = request.Stake,
                BetDate = request.TransactionDate,
                OddsType = request.OddsType,
                CurrencyCode = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), authenticationResponse.Result.CurrencyCode),
                LanguageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), authenticationResponse.Result.LanguageCode),
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

            return await _bettingRepository.PlaceBet(payload);
        }
        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string operatorToken, BetHistoryRequest request)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            var payload = new BetHistoryRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientId,
                UserId = authenticationResponse.Result.UserId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                BetStatusFilter = request.BetStatusFilter,
                IsCurrent = request.IsCurrent,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return await _bettingRepository.GetBetHistory(payload);
        }
        private async Task<BaseResponseDTO<ClientResponse>> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid)
            {
                var result = await _platformContext.Client.FindAsync(guid);

                if (result == null || result.IsActive == false)
                {
                    return new BaseResponseDTO<ClientResponse>
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                    };
                }
                else
                {
                    return new BaseResponseDTO<ClientResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString().Replace('_', ' '),
                        Result = new ClientResponse
                        {
                            ClientId = result.ClientId,
                            ClientName = result.ClientName,
                            ClientCode = result.ClientCode,
                            LimitId = result.LimitId,
                            IsActive = result.IsActive,
                            Endpoint = result.Endpoint,
                            CompanyShare = result.CompanyShare
                        }
                    };
                }
            }
            else
            {
                return new BaseResponseDTO<ClientResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }
        }

        public async Task<BaseResponseDTO<BetConfigResponseDTO>> GetBetConfig(string apiKey, string clientId, BetConfigRequestDTO request)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetConfigResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return new BaseResponseDTO<BetConfigResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetConfigResponseDTO
                {
                    DefaultSportConfig = new DefaultSportConfig
                    {
                        MinStake = 10,
                        MaxStake = 100
                    },
                    OverrideSportConfig = new List<OverrideSportConfig>
                    {
                        new OverrideSportConfig
                        {
                            SportId = 1,
                            SportName = "Soccer",
                            MinStake = 1,
                            MaxStake = 100
                        },
                        new OverrideSportConfig
                        {
                            SportId = 21,
                            SportName = "Cricket",
                            MinStake = 1,
                            MaxStake = 100
                        }
                    },
                    Parlayconfig = new ParlayConfig
                    {
                        MinStake = 10,
                        MaxStake = 50
                    }
                }
            };
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
        private bool IsMatchCurrencyCode(CurrencyCode requestCurrencyCode, string userCurrencyCode)
        {
            return requestCurrencyCode.ToString().ToUpper() == userCurrencyCode.ToUpper() ? true : false;
        }
        private bool IsValidCurrencyCode(CurrencyCode currencyCode)
        {
            return Enum.IsDefined(typeof(CurrencyCode), currencyCode) ? true : false;
        }
        private bool IsValidLanguageCode(LanguageCode languageCode)
        {
            return Enum.IsDefined(typeof(LanguageCode), languageCode) ? true : false;
        }
        private bool IsValidOddsType(OddsType oddsType)
        {
            return Enum.IsDefined(typeof(OddsType), oddsType) ? true : false;
        }
        private bool IsValidChannel(Channel channel)
        {
            return Enum.IsDefined(typeof(Channel), channel) ? true : false;
        }
        private bool IsValidFixtureType(List<FixtureType> fixtureTypes)
        {
            foreach (var item in fixtureTypes)
            {
                if (!Enum.IsDefined(typeof(FixtureType), item))
                    return false;
            }

            return true;
        }
        private bool IsValidFixtureStatus(List<FixtureStatus> fixtureStatus)
        {
            foreach (var item in fixtureStatus)
            {
                if (!Enum.IsDefined(typeof(FixtureStatus), item))
                    return false;
            }

            return true;
        }

        private bool IsValidOdds(List<decimal> odds)
        {
            foreach (var item in odds)
            {
                if (item <= 1m)
                    return false;
            }

            return true;
        }

    }
}
