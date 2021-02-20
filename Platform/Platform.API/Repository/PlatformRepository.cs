using Microsoft.EntityFrameworkCore;
using Platform.API.Models;
using Platform.Shared.Data;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IOperatorRepository _operatorRepository;
        private readonly IBettingRepository _bettingRepository;
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly PlatformContext _platformContext;
        public PlatformRepository(IOperatorRepository operatorRepository, IBettingRepository bettingRepository, ISportsbookRepository sportsbookRepository, PlatformContext platformContext)
        {
            _operatorRepository = operatorRepository;
            _bettingRepository = bettingRepository;
            _sportsbookRepository = sportsbookRepository;
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
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
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
                            CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare
                        }
                    };
                }
            }
            else
            {
                //TODO: temp approach while using the old backoffice sportsbook client with ClientId as integer type
                var isInteger = int.TryParse(clientId, out int intClientId);

                if (isInteger)
                {
                    var result = await _platformContext.Client.Where(c => c.ClientIdRef == intClientId).FirstOrDefaultAsync();

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
                                CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare
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
                //
            }
        }

        public async Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(string apiKey, string clientId, BetConfigRequest request)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetConfigFEResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            if (!IsValidCurrencyCode(request.CurrencyCode))
            {
                return new BaseResponseDTO<BetConfigFEResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_CurrencyCode,
                    Message = StatusCode.Unsupported_CurrencyCode.ToString().Replace('_', ' ')
                };
            }

            #region Mock
            //return new BaseResponseDTO<BetConfigFEResponseDTO>
            //{
            //    StatusCode = StatusCode.Success,
            //    Message = StatusCode.Success.ToString(),
            //    Result = new BetConfigFEResponseDTO
            //    {
            //        DefaultSportConfig = new DefaultSportConfigFE
            //        {
            //            MinStake = 10,
            //            MaxStake = 100
            //        },
            //        OverrideSportConfig = new List<OverrideSportConfigFE>
            //        {
            //            new OverrideSportConfigFE
            //            {
            //                SportId = 1,
            //                SportName = "Soccer",
            //                MinStake = 1,
            //                MaxStake = 100
            //            },
            //            new OverrideSportConfigFE
            //            {
            //                SportId = 21,
            //                SportName = "Cricket",
            //                MinStake = 1,
            //                MaxStake = 100
            //            }
            //        },
            //        Parlayconfig = new ParlayConfigFE
            //        {
            //            MinStake = 10,
            //            MaxStake = 50
            //        }
            //    }
            //};
            #endregion


            var rate = await GetExchangeRate(request.CurrencyCode.ToString());

            if (rate.CurrencyCode == null)
            {
                return new BaseResponseDTO<BetConfigFEResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_CurrencyCode,
                    Message = StatusCode.Unsupported_CurrencyCode.ToString().Replace('_', ' ')
                };
            }

            var payload = new BetConfigRequestDTO()
            {
                CompanyShare = clientResponse.Result.CompanyShare,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                CurrencyCode = request.CurrencyCode.ToString(),
                ExchangeRate = rate.ExchangeRate
            };

            return await _bettingRepository.BetConfig(payload);
        }

        public async Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(string apiKey, string clientId, string operatorToken, LivestreamRequest request)
        {
            var isIPAddress = IPAddress.TryParse(request.IPAddress, out IPAddress ipAddress);

            if (!isIPAddress)
            {
                return new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = StatusCode.IPAddress_Must_Be_IPv4,
                    Message = StatusCode.IPAddress_Must_Be_IPv4.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            return await _sportsbookRepository.GetLivestream(new LivestreamRequestDTO 
            { 
                ProviderFixtureId = request.ProviderFixtureId, 
                IPAddress = request.IPAddress, 
                IsMobile = request.IsMobile 
            });
        }
        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(string apiKey, string clientId, TopLeaguesRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), string.IsNullOrEmpty(request.LanguageCode) ? "EN" : request.LanguageCode.ToUpper()) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), request.LanguageCode.ToUpper());

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTopLeagues(new TopLeaguesRequestDTO 
            { 
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }
        public async Task<BaseResponseDTO<TopFixturesResponseDTO>> GetTopFixtures(string apiKey, string clientId, TopFixturesRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), string.IsNullOrEmpty(request.LanguageCode) ? "EN" : request.LanguageCode.ToUpper()) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TopFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), request.LanguageCode.ToUpper());

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TopFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TopFixturesResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTopFixtures(new TopFixturesRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<OrderedSportsResponseDTO>> GetOrderedSports(string apiKey, string clientId, OrderedSportsRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), string.IsNullOrEmpty(request.LanguageCode) ? "EN" : request.LanguageCode.ToUpper()) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<OrderedSportsResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), request.LanguageCode.ToUpper());

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<OrderedSportsResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<OrderedSportsResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetOrderedSports(new OrderedSportsRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<OrderedLeaguesResponseDTO>> GetOrderedLeagues(string apiKey, string clientId, OrderedLeaguesRequest request)
        {
            if (!IsValidLanguageCode(request.LanguageCode))
            {
                return new BaseResponseDTO<OrderedLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<OrderedLeaguesResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetOrderedLeagues(new OrderedLeaguesRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = request.LanguageCode,
                SportId = request.SportId
            });
        }

        public async Task<BaseResponseDTO<ClientResponseDTO>> AddOrUpdateClient(ClientRequestDTO request)
        {
            Guid clientId;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var currentClient = await _platformContext.Client.AsNoTracking().FirstOrDefaultAsync(c => c.ClientIdRef == request.ClientIdRef);
                    decimal currentCompanyShare = request.CurrentCompanyShare;
                    decimal nextCompanyShare = request.CurrentCompanyShare;
                    string endPoint = string.IsNullOrEmpty(request.Endpoint) ? string.Empty : request.Endpoint;
                    string limitId = string.IsNullOrEmpty(request.LimitId) ? "0" : request.LimitId;
                    DateTime effectiveDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                    if (currentClient != null)
                    {
                        currentCompanyShare = currentClient.EffectiveDate < DateTime.UtcNow  && currentClient.CurrentCompanyShare != request.CurrentCompanyShare ?
                                                 currentClient.NextCompanyShare : currentClient.CurrentCompanyShare;
                        effectiveDate = currentClient.EffectiveDate < DateTime.UtcNow && currentClient.CurrentCompanyShare != request.CurrentCompanyShare ?
                                                GetFirstDayOfNextMonth() : currentClient.EffectiveDate;
                        nextCompanyShare = currentClient.CurrentCompanyShare != request.CurrentCompanyShare ?
                                                 request.CurrentCompanyShare : currentClient.NextCompanyShare;
                        endPoint = string.IsNullOrEmpty(request.Endpoint) ? currentClient.Endpoint : request.Endpoint;
                        limitId = string.IsNullOrEmpty(request.LimitId) ? currentClient.LimitId : request.LimitId;
                    }

                    var clientEntity = _platformContext.Client.Update(new Client
                    {
                        ClientId = currentClient != null ? currentClient.ClientId : Guid.Empty,
                        ClientCode = request.ClientCode,
                        ClientName = request.ClientName,
                        ClientIdRef = request.ClientIdRef,
                        IsActive = request.IsActive,
                        Endpoint = endPoint,
                        LimitId = limitId,
                        CurrentCompanyShare = currentCompanyShare,
                        EffectiveDate = effectiveDate,
                        NextCompanyShare = nextCompanyShare
                    }).Entity;

                    clientId = clientEntity.ClientId;

                    await _platformContext.SaveChangesAsync();

                    scope.Complete();
                }
                catch(Exception ex)
                {
                    scope.Dispose();
                    return new BaseResponseDTO<ClientResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };
                }
            }
            return new BaseResponseDTO<ClientResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ClientResponseDTO
                {
                    ClientId = clientId
                }
            };
        }

        private DateTime GetFirstDayOfNextMonth()
        {
            DateTime ss = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            return ss.AddMonths(1);
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
