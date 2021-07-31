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
using System.Text.Json;
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
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<WalletBalanceResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

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
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidIPAddress(request.IPAddress))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.IPAddress_Must_Be_IPv4,
                    Message = StatusCode.IPAddress_Must_Be_IPv4.ToString().Replace('_', ' '),
                };
            }

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

            var result = await _sportsbookRepository.GetBetFixture(new BetFixtureRequestDTO
            {
                OddsChangeType = request.OddsChangeType,
                Selections = request.Selections.Select(s => new BetFixtureSelectionRequestDTO 
                { 
                    SportId = s.SportId,
                    FixtureId = Convert.ToInt64(s.FixtureId),
                    MarketId = Convert.ToInt64(s.MarketId),
                    SelectionId = Convert.ToInt64(s.SelectionId),
                    Odds = s.Odds,
                    CorrelationId = s.CorrelationId
                }).ToList()
            });

            if (result.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace('_', ' '),
                };
            }

            if (result.Result.Selections.Any(s => s.BetSelectionState.StatusCode != StatusCode.Success))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace('_', ' '),
                    Result = new BetResponseDTO
                    {
                        BetId = string.Empty,
                        UserId = string.Empty,
                        BetSelections = result.Result.Selections.Select(s => s.BetSelectionState).ToList()
                    }
                };
            }

            var payload = new BetRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                ClientIdRef = clientResponse.Result.ClientIdRef,
                ClientCode = clientResponse.Result.ClientCode,
                ClientName = clientResponse.Result.ClientName,
                LimitId = clientResponse.Result.LimitId,
                Endpoint = clientResponse.Result.Endpoint,
                CompanyShare = clientResponse.Result.CompanyShare,
                ExchangeRate = rate.ExchangeRate,
                UserId = authenticationResponse.Result.UserId,
                Stake = request.Stake,
                BetDate = DateTime.UtcNow,
                OddsType = request.OddsType,
                CurrencyCode = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), authenticationResponse.Result.CurrencyCode),
                LanguageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), authenticationResponse.Result.LanguageCode) == LanguageCode.ZHT ? LanguageCode.ZH : (LanguageCode)Enum.Parse(typeof(LanguageCode), authenticationResponse.Result.LanguageCode),
                Channel = request.Channel,
                IPAddress = request.IPAddress,
                Selections = request.Selections.Select(s => new BetSelectionRequestDTO
                {
                    SportId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SportId).FirstOrDefault(),
                    SportName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SportName).FirstOrDefault(),
                    SegmentId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SegmentId).FirstOrDefault(),
                    SegmentName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SegmentName).FirstOrDefault(),
                    LeagueId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.LeagueId).FirstOrDefault(),
                    LeagueName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.LeagueName).FirstOrDefault(),
                    FixtureId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureId).FirstOrDefault(),
                    FixtureName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureName).FirstOrDefault(),
                    FixtureType = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureType).FirstOrDefault(),
                    MarketTemplateId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.MarketTemplateId).FirstOrDefault(),
                    MarketId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.MarketId).FirstOrDefault(),
                    MarketName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.MarketName).FirstOrDefault(),
                    Line = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Line).FirstOrDefault(),
                    SelectionTemplateId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionTemplateId).FirstOrDefault(),
                    SelectionId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionId).FirstOrDefault(),
                    SelectionName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionName).FirstOrDefault(),
                    Odds = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Odds).FirstOrDefault(),
                    FixtureStatus = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureStatus).FirstOrDefault(),
                    FixtureStartDate = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureStartDate).FirstOrDefault(),
                    BetScore = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Score).FirstOrDefault(),
                    FixtureSpecifier = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureSpecifier).FirstOrDefault(),
                    SelectionSpecifier = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionSpecifier).FirstOrDefault(),
                    CorrelationId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.CorrelationId).FirstOrDefault(),
                    Probability = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Probability).FirstOrDefault(),
                    WonProbability = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.WonProbability).FirstOrDefault(),
                    LostProbability = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.LostProbability).FirstOrDefault(),
                    VoidProbability = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.VoidProbability).FirstOrDefault(),
                    HalfWonProbability = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.HalfWonProbability).FirstOrDefault(),
                    HalfLostProbability = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.HalfLostProbability).FirstOrDefault()
                }).ToList()
            };

            return await _bettingRepository.PlaceBet(payload);
        }
        public async Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(string apiKey, string clientId, string operatorToken, BetRequest request)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            if (!IsValidIPAddress(request.IPAddress))
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.IPAddress_Must_Be_IPv4,
                    Message = StatusCode.IPAddress_Must_Be_IPv4.ToString().Replace('_', ' '),
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            if (!IsValidOddsType(request.OddsType))
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_OddsType,
                    Message = StatusCode.Unsupported_OddsType.ToString().Replace('_', ' '),
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            if (!IsValidChannel(request.Channel))
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_Channel,
                    Message = StatusCode.Unsupported_Channel.ToString().Replace('_', ' '),
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            if (!IsValidOdds(request.Selections.Select(s => s.Odds).ToList()))
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.Odds_Must_Be_Greater_Or_Higher_To_1,
                    Message = StatusCode.Odds_Must_Be_Greater_Or_Higher_To_1.ToString().Replace('_', ' '),
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message,
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            var rate = await GetExchangeRate(authenticationResponse.Result.CurrencyCode);

            var result = await _sportsbookRepository.GetBetFixture(new BetFixtureRequestDTO
            {
                OddsChangeType = request.OddsChangeType,
                Selections = request.Selections.Select(s => new BetFixtureSelectionRequestDTO
                {
                    SportId = s.SportId,
                    FixtureId = Convert.ToInt64(s.FixtureId),
                    MarketId = Convert.ToInt64(s.MarketId),
                    SelectionId = Convert.ToInt64(s.SelectionId),
                    Odds = s.Odds,
                    CorrelationId = s.CorrelationId
                }).ToList()
            });

            if (result.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace('_', ' '),
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            var payload = new BetRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                ClientIdRef = clientResponse.Result.ClientIdRef,
                ClientCode = clientResponse.Result.ClientCode,
                ClientName = clientResponse.Result.ClientName,
                LimitId = clientResponse.Result.LimitId,
                Endpoint = clientResponse.Result.Endpoint,
                CompanyShare = clientResponse.Result.CompanyShare,
                ExchangeRate = rate.ExchangeRate,
                UserId = authenticationResponse.Result.UserId,
                Stake = request.Stake,
                BetDate = DateTime.UtcNow,
                OddsType = request.OddsType,
                CurrencyCode = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), authenticationResponse.Result.CurrencyCode),
                LanguageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), authenticationResponse.Result.LanguageCode) == LanguageCode.ZHT ? LanguageCode.ZH : (LanguageCode)Enum.Parse(typeof(LanguageCode), authenticationResponse.Result.LanguageCode),
                Channel = request.Channel,
                IPAddress = request.IPAddress,
                Selections = request.Selections.Select(s => new BetSelectionRequestDTO
                {
                    SportId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SportId).FirstOrDefault(),
                    SportName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SportName).FirstOrDefault(),
                    SegmentId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SegmentId).FirstOrDefault(),
                    SegmentName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SegmentName).FirstOrDefault(),
                    LeagueId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.LeagueId).FirstOrDefault(),
                    LeagueName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.LeagueName).FirstOrDefault(),
                    FixtureId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureId).FirstOrDefault(),
                    FixtureName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureName).FirstOrDefault(),
                    FixtureType = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureType).FirstOrDefault(),
                    MarketTemplateId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.MarketTemplateId).FirstOrDefault(),
                    MarketId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.MarketId).FirstOrDefault(),
                    MarketName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.MarketName).FirstOrDefault(),
                    Line = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Line).FirstOrDefault(),
                    SelectionTemplateId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionTemplateId).FirstOrDefault(),
                    SelectionId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionId).FirstOrDefault(),
                    SelectionName = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionName).FirstOrDefault(),
                    Odds = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Odds).FirstOrDefault(),
                    FixtureStatus = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureStatus).FirstOrDefault(),
                    FixtureStartDate = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureStartDate).FirstOrDefault(),
                    BetScore = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Score).FirstOrDefault(),
                    FixtureSpecifier = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.FixtureSpecifier).FirstOrDefault(),
                    SelectionSpecifier = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.SelectionSpecifier).FirstOrDefault(),
                    CorrelationId = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.CorrelationId).FirstOrDefault()
                }).ToList()
            };

            return await _bettingRepository.GetMaxStake(payload);
        }
        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string langCode, string operatorToken, BetHistoryRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

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
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                UserId = authenticationResponse.Result.UserId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                BetStatusFilter = request.BetStatusFilter,
                IsCurrent = request.IsCurrent,
                Page = request.Page,
                PageSize = request.PageSize
            };

            if (languageCode == LanguageCode.EN)
                return await _bettingRepository.GetBetHistory(payload);
            else
            {
                var betHistory = await _bettingRepository.GetBetHistory(payload);

                if (betHistory.Result.Bets.Count == 0)
                    return betHistory;
               
                var betFixtureTranslation = new BetFixtureTranslationRequestDTO();
                betFixtureTranslation.LanguageCode = languageCode;

                foreach (var bet in betHistory.Result.Bets)
                {
                    foreach (var selection in bet.Selections)
                    {
                        if (betFixtureTranslation.Selections.Any(c => c.SelectionId == Convert.ToInt64(selection.SelectionId))) continue;

                        betFixtureTranslation.Selections.Add(new BetFixtureSelectionTranslationRequestDTO
                        {
                            FixtureId = Convert.ToInt64(selection.FixtureId),
                            MarketId = Convert.ToInt64(selection.MarketId),
                            SelectionId = Convert.ToInt64(selection.SelectionId)
                        });
                    }
                }

                var translations = await _sportsbookRepository.GetBetFixtureTranslation(betFixtureTranslation);

                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    Message = betHistory.Message,
                    StatusCode = betHistory.StatusCode,
                    Result = new BetHistoryResponseDTO
                    {
                        Bets = betHistory.Result.Bets.Select(s => new BetHistoryDTO
                        {
                            BetId = s.BetId,
                            Stake = s.Stake,
                            Odds = s.Odds,
                            Payout = s.Payout,
                            WinLoss = s.WinLoss,
                            BetStatusId = s.BetStatusId,
                            BetStatusName = s.BetStatusName,
                            TransactionDate = s.TransactionDate,
                            Commission = s.Commission,
                            CurrencyCode = s.CurrencyCode,
                            OddsType = s.OddsType,
                            CashOutStake = s.CashOutStake,
                            CashOutStatusId = s.CashOutStatusId,
                            CashOutStatusName = s.CashOutStatusName,
                            StakeTax = s.StakeTax,
                            WinningsTax = s.WinningsTax,
                            Selections = GetTranslatedBetSelectionHistory(s.Selections, translations.Result.Selections)
                        }).ToList()
                    }
                };
            }
        }

        private List<BetSelectionHistoryDTO> GetTranslatedBetSelectionHistory(List<BetSelectionHistoryDTO> selections, List<BetFixtureSelectionTranslationResponseDTO> translations)
        {
            var result = new List<BetSelectionHistoryDTO>();

            foreach (var selection in selections)
            {
                var translation = translations.Where(c => c.FixtureId == Convert.ToInt64(selection.FixtureId) && c.MarketId == Convert.ToInt64(selection.MarketId) && c.SelectionId == Convert.ToInt64(selection.SelectionId)).FirstOrDefault();

                result.Add(new BetSelectionHistoryDTO
                {
                    SportId = selection.SportId,
                    SportName = translation == null ? selection.SportName : translation.SportName,
                    SegmentId = selection.SegmentId,
                    SegmentName = translation == null ? selection.SegmentName : translation.SegmentName,
                    LeagueId = selection.LeagueId,
                    LeagueName = translation == null ? selection.LeagueName : translation.LeagueName,
                    FixtureId = selection.FixtureId.ToString(),
                    FixtureName = translation == null ? selection.FixtureName : translation.FixtureName,
                    MarketId = selection.MarketId.ToString(),
                    MarketName = translation == null ? selection.MarketName : translation.MarketName,
                    Line = selection.Line,
                    SelectionId = selection.SelectionId.ToString(),
                    SelectionName = translation == null ? selection.SelectionName : translation.SelectionName,
                    Odds = selection.Odds,
                    BetScore = selection.BetScore,
                    SettlementScore = selection.SettlementScore,
                    BetSelectionId = selection.BetSelectionId.ToString(),
                    FixtureStartDate = selection.FixtureStartDate,
                    FixtureStatusId = selection.FixtureStatusId,
                    FixtureStatusName = selection.FixtureStatusName,
                    MarketTemplateId = selection.MarketTemplateId,
                    SelectionTemplateId = selection.SelectionTemplateId,
                    BetSelectionStatusId = selection.BetSelectionStatusId,
                    BetSelectionStatusName = selection.BetSelectionStatusName
                });
            }

            return result;
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
                            CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare,
                            ClientIdRef = result.ClientIdRef
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
                                CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare,
                                ClientIdRef = result.ClientIdRef
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
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<LivestreamResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

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
        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetTopLeagues(string apiKey, string clientId, string langCode, TopLeaguesRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

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
        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues(string apiKey, string clientId, string langCode, TopLeaguesRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

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

            return await _sportsbookRepository.GetAllLeagues(new TopLeaguesRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }

        public async Task<BaseResponseDTO<TopLeaguesResponseDTO>> GetAllLeagues2(string apiKey, string clientId, TopLeaguesRequest2 request)
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

            return await _sportsbookRepository.GetAllLeagues2(new TopLeaguesRequest2DTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId,
                LeagueId = request.LeagueId,
                SegmentId = request.SegmentId
            });
        }


        public async Task<BaseResponseDTO<TopFixturesIdsResponseDTO>> GetTopFixtureIds(string apiKey, string clientId, string langCode)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TopFixturesIdsResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TopFixturesIdsResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TopFixturesIdsResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTopFixtureIds(new TWRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<MarketTemplateGroupResponseDTO>> GetMarketTemplateGroups(string apiKey, string clientId)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<MarketTemplateGroupResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetMarketTemplateGroups();
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures(string apiKey, string clientId, string langCode)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTopFixtures2(new TWRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(string apiKey, string clientId, string langCode)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetLiveFixtureCount(new TWFLiveFixtureCountRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
            });
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(string apiKey, string clientId, string langCode, TodayFixtureCountRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTodayFixtureCount(new TWFTodayFixtureCountRequestDTO
            {
                MinuteUTCOffset = request.MinuteUTCOffset,
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(string apiKey, string clientId, string langCode, FixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetLiveFixtures(new TWLiveFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(string apiKey, string clientId, string langCode, FixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetLiveFixturesByLeague(new TWLeagueLiveFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId,
                SegmentId = request.SegmentId,
                LeagueId = request.LeagueId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(string apiKey, string clientId, string langCode, TodayFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTodayFixtures(new TWTodayFixtureRequestDTO
            {
                MinuteUTCOffset = request.MinuteUTCOffset,
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(string apiKey, string clientId, string langCode, TodayFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTodayFixturesByLeague(new TWLeagueTodayFixtureRequestDTO
            {
                MinuteUTCOffset = request.MinuteUTCOffset,
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId,
                SegmentId = request.SegmentId,
                LeagueId = request.LeagueId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(string apiKey, string clientId, string langCode, UpcomingFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetUpcomingFixtures(new TWUpcomingFixtureRequestDTO
            {
                MinuteUTCOffset = request.MinuteUTCOffset,
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(string apiKey, string clientId, string langCode, DailyFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetDailyFixtures(new TWDailyFixtureRequestDTO
            {
                DateUTC = request.DateUTC,
                MinuteUTCOffset = request.MinuteUTCOffset,
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(string apiKey, string clientId, string langCode, DailyFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetDailyFixturesByLeague(new TWLeagueDailyFixtureRequestDTO
            {
                DateUTC = request.DateUTC,
                MinuteUTCOffset = request.MinuteUTCOffset,
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId,
                SegmentId = request.SegmentId,
                LeagueId = request.LeagueId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(string apiKey, string clientId, string langCode, TopLeagueFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetTopLeagueFixtures(new TWTopLeagueFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(string apiKey, string clientId, string langCode, SegmentFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetSegmentFixtures(new TWSegmentFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId,
                SegmentId = request.SegmentId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(string apiKey, string clientId, string langCode, LeagueFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetFixtures(new TWLeagueFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId,
                SegmentId = request.SegmentId,
                LeagueId = request.LeagueId
            });
        }
        public async Task<BaseResponseDTO<SportsResponseDTO>> GetSports(string apiKey, string clientId, string langCode)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<SportsResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<SportsResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<SportsResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetSports(new SportsRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(string apiKey, string clientId, string langCode, FixtureDetailsRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetFixtureDetails(new TWFixtureDetailsRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                FixtureId = request.FixtureId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails(string apiKey, string clientId, string langCode, FixtureMarketSelectionDetailsRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetSelectionDetails(new TWFixtureMarketSelectionDetailsRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                FixtureId = request.FixtureId,
                MarketId = request.MarketId,
                SelectionId = request.SelectionId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(string apiKey, string clientId, string langCode, OutrightFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetOutrightFixtures(new TWOutrightFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(string apiKey, string clientId, string langCode, OutrightFixtureRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetOutrightFixturesByLeague(new TWLeagueOutrightFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                SportId = request.SportId,
                SegmentId = request.SegmentId,
                LeagueId = request.LeagueId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(string apiKey, string clientId, string langCode, string operatorToken)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            return await _sportsbookRepository.GetUserFavorites2(new UserFavoriteRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                UserId = authenticationResponse.Result.UserId,
                ClientCode = clientResponse.Result.ClientCode,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(string apiKey, string clientId, string langCode, SearchResultRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<TWSearchResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<TWSearchResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(request.SearchText) || string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText.Length < 3)
            {
                return new BaseResponseDTO<TWSearchResponseDTO>
                {
                    StatusCode = StatusCode.Search_Text_Needs_To_Be_3_Characters_Or_Above,
                    Message = StatusCode.Search_Text_Needs_To_Be_3_Characters_Or_Above.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWSearchResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetSearchResults(new TWSearchRequestDTO
            {
                SearchText = request.SearchText,
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(string apiKey, string clientId, string langCode, string operatorToken)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            return await _sportsbookRepository.GetUserFavorites(new UserFavoriteRequestDTO 
            { 
                ClientId = clientResponse.Result.ClientId,
                UserId = authenticationResponse.Result.UserId,
                ClientCode = clientResponse.Result.ClientCode,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> AddUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, InsertDeleteUserFavoritesRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            return await _sportsbookRepository.AddUserFavorites(new UserFavoriteInsertDeleteRequestDTO
            {
                UserId = authenticationResponse.Result.UserId,
                ClientCode = clientResponse.Result.ClientCode,
                Id = request.Id,
                UserFavoriteHierarchy = request.UserFavoriteHierarchy,
                LanguageCode = languageCode
            });
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> DeleteUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, InsertDeleteUserFavoritesRequest request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<UserFavoriteResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            return await _sportsbookRepository.DeleteUserFavorites(new UserFavoriteInsertDeleteRequestDTO
            {
                UserId = authenticationResponse.Result.UserId,
                ClientCode = clientResponse.Result.ClientCode,
                Id = request.Id,
                UserFavoriteHierarchy = request.UserFavoriteHierarchy,
                LanguageCode = languageCode
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
                        MonthlyFee = request.MonthlyFee,
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

        public async Task<BaseResponseDTO<bool>> ProcessStatusUpdate(ClientStatusRequestDTO request)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var currentClient = await _platformContext.Client.AsNoTracking().FirstOrDefaultAsync(c => c.ClientIdRef == request.ClientIdRef);

                    currentClient.IsActive = request.IsActive;

                    _platformContext.Client.Update(currentClient);

                    await _platformContext.SaveChangesAsync();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return new BaseResponseDTO<bool>
                    {
                        Result = false,
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };
                }
            }
            return new BaseResponseDTO<bool>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = true
            };
        }

        public async Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures(string strClientRefIds)
        {
            var clientRefIds = strClientRefIds.Split(',').ToList().ConvertAll(int.Parse);

            var clients = _platformContext.Client.Where(c => clientRefIds.Contains(c.ClientIdRef)).ToList();
            var clientIds = clients.Select(s => s.ClientId).ToList();

            var sportsbookRequest = new ClientTopFixtureRequestDTO()
            {
                ClientIds = clientIds,
                LanguageCode = LanguageCode.EN
            };

            var response = await _sportsbookRepository.GetClientTopFixtures(sportsbookRequest);

            foreach (var topFixture in response.Result.TopFixtures)
            {
                var client = clients.FirstOrDefault(c => c.ClientId == topFixture.ClientId);

                if (client != null)
                {
                    topFixture.ClientIdRef = client.ClientIdRef;
                    topFixture.ClientName = client.ClientName;
                }
            }

            return response;
        }

        public async Task<BaseResponseDTO<BackofficeSportsResponseDTO>> GetSportstreeDetails(int clientRefId)
        {
            var client = _platformContext.Client.FirstOrDefault(c => c.ClientIdRef == clientRefId);

            var sportsbookRequest = new BackofficeSportsRequestDTO()
            {
                ClientId = client.ClientId,
                LanguageCode = LanguageCode.EN
            };

            return await _sportsbookRepository.GetSportstreeDetails(sportsbookRequest);
        }

        public async Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> PostClientTopFixtures(ClientTopFixtureRequest request)
        {
            var client = _platformContext.Client.FirstOrDefault(c => c.ClientIdRef == request.ClientId);

            var sportsbookRequest = new PostClientTopFixtureRequestDTO()
            {
                ClientId = client.ClientId,
                TopFixtures = request.TopFixtures.Select(s => new Shared.Models.ClientTopFixtureSetup
                { 
                    FixtureId = s.FixtureId,
                    SortOrder = s.SortOrder
                }).ToList()
            };

            return await _sportsbookRepository.PostClientTopFixtures(sportsbookRequest);
        }

        public async Task<BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>> GetClientTopLeagues(BackOfficeTopLeaguesRequestDTO request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), string.IsNullOrEmpty(request.LanguageCode) ? "EN" : request.LanguageCode.ToUpper()) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), request.LanguageCode.ToUpper());

            var clientIdRefs = request.ClientIdRefs.Split(',').ToList().ConvertAll(int.Parse);

            var clientResult = await _platformContext.Client.Where(c => clientIdRefs.Contains(c.ClientIdRef)).ToListAsync();
            if (clientResult == null || clientResult.Count == 0)
            {
                return new BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var sportsbookRequest = new ClientTopLeaguesRequestDTO()
            {
                ClientIds = clientResult.Select(c => c.ClientId).ToList(),
                SportId = request.SportId,
                LanguageCode = languageCode,
                LeagueId = request.LeagueId,
                SegmentId = request.SegmentId
            };

            var clientTopLeaguesResponse = await _sportsbookRepository.GetClientTopLeagues(sportsbookRequest);
            if(clientTopLeaguesResponse.StatusCode == StatusCode.Success)
            {
                var backOfficeResponse = new BackOfficeTopLeaguesResponseDTO()
                {
                    ClientTopLeagues = clientTopLeaguesResponse.Result.ClientTopLeagues.Select(c => new BackOfficeClientTopLeague()
                    {
                        ClientIdRef = clientResult.Where(o => o.ClientId == c.ClientId).FirstOrDefault().ClientIdRef,
                        TopLeagues = c.TopLeagues
                    }).ToList()
                };

                return new BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>
                {
                    StatusCode = clientTopLeaguesResponse.StatusCode,
                    Message = clientTopLeaguesResponse.Message,
                    Result = backOfficeResponse
                };
            }
            else
            {
                return new BaseResponseDTO<BackOfficeTopLeaguesResponseDTO>
                {
                    StatusCode = clientTopLeaguesResponse.StatusCode,
                    Message = clientTopLeaguesResponse.Message
                };

            }
        }

        public async Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> AddOrUpdateTopLeagues(BackOfficeClientTopLeague request)
        {
            var result = await _platformContext.Client.Where(c => c.ClientIdRef == request.ClientIdRef).FirstOrDefaultAsync();
            if (result == null)
            {
                return new BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientTopLeaguesMessageResponseDTO()
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                    }
                };
            }

            var sportsbookRequest = new ClientTopLeague()
            {
                ClientId = result.ClientId,
                SportId = request.SportId,
                TopLeagues = request.TopLeagues
            };

            return await _sportsbookRepository.AddOrUpdateTopLeagues(sportsbookRequest);
        }

        public async Task<BaseResponseDTO<SportsLookupResponseDTO>> GetClientSportsLookup(BackofficeClientSportsRequestDTO request)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), string.IsNullOrEmpty(request.LanguageCode) ? "EN" : request.LanguageCode.ToUpper()) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<SportsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), request.LanguageCode.ToUpper());

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<SportsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            return await _sportsbookRepository.GetClientSportsLookup(new SportsLookupRequestDTO
            {
                LanguageCode = languageCode
            });
        }

        public async Task<BaseResponseDTO<BetCashOutResponseDTO>> CashoutBet(string apiKey, string clientId, string operatorToken, CashoutBetRequest request)
        {
            if (!Guid.TryParse(request.BetId, out Guid betId))
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.Bet_Not_Found,
                    Message = StatusCode.Bet_Not_Found.ToString().Replace('_', ' '),
                };
            }

            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidIPAddress(request.IPAddress))
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.IPAddress_Must_Be_IPv4,
                    Message = StatusCode.IPAddress_Must_Be_IPv4.ToString().Replace('_', ' '),
                };
            }

            if (!IsValidChannel(request.Channel))
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_Channel,
                    Message = StatusCode.Unsupported_Channel.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            return await _bettingRepository.CashOutBet(new CashoutBetRequestDTO
            {
                APIKey = apiKey,
                OperatorToken = operatorToken,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                ClientIdRef = clientResponse.Result.ClientIdRef,
                ClientCode = clientResponse.Result.ClientCode,
                ClientName = clientResponse.Result.ClientName,
                LimitId = clientResponse.Result.LimitId,
                Endpoint = clientResponse.Result.Endpoint,
                CompanyShare = clientResponse.Result.CompanyShare,
                BetId = betId,
                UserId = authenticationResponse.Result.UserId,
                CashOutStake = request.CashoutStake
            });
        }

        public async Task<BaseResponseDTO<ExchangeRateResponseDTO>> GetCurrentExchangeRate(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode))
            {
                return new BaseResponseDTO<ExchangeRateResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString()
                };
            }

            var result = await _platformContext.ForexEUR.FindAsync(currencyCode);

            if (result == null)
            {
                return new BaseResponseDTO<ExchangeRateResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString()
                };
            }
            else
            {
                return new BaseResponseDTO<ExchangeRateResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ExchangeRateResponseDTO
                    {
                        CurrencyCode = result.CurrencyCode,
                        ExchangeRate = result.ExchangeRate
                    }
                };
            }
        }

        public async Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO request)
        {
            return await _bettingRepository.ProcessBetConfig(request);
        }

        public async Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request)
        {
            return await _bettingRepository.CancelBets(request);
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
        private bool IsValidIPAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out IPAddress ipAddresss);
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
