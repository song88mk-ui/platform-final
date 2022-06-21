using Microsoft.EntityFrameworkCore;
using Platform.API.Models;
using Platform.Shared.Data;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
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
        private readonly IMemCache _memCache;
        public PlatformRepository(IOperatorRepository operatorRepository, IBettingRepository bettingRepository, ISportsbookRepository sportsbookRepository, PlatformContext platformContext, IMemCache memCache)
        {
            _operatorRepository = operatorRepository;
            _bettingRepository = bettingRepository;
            _sportsbookRepository = sportsbookRepository;
            _platformContext = platformContext;
            _memCache = memCache;
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
            if (request.Selections.Count >= 2 && request.Selections.Any(c => c.SportId > 1000) && request.Selections.Any(c => c.SportId < 1000))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Virtuals_Cannot_Be_Added_As_Part_Of_Parlay_With_Other_Sports,
                    Message = StatusCode.Virtuals_Cannot_Be_Added_As_Part_Of_Parlay_With_Other_Sports.ToString().Replace('_', ' '),
                };
            }

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

            if (request.IsBoosted && request.Selections.Count >= 2)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Boosted_Odds_Cannot_Be_Parlayed,
                    Message = StatusCode.Boosted_Odds_Cannot_Be_Parlayed.ToString().Replace('_', ' '),
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
                ClientCode = clientResponse.Result.ClientCode,
                OddsChangeType = request.IsBoosted == true ? OddsChangeType.None : request.OddsChangeType,
                IsBoosted = request.IsBoosted,
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

            if (request.IsBoosted && result.Result.Selections.Any(s => s.FixtureStatus >= FixtureStatus.Live))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Boosted_Odds_Not_Allowed_Once_Fixture_Transitions_To_Live,
                    Message = StatusCode.Boosted_Odds_Not_Allowed_Once_Fixture_Transitions_To_Live.ToString().Replace('_', ' '),
                };
            }

            var selections = new List<BetSelectionRequestDTO>();

            foreach (var selection in request.Selections)
            {
                var initialSelection = result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(selection.SelectionId)).FirstOrDefault();

                selections.Add(new BetSelectionRequestDTO
                {
                    SportId = initialSelection.SportId,
                    SportName = initialSelection.SportName,
                    SegmentId = initialSelection.SegmentId,
                    SegmentName = initialSelection.SegmentName,
                    LeagueId = initialSelection.LeagueId,
                    LeagueName = initialSelection.LeagueName,
                    FixtureId = initialSelection.FixtureId,
                    FixtureName = initialSelection.FixtureName,
                    FixtureType = initialSelection.FixtureType,
                    MarketTemplateId = initialSelection.MarketTemplateId,
                    MarketId = initialSelection.MarketId,
                    MarketName = initialSelection.MarketName,
                    Line = initialSelection.Line,
                    SelectionTemplateId = initialSelection.SelectionTemplateId,
                    SelectionId = initialSelection.SelectionId,
                    SelectionName = initialSelection.SelectionName,
                    Odds = initialSelection.Odds,
                    OddsRef = initialSelection.OddsRef,
                    FixtureStatus = initialSelection.FixtureStatus,
                    FixtureStartDate = initialSelection.FixtureStartDate,
                    BetScore = initialSelection.Score,
                    FixtureSpecifier = initialSelection.FixtureSpecifier,
                    SelectionSpecifier = initialSelection.SelectionSpecifier,
                    CorrelationId = initialSelection.CorrelationId,
                    Probability = initialSelection.Probability,
                    WonProbability = initialSelection.WonProbability,
                    LostProbability = initialSelection.LostProbability,
                    VoidProbability = initialSelection.VoidProbability,
                    HalfWonProbability = initialSelection.HalfWonProbability,
                    HalfLostProbability = initialSelection.HalfLostProbability,
                    IsBinary = initialSelection.SelectionCount > 2 ? false : true
                });
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
                IsBoosted = request.IsBoosted,
                Selections = selections
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
                ClientCode = clientResponse.Result.ClientCode,
                OddsChangeType = request.IsBoosted == true ? OddsChangeType.None : request.OddsChangeType,
                IsBoosted = request.IsBoosted,
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
                IsBoosted = request.IsBoosted,
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
                    Odds = request.IsBoosted == true ? s.Odds : result.Result.Selections.Where(c => c.SelectionId == Convert.ToInt64(s.SelectionId)).Select(ss => ss.Odds).FirstOrDefault(),
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
            if (request.IsCurrent == false)
            {
                if (request.FromDate > request.ToDate)
                {
                    return new BaseResponseDTO<BetHistoryResponseDTO>
                    {
                        StatusCode = StatusCode.FromDate_Must_Be_Earlier_Than_ToDate,
                        Message = StatusCode.FromDate_Must_Be_Earlier_Than_ToDate.ToString().Replace('_', ' ')
                    };
                }
            }

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
                            ParlayBonus = s.ParlayBonus,
                            ParlayBonusWinnings = s.ParlayBonusWinnings,
                            IsBoosted = s.IsBoosted,
                            IsFreeBet = s.IsFreeBet,
                            StakeBonus = s.StakeBonus,
                            StakeBonusPayout = s.StakeBonus,
                            StakeReal = s.StakeReal,
                            StakeRealPayout = s.StakeRealPayout,
                            TaxedStake = s.TaxedStake,
                            TaxedWinnings = s.TaxedWinnings,
                            Selections = GetTranslatedBetSelectionHistory(s.Selections, translations.Result.Selections)
                        }).ToList()
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(string clientId, string langCode, string operatorToken)
        {
            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
            {
                return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var authenticationResponse = await _operatorRepository.Authenticate(operatorToken, clientResponse.Result.Endpoint);

            if (authenticationResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
                {
                    StatusCode = authenticationResponse.StatusCode,
                    Message = authenticationResponse.Message,
                };
            }

            var payload = new BetHistoryRunningRequestDTO
            {
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                UserId = authenticationResponse.Result.UserId
            };

            return await _bettingRepository.GetBetHistoryRunning(payload);
        }
        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistorySettled(string apiKey, string clientId, string langCode, BetHistorySettledRequest request)
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

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var payload = new BetHistorySettledRequestDTO
            {
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                UserId = request.UserId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return await _bettingRepository.GetBetHistorySettled(payload);
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
                var key = $"cc:{guid.ToString().ToUpper()}";

                var cache = _memCache.GetCache<BaseResponseDTO<ClientResponse>>(key);

                if (cache != null)
                    return cache;

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
                    var finalResult = new BaseResponseDTO<ClientResponse>
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

                    _memCache.AddOrUpdateCache(key, finalResult, 24);

                    return finalResult;
                }
            }
            else
            {
                //TODO: temp approach while using the old backoffice sportsbook client with ClientId as integer type
                var isInteger = int.TryParse(clientId, out int intClientId);

                if (isInteger)
                {
                    var key = $"cc:{intClientId.ToString().ToUpper()}";

                    var cache = _memCache.GetCache<BaseResponseDTO<ClientResponse>>(key);

                    if (cache != null)
                        return cache;

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
                        var finalResult = new BaseResponseDTO<ClientResponse>
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

                        _memCache.AddOrUpdateCache(key, finalResult, 24);

                        return finalResult;
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

        public async Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(string apiKey, string clientId, string operatorToken, string IPCountryCode, LivestreamRequest request)
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
                IPCountryCode = IPCountryCode,
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode
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
                SegmentId = request.SegmentId,
                ClientCode = clientResponse.Result.ClientCode
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
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode
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
        public async Task<BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>> GetSelectorMarketTemplates(string apiKey, string clientId)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<List<SportSelectorMarketTemplateDTO>>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetSelectorMarketTemplates();
        }
        public async Task<BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>> GetMarketSortOrder(string clientId)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<ClientMarketTemplateGroupResponseDTO2>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetMarketSortOrder(new MarketSortOrderRequestDTO { ClientId = clientResponse.Result.ClientId });
        }
        public async Task<BaseResponseDTO<ClientFixtureSortOrderResponseDTO>> GetFixtureSortOrder(string clientId)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<ClientFixtureSortOrderResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetFixtureSortOrder(new FixtureSortOrderRequestDTO { ClientId = clientResponse.Result.ClientId });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures(string apiKey, string clientId, string langCode, string ipCountryCode)
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
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount(string apiKey, string clientId, string langCode, SportGroup sportGroupId)
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
                ClientCode = clientResponse.Result.ClientCode,
                SportGroupId = (int)sportGroupId
            });
        }
        public async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetTodayFixtureCount(string apiKey, string clientId, string langCode, TodayFixtureCountRequest request, SportGroup sportGroupId)
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
                ClientCode = clientResponse.Result.ClientCode,
                SportGroupId = (int)sportGroupId
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(string apiKey, string clientId, string langCode, FixtureRequest request, string ipCountryCode)
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixturesByLeague(string apiKey, string clientId, string langCode, FixtureRequest request, string ipCountryCode)
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
                LeagueId = request.LeagueId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixtures(string apiKey, string clientId, string langCode, TodayFixtureRequest request, string ipCountryCode)
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTodayFixturesByLeague(string apiKey, string clientId, string langCode, TodayFixtureRequest request, string ipCountryCode)
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
                LeagueId = request.LeagueId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUpcomingFixtures(string apiKey, string clientId, string langCode, UpcomingFixtureRequest request, string ipCountryCode)
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixtures(string apiKey, string clientId, string langCode, DailyFixtureRequest request, string ipCountryCode)
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetDailyFixturesByLeague(string apiKey, string clientId, string langCode, DailyFixtureRequest request, string ipCountryCode)
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
                LeagueId = request.LeagueId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopLeagueFixtures(string apiKey, string clientId, string langCode, TopLeagueFixtureRequest request, string ipCountryCode)
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetSegmentFixtures(string apiKey, string clientId, string langCode, SegmentFixtureRequest request, string ipCountryCode)
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
                SegmentId = request.SegmentId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtures(string apiKey, string clientId, string langCode, LeagueFixtureRequest request, string ipCountryCode)
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
                LeagueId = request.LeagueId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<SportsResponseDTO>> GetSports(string apiKey, string clientId, string langCode, SportGroup sportGroup)
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
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode,
                SportGroup = sportGroup
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetFixtureDetails(string apiKey, string clientId, string langCode, FixtureDetailsRequest request, string ipCountryCode)
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
                FixtureId = request.FixtureId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetSelectionDetails(string apiKey, string clientId, string langCode, FixtureMarketSelectionDetailsRequest request, string ipCountryCode)
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
                SelectionId = request.SelectionId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixtures(string apiKey, string clientId, string langCode, OutrightFixtureRequest request, string ipCountryCode)
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
                SportId = request.SportId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetOutrightFixturesByLeague(string apiKey, string clientId, string langCode, OutrightFixtureRequest request, string ipCountryCode)
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
                LeagueId = request.LeagueId,
                ClientCode = clientResponse.Result.ClientCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWResponseDTO>> GetUserFavorites2(string apiKey, string clientId, string langCode, string operatorToken, string ipCountryCode)
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
                LanguageCode = languageCode,
                IpCountryCode = ipCountryCode,
            });
        }
        public async Task<BaseResponseDTO<TWSearchResponseDTO>> GetSearchResults(string apiKey, string clientId, string langCode, SearchResultRequest request, SportGroup sportGroupId)
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
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode,
                SportGroupId = (int)sportGroupId
            });
        }
        public async Task<BaseResponseDTO<UserFavoriteResponseDTO>> GetUserFavorites(string apiKey, string clientId, string langCode, string operatorToken, string ipCountryCode)
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
                LanguageCode = languageCode,
                IpCountryCode = ipCountryCode,
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
                        currentCompanyShare = currentClient.EffectiveDate < DateTime.UtcNow && currentClient.CurrentCompanyShare != request.CurrentCompanyShare ?
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
                catch (Exception ex)
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
            var clientCodes = clients.Select(s => s.ClientCode).ToList();

            var sportsbookRequest = new ClientTopFixtureRequestDTO()
            {
                ClientIds = clientIds,
                LanguageCode = LanguageCode.EN,
                ClientCodes = clientCodes
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

        public async Task<BaseResponseDTO<ClientTopFixtureResponseDTO>> GetClientTopFixtures2(string clientCodes)
        {
            var strClientCodes = clientCodes.Split(',').ToList();

            var clients = _platformContext.Client.Where(c => strClientCodes.Contains(c.ClientCode)).ToList();

            var sportsbookRequest = new ClientTopFixtureRequestDTO2()
            {
                ClientInfos = clients.Select(s => new ClientInfo
                {
                    ClientId = s.ClientId,
                    ClientCode = s.ClientCode
                }).ToList(),
                LanguageCode = LanguageCode.EN,
            };

            var response = await _sportsbookRepository.GetClientTopFixtures2(sportsbookRequest);

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
                LanguageCode = LanguageCode.EN,
                ClientCode = client.ClientCode
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

        public async Task<BaseResponseDTO<ClientTopFixtureMessageResponseDTO>> CopyClientTopFixtures(CopyClientTopFixtureRequest request)
        {
            // fetch clientids
            var clientIds = await _platformContext.Client.Where(c => request.ClientIds.Contains(c.ClientIdRef)).Select(s => s.ClientId).ToListAsync();

            if (!clientIds.Any())
            {
                return new BaseResponseDTO<ClientTopFixtureMessageResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ClientTopFixtureMessageResponseDTO()
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                    }
                };
            }

            var sportsbookRequest = new CopyClientTopFixtureRequestDTO
            {
                ClientIds = clientIds,
                TopFixtures = request.TopFixtures.Select(s => new Shared.Models.ClientTopFixtureSetup
                {
                    FixtureId = s.FixtureId,
                    SortOrder = s.SortOrder
                }).ToList()
            };

            return await _sportsbookRepository.CopyClientTopFixtures(sportsbookRequest);
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
                SegmentId = request.SegmentId,
                ClientCodes = clientResult.Select(s => s.ClientCode).ToList()
            };

            var clientTopLeaguesResponse = await _sportsbookRepository.GetClientTopLeagues(sportsbookRequest);
            if (clientTopLeaguesResponse.StatusCode == StatusCode.Success)
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

            var sportsbookRequest = new ClientTopLeagueDTO()
            {
                ClientId = result.ClientId,
                SportId = request.SportId,
                TopLeagues = request.TopLeagues
            };

            return await _sportsbookRepository.AddOrUpdateTopLeagues(sportsbookRequest);
        }

        public async Task<BaseResponseDTO<ClientTopLeaguesMessageResponseDTO>> CopyClientTopLeagues(BackOfficeCopyClientTopLeague request)
        {
            // fetch clientids
            var clientIds = _platformContext.Client.Where(c => request.ClientIdRefs.Contains(c.ClientIdRef)).Select(s => s.ClientId).ToList();

            if (!clientIds.Any())
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

            var sportsbookRequest = new CopyClientTopLeagueRequestDTO()
            {
                ClientIds = clientIds,
                SportId = request.SportId,
                TopLeagues = request.TopLeagues
            };

            return await _sportsbookRepository.CopyClientTopLeagues(sportsbookRequest);
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

        public async Task<BaseResponseDTO<ManualSettlementResponseDTO>> ManualSettlement(ManualBetSettlementRequest request)
        {
            return await _bettingRepository.ManualSettlement(request);
        }

        public async Task<BaseResponseDTO<ManualBetDetailsResponseDTO>> GetManualBetDetails(ManualBetDetailsRequest request)
        {
            return await _bettingRepository.GetManualBetDetails(request);
        }

        public async Task<BaseResponseDTO<ResendSettlementResponseDTO>> ResendSettlement(ResendSettlementRequestDTO request)
        {
            var betIds = request.Bets.Select(s => Guid.Parse(s.BetId)).ToList();

            var cancelledBets = request.Bets.Where(b => b.CurrentStatus == BetStatus.Cancelled).ToList();

            var bets = await _platformContext.OperatorSettlementRetry.AsNoTracking()
                .Where(c => betIds.Contains(c.BetId))
                .ToListAsync();

            if (!cancelledBets.Any() && !bets.Any())
            {
                return new BaseResponseDTO<ResendSettlementResponseDTO>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace('_', ' '),
                    Result = new ResendSettlementResponseDTO()
                };
            }

            if (!cancelledBets.Any() && bets.Any(c => c.IsSuccess || c.RetryCount < 8))
            {
                return new BaseResponseDTO<ResendSettlementResponseDTO>()
                {
                    StatusCode = StatusCode.Resend_Attempt_Not_Successful_Please_Try_Again,
                    Message = StatusCode.Resend_Attempt_Not_Successful_Please_Try_Again.ToString().Replace('_', ' '),
                    Result = new ResendSettlementResponseDTO()
                    //Result = new ResendSettlementResponseDTO
                    //{
                    //    Data = bets.Select(s => new ResendSettlementResponse
                    //    {
                    //        BetId = s.BetId,
                    //        StatusCode = s.RetryCount >= 8 && s.IsSuccess == false ? StatusCode.Error : StatusCode.Success,
                    //        Message = s.RetryCount >= 8 && s.IsSuccess == false ? StatusCode.Error.ToString().Replace('_', ' ') : StatusCode.Success.ToString().Replace('_', ' ')
                    //    }).ToList()
                    //}
                };
            }

            if (cancelledBets.Any())
            {
                var isSuccess = false;
                var response = new ResendSettlementResponseDTO();
                foreach (var betId in betIds)
                {
                    var cancelledBetDetail = (await _platformContext.OperatorLog.AsNoTracking()
                    .Where(c => betId.ToString() == c.BetId && c.Direction == "REQUEST").OrderByDescending(o => o.Timestamp).ToListAsync()).FirstOrDefault();

                    if (cancelledBetDetail != null && cancelledBetDetail.URL.Contains("bet/cancellation"))
                    {

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "REQUEST",
                            ClientId = cancelledBetDetail.ClientId,
                            ClientCode = cancelledBetDetail.ClientCode,
                            URL = cancelledBetDetail.URL,
                            BetId = cancelledBetDetail.BetId.ToUpper(),
                            Info = cancelledBetDetail.Info,
                            OperatorToken = cancelledBetDetail.OperatorToken,
                            CorrelationId = cancelledBetDetail.CorrelationId,
                            Timestamp = DateTime.UtcNow
                        });

                        var operatorResponse = await _operatorRepository.ResendCancellation(cancelledBetDetail.URL, cancelledBetDetail.Info);
                        if (operatorResponse.StatusCode == StatusCode.Success)
                        {
                            isSuccess = true;
                            response.Data.Add(new ResendSettlementResponse()
                            {
                                StatusCode = StatusCode.Success,
                                BetId = betId,
                                Message = $"{operatorResponse.Message}"
                            });
                        }
                        else
                        {
                            response.Data.Add(new ResendSettlementResponse()
                            {
                                StatusCode = StatusCode.Error,
                                BetId = betId,
                                Message = $"{operatorResponse.Message}"
                            });
                        }
                        //add to operator log
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = cancelledBetDetail.ClientId,
                            ClientCode = cancelledBetDetail.ClientCode,
                            URL = cancelledBetDetail.URL,
                            BetId = cancelledBetDetail.BetId.ToUpper(),
                            Info = JsonSerializer.Serialize(operatorResponse.Result),
                            OperatorToken = cancelledBetDetail.OperatorToken,
                            CorrelationId = cancelledBetDetail.CorrelationId,
                            Timestamp = DateTime.UtcNow
                        });


                    }
                    else
                    {
                        response.Data.Add(new ResendSettlementResponse()
                        {
                            StatusCode = StatusCode.Error,
                            BetId = betId,
                            Message = $"No cancellation found"
                        });
                    }
                }

                if (isSuccess)
                {
                    return new BaseResponseDTO<ResendSettlementResponseDTO>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString().Replace('_', ' '),
                        Result = response
                    };
                }
                else
                {
                    return new BaseResponseDTO<ResendSettlementResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}",
                        Result = response
                    };
                }

            }
            else
            {
                var messageRequest = new ResendSettlementMessageRequestDTO()
                {
                    Messages = bets.Select(s => new ResendSettlementMessageDTO
                    {
                        BetId = s.BetId,
                        MessageType = s.MessageType,
                        Info = s.Info,
                        CorrelationId = s.CorrelationId
                    }).ToList()
                };

                return await _bettingRepository.ResendSettlement(messageRequest);
            }

        }

        public async Task<BaseResponseDTO<ResendSettlementBetDetailResponseDTO>> GetResendSettlementBetDetails(ResendSettlementBetDetailRequestDTO request)
        {
            if (Guid.TryParse(request.BetId, out var betId))
            {
                var response = new ResendSettlementBetDetailResponseDTO();

                var bet = await _platformContext.OperatorSettlementRetry.AsNoTracking().FirstOrDefaultAsync(c => c.BetId == betId);

                if (bet != null)
                {
                    var onBetSettlement = JsonSerializer.Deserialize<OnBetSettlement>(bet.Info);

                    response.Bet = new ResendSettlementBetDetailDTO()
                    {
                        BetId = onBetSettlement.Bet.BetId,
                        UserId = onBetSettlement.Bet.UserId,
                        Stake = onBetSettlement.Bet.Stake,
                        TrueStake = onBetSettlement.Bet.TrueStake,
                        Odds = onBetSettlement.Bet.Odds,
                        OddsTypeId = onBetSettlement.Bet.OddsTypeId,
                        OddsTypeName = onBetSettlement.Bet.OddsTypeName,
                        Payout = onBetSettlement.Bet.Payout,
                        WinLoss = onBetSettlement.Bet.WinLoss,
                        BetStatusId = onBetSettlement.Bet.BetStatusId,
                        BetStatusName = onBetSettlement.Bet.BetStatusName,
                        Reason = onBetSettlement.Bet.Reason,
                        CurrencyCode = onBetSettlement.Bet.CurrencyCode,
                        LanguageCode = onBetSettlement.Bet.LanguageCode,
                        ExchangeRate = onBetSettlement.Bet.ExchangeRate,
                        IPAddress = onBetSettlement.Bet.IPAddress,
                        Channel = onBetSettlement.Bet.Channel,
                        ClientId = onBetSettlement.Bet.ClientId,
                        ClientIdRef = onBetSettlement.Bet.ClientIdRef,
                        ClientCode = onBetSettlement.Bet.ClientCode,
                        ClientName = onBetSettlement.Bet.ClientName,
                        LimitId = onBetSettlement.Bet.LimitId,
                        Endpoint = onBetSettlement.Bet.Endpoint,
                        OperatorToken = onBetSettlement.Bet.OperatorToken,
                        CompanyShare = onBetSettlement.Bet.CompanyShare,
                        IsResettled = onBetSettlement.Bet.IsResettled,
                        IsCashout = onBetSettlement.Bet.IsCashout,
                        BetDate = onBetSettlement.Bet.BetDate,
                        BetDateOffset = onBetSettlement.Bet.BetDateOffset,
                        SettlementDate = onBetSettlement.Bet.SettlementDate,
                        SettlementDateOffset = onBetSettlement.Bet.SettlementDateOffset,
                        StakeTax = onBetSettlement.Bet.StakeTax,
                        WinningsTax = onBetSettlement.Bet.WinningsTax,
                        TaxedStake = onBetSettlement.Bet.TaxedStake,
                        TaxedWinnings = onBetSettlement.Bet.TaxedWinnings,
                        ParlayBonus = onBetSettlement.Bet.ParlayBonus,
                        ParlayBonusWinnings = onBetSettlement.Bet.ParlayBonusWinnings,
                        StakeReal = onBetSettlement.Bet.StakeReal,
                        StakeBonus = onBetSettlement.Bet.StakeBonus,
                        StakeRealPayout = onBetSettlement.Bet.StakeRealPayout,
                        StakeBonusPayout = onBetSettlement.Bet.StakeBonusPayout,
                        IsFreeBet = onBetSettlement.Bet.IsFreeBet,
                        RetryCount = bet.RetryCount,
                        IsSuccess = bet.IsSuccess,
                        BetSelections = onBetSettlement.BetSelections.Select(s => new ResendSettlementBetSelectionDetailDTO()
                        {
                            BetSelectionId = s.BetSelectionId,
                            BetId = s.BetId,
                            UserId = s.UserId,
                            SportId = s.SportId,
                            SportName = s.SportName,
                            SegmentId = s.SegmentId,
                            SegmentName = s.SegmentName,
                            LeagueId = s.LeagueId,
                            LeagueName = s.LeagueName,
                            FixtureId = s.FixtureId,
                            FixtureName = s.FixtureName,
                            FixtureTypeId = s.FixtureTypeId,
                            FixtureTypeName = s.FixtureTypeName,
                            MarketTemplateId = s.MarketTemplateId,
                            MarketId = s.MarketId,
                            MarketName = s.MarketName,
                            SelectionTemplateId = s.SelectionTemplateId,
                            SelectionId = s.SelectionId,
                            SelectionName = s.SelectionName,
                            FixtureStatusId = s.FixtureStatusId,
                            FixtureStatusName = s.FixtureStatusName,
                            Line = s.Line,
                            Odds = s.Odds,
                            BetSelectionStatusId = s.BetSelectionStatusId,
                            BetSelectionStatusName = s.BetSelectionStatusName,
                            IsSettled = s.IsSettled,
                            IsResettled = s.IsResettled,
                            FixtureSpecifier = s.FixtureSpecifier,
                            SelectionSpecifier = s.SelectionSpecifier,
                            FixtureStartDate = s.FixtureStartDate,
                            BetScore = s.BetScore,
                            SettlementScore = s.SettlementScore,
                            Probability = s.Probability,
                            WonProbability = s.WonProbability,
                            LostProbability = s.LostProbability,
                            VoidProbability = s.VoidProbability,
                            HalfWonProbability = s.HalfWonProbability,
                            HalfLostProbability = s.HalfLostProbability,
                            CorrelationId = s.CorrelationId,
                            DeadHeatFactor = s.DeadHeatFactor
                        }).ToList()
                    };
                }
                else
                {
                    // validate if bet is rejected or cancelled
                    var manualbetDetails = await _bettingRepository.GetManualBetDetails(new ManualBetDetailsRequest
                    {
                        BetIds = new List<string>() { request.BetId }
                    });

                    if (manualbetDetails.Result.Bets.Any())
                    {
                        var betDetail = manualbetDetails.Result.Bets.Select(s => new ResendSettlementBetDetailDTO
                        {
                            BetId = s.BetId,
                            UserId = s.UserId,
                            Stake = s.Stake,
                            TrueStake = s.TrueStake,
                            Odds = s.Odds,
                            OddsTypeId = s.OddsTypeId,
                            OddsTypeName = s.OddsTypeName,
                            Payout = s.Payout,
                            WinLoss = s.WinLoss,
                            BetStatusId = s.BetStatusId,
                            BetStatusName = s.BetStatusName,
                            Reason = s.Reason,
                            CurrencyCode = s.CurrencyCode,
                            LanguageCode = s.LanguageCode,
                            ExchangeRate = s.ExchangeRate,
                            IPAddress = s.IPAddress,
                            Channel = s.Channel,
                            ClientId = s.ClientId,
                            ClientIdRef = s.ClientIdRef,
                            ClientCode = s.ClientCode,
                            ClientName = s.ClientName,
                            LimitId = s.LimitId,
                            Endpoint = s.Endpoint,
                            OperatorToken = s.OperatorToken,
                            CompanyShare = s.CompanyShare,
                            IsResettled = s.IsResettled,
                            IsCashout = s.IsCashout,
                            BetDate = s.BetDate,
                            BetDateOffset = s.BetDateOffset,
                            SettlementDate = s.SettlementDate,
                            SettlementDateOffset = s.SettlementDateOffset,
                            StakeTax = s.StakeTax,
                            WinningsTax = s.WinningsTax,
                            TaxedStake = s.TaxedStake,
                            TaxedWinnings = s.TaxedWinnings,
                            ParlayBonus = s.ParlayBonus,
                            ParlayBonusWinnings = s.ParlayBonusWinnings,
                            StakeReal = s.StakeReal,
                            StakeBonus = s.StakeBonus,
                            StakeRealPayout = s.StakeRealPayout,
                            StakeBonusPayout = s.StakeBonusPayout,
                            IsFreeBet = s.IsFreeBet,
                            BetSelections = s.Selections.Select(ss => new ResendSettlementBetSelectionDetailDTO()
                            {
                                BetSelectionId = ss.BetSelectionId,
                                BetId = ss.BetId,
                                UserId = ss.UserId,
                                SportId = ss.SportId,
                                SportName = ss.SportName,
                                SegmentId = ss.SegmentId,
                                SegmentName = ss.SegmentName,
                                LeagueId = ss.LeagueId,
                                LeagueName = ss.LeagueName,
                                FixtureId = long.Parse(ss.FixtureId),
                                FixtureName = ss.FixtureName,
                                FixtureTypeId = ss.FixtureTypeId,
                                FixtureTypeName = ss.FixtureTypeName,
                                MarketTemplateId = ss.MarketTemplateId,
                                MarketId = long.Parse(ss.MarketId),
                                MarketName = ss.MarketName,
                                SelectionTemplateId = ss.SelectionTemplateId,
                                SelectionId = long.Parse(ss.SelectionId),
                                SelectionName = ss.SelectionName,
                                FixtureStatusId = ss.FixtureStatusId,
                                FixtureStatusName = ss.FixtureStatusName,
                                Line = ss.Line,
                                Odds = ss.Odds,
                                BetSelectionStatusId = ss.BetSelectionStatusId,
                                BetSelectionStatusName = ss.BetSelectionStatusName,
                                IsSettled = ss.IsSettled,
                                IsResettled = ss.IsResettled,
                                FixtureSpecifier = ss.FixtureSpecifier,
                                SelectionSpecifier = ss.SelectionSpecifier,
                                FixtureStartDate = ss.FixtureStartDate,
                                BetScore = ss.BetScore,
                                SettlementScore = ss.SettlementScore,
                                Probability = ss.Probability,
                                WonProbability = ss.WonProbability,
                                LostProbability = ss.LostProbability,
                                VoidProbability = ss.VoidProbability,
                                HalfWonProbability = ss.HalfWonProbability,
                                HalfLostProbability = ss.HalfLostProbability,
                                CorrelationId = ss.CorrelationId,
                                DeadHeatFactor = ss.DeadHeatFactor
                            }).ToList()
                        }).FirstOrDefault();

                        if (betDetail != null && (betDetail.BetStatusId == (int)BetStatus.Rejected || betDetail.BetStatusId == (int)BetStatus.Cancelled))
                            response.Bet = betDetail;
                    }
                }

                return new BaseResponseDTO<ResendSettlementBetDetailResponseDTO>()
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace('_', ' '),
                    Result = response
                };
            }
            else
            {
                return new BaseResponseDTO<ResendSettlementBetDetailResponseDTO>()
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace('_', ' '),
                    Result = new ResendSettlementBetDetailResponseDTO()
                };
            }
        }

        public async Task<BaseResponseDTO<ParlayBonusResponseDTO>> GetClientParlayBonus(GetParlayBonusRequestDTO request)
        {
            var clientResult = await _platformContext.Client.Where(c => c.ClientIdRef == request.ClientIdRef).FirstOrDefaultAsync();
            if (clientResult == null)
            {
                return new BaseResponseDTO<ParlayBonusResponseDTO>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            request.ClientId = clientResult.ClientId;

            var response = await _bettingRepository.GetParlayBonus(request);
            if (response.StatusCode == StatusCode.Success)
            {
                var backOfficeResponse = new ParlayBonusResponseDTO()
                {
                    ParlayBonusConfig = response.Result.ParlayBonusConfig,
                    ParlayBonusMinOdds = response.Result.ParlayBonusMinOdds
                };

                return new BaseResponseDTO<ParlayBonusResponseDTO>
                {
                    StatusCode = response.StatusCode,
                    Message = response.Message,
                    Result = backOfficeResponse
                };
            }
            else
            {
                return new BaseResponseDTO<ParlayBonusResponseDTO>
                {
                    StatusCode = response.StatusCode,
                    Message = response.Message
                };

            }
        }

        public async Task<BaseResponseDTO<StatusCode>> AddOrUpdateParlayBonus(ParlayBonusRequestDTO request)
        {
            var result = await _platformContext.Client.Where(c => c.ClientIdRef == request.ClientIdRef).FirstOrDefaultAsync();
            if (result == null)
            {
                return new BaseResponseDTO<StatusCode>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString()
                };
            }
            request.ClientId = result.ClientId;
            return await _bettingRepository.ProcessParlayBonus(request);
        }

        public async Task<BaseResponseDTO<SportGroupResponseDTO>> GetSportGroups()
        {
            return await _sportsbookRepository.GetSportGroups();
        }

        public async Task<BaseResponseDTO<BoostedResponseDTO>> GetBoostedFixtures(string apiKey, string clientId, string langCode, string ipCountryCode)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<BoostedResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<BoostedResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BoostedResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetBoostedFixtures(new BoostedFixturesRequestDTO 
            {
                ClientId = clientResponse.Result.ClientId,
                ClientCode = clientResponse.Result.ClientCode,
                LanguageCode = languageCode,
                IPCountryCode = ipCountryCode
            });
        }

        public async Task<BaseResponseDTO<BetBuilderFixtureResponseDTO>> GetBetBuilderFixtures(string clientId, string langCode)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<BetBuilderFixtureResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<BetBuilderFixtureResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetBuilderFixtureResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetBetBuilderFixtures(new BetBuilderFixtureRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                ClientCode = clientResponse.Result.ClientCode,
                LanguageCode = languageCode
            });
        }

        public async Task<BaseResponseDTO<BetBuilderSelectionResponseDTO>> GetBetBuilderSelections(string clientId, string langCode, string fixtureId)
        {
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), langCode) ? true : false;

            if (!isValidLanguageCode)
            {
                return new BaseResponseDTO<BetBuilderSelectionResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var languageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), langCode);

            if (!IsValidLanguageCode(languageCode))
            {
                return new BaseResponseDTO<BetBuilderSelectionResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                };
            }

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetBuilderSelectionResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            return await _sportsbookRepository.GetBetBuilderSelections(new BetBuilderSelectionRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                ClientCode = clientResponse.Result.ClientCode,
                LanguageCode = languageCode,
                FixtureId = fixtureId
            });
        }

        public async Task<BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>> GetBetBuilderSelectionsOdds(string clientId, BetBuilderSelectionsOddsRequestDTO request)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<BetBuilderSelectionsOddsResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message,
                    Result = new BetBuilderSelectionsOddsResponseDTO()
                };
            }

            return await _sportsbookRepository.GetBetBuilderSelectionsOdds(request);
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
        private void AddOperatorLog(OperatorLog operatorLog)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    _platformContext.OperatorLog.Add(operatorLog);

                    _platformContext.SaveChanges();
                    scope.Complete();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
