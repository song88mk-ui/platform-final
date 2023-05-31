using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.OpenApi.Writers;
using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;
using Platform.Shared.Data.CasinoBetting;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Platform.QTech.API.Repository
{
    public class CasinoRepository : ICasinoRepository
    {
        private readonly IProviderRepository _providerRepository;
        private readonly IOperatorRepository _operatorRepository;
        private readonly IMemCache _memCache;
        private readonly PlatformContext _platformContext;
        private readonly CasinoBettingContext _casinoBettingContext;

        public CasinoRepository(IProviderRepository providerRepository, IMemCache memCache, PlatformContext platformContext, CasinoBettingContext casinoBettingContext, IOperatorRepository operatorRepository)
        {
            _providerRepository = providerRepository;
            _memCache = memCache;
            _platformContext = platformContext;
            _casinoBettingContext = casinoBettingContext;
            _operatorRepository = operatorRepository;
        }
        public async Task<Tuple<SessionCode, GameSessionResponse>> GetSession(GameSessionRequest request)
        {
            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<SessionCode, GameSessionResponse>(SessionCode.ACCOUNT_BLOCKED, new GameSessionResponse());

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != StatusCode.Success)
                return new Tuple<SessionCode, GameSessionResponse>(SessionCode.INVALID_TOKEN, new GameSessionResponse());

            return new Tuple<SessionCode, GameSessionResponse>(SessionCode.OK, new GameSessionResponse 
            {
                Balance = authenticationResponse.Result.Balance,
                Currency = authenticationResponse.Result.CurrencyCode
            });
        }
        public async Task<Tuple<BalanceCode, BalanceResponse>> GetBalance(BalanceRequest request)
        {
            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.REQUEST_DECLINED, new BalanceResponse());

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != StatusCode.Success)
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.LOGIN_FAILED, new BalanceResponse());

            return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.OK, new BalanceResponse
            {
                Balance = authenticationResponse.Result.Balance,
                Currency = authenticationResponse.Result.CurrencyCode
            });
        }
        public async Task<Tuple<WithdrawCode, WithdrawResponse>> Withdraw(WithdrawRequest request)
        {
            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.REQUEST_DECLINED, new WithdrawResponse());

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != StatusCode.Success)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.LOGIN_FAILED, new WithdrawResponse());

            if (authenticationResponse.Result.Balance < request.Amount)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INSUFFICIENT_FUNDS, new WithdrawResponse());

            var round = _casinoBettingContext.Transaction.Where(c => c.RoundId == request.RoundId).FirstOrDefault();

            var dateNow = DateTime.UtcNow;

            var isComplete = request.Completed.ToLower() == "true" ? true : false;

            if (round == null)
            {
                //call platform.operator.api for debit

                var rate = _platformContext.ForexEUR.Find(request.Currency);

                using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                var transactionEntity = _casinoBettingContext.Transaction.Update(new Shared.Data.CasinoBetting.Entities.CasinoBet
                {
                    ProviderTransactionId = request.TxnId,
                    TransactionType = request.TxnType,
                    ClientId = clientId.ToUpper(),
                    UserId = authenticationResponse.Result.UserId,
                    RoundId = request.RoundId,
                    Stake = request.Amount,
                    StakeBonus = request.BonusBetAmount,
                    WinLoss = isComplete ? request.Amount * -1 : 0,
                    Payout = 0,
                    BetStatusId = isComplete ? (int)BetStatus.Lost : (int)BetStatus.Running,
                    BetStatusName = isComplete ? BetStatus.Lost.ToString() : BetStatus.Running.ToString(),
                    CurrencyCode = request.Currency.ToUpper(),
                    GameId = request.GameId,
                    Device = request.Device,
                    ClientType = request.ClientType,
                    ProviderBetId = request.ClientRoundId,
                    Category = request.Category,
                    IsComplete = isComplete,
                    ExchangeRate = rate.ExchangeRate,
                    JackpotContributions = JsonSerializer.Serialize(request.JpContributions),
                    ConversionRate = JsonSerializer.Serialize(request.ConversionRate),
                    BonusType = request.BonusType,
                    BonusPromoCode = request.BonusPromoCode,
                    JackpotPayout = 0,
                    TableId = request.TableId,
                    TransactionIds = JsonSerializer.Serialize(new List<string> { request.TxnId }),
                    ProviderTransactionDate = DateTime.Parse(request.Created.Split('[').First()).ToUniversalTime(),
                    BetDate = dateNow,
                    SettlementDate = dateNow
                }).Entity;

                _casinoBettingContext.TransactionAudit.Add(new Shared.Data.CasinoBetting.Entities.CasinoBetAudit
                {
                    BetId = transactionEntity.BetId,
                    ProviderTransactionId = transactionEntity.ProviderTransactionId,
                    TransactionType = transactionEntity.TransactionType,
                    ClientId = transactionEntity.ClientId,
                    UserId = transactionEntity.UserId,
                    RoundId = transactionEntity.RoundId,
                    Stake = transactionEntity.Stake,
                    StakeBonus = transactionEntity.StakeBonus,
                    WinLoss = transactionEntity.WinLoss,
                    Payout = transactionEntity.Payout,
                    BetStatusId = transactionEntity.BetStatusId,
                    BetStatusName = transactionEntity.BetStatusName,
                    CurrencyCode = transactionEntity.CurrencyCode,
                    GameId = transactionEntity.GameId,
                    Device = transactionEntity.Device,
                    ClientType = transactionEntity.ClientType,
                    ProviderBetId = transactionEntity.ProviderBetId,
                    Category = transactionEntity.Category,
                    IsComplete = transactionEntity.IsComplete,
                    ExchangeRate = transactionEntity.ExchangeRate,
                    JackpotContributions = transactionEntity.JackpotContributions,
                    ConversionRate = transactionEntity.ConversionRate,
                    BonusType = transactionEntity.BonusType,
                    BonusPromoCode = transactionEntity.BonusPromoCode,
                    JackpotPayout = transactionEntity.JackpotPayout,
                    TableId = transactionEntity.TableId,
                    TransactionIds = transactionEntity.TransactionIds,
                    ProviderTransactionDate = transactionEntity.ProviderTransactionDate,
                    BetDate = transactionEntity.BetDate,
                    SettlementDate = transactionEntity.SettlementDate,
                    Payload = JsonSerializer.Serialize(request)
                });

                _casinoBettingContext.SaveChanges();
                scope.Complete();

                //TODO: get afer balance from response of debit call to operator api
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse
                {
                    ReferenceId = transactionEntity.BetId.ToString().ToUpper(),
                    Balance = authenticationResponse.Result.Balance
                });
            }
            else
            {
                var transactionIds = JsonSerializer.Deserialize<List<string>>(round.TransactionIds);

                if (transactionIds.Contains(request.TxnId))
                {
                    return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse 
                    { 
                        ReferenceId = round.BetId.ToString().ToUpper(), 
                        Balance = authenticationResponse.Result.Balance 
                    });
                }

                if (request.Amount == 0 && request.BonusBetAmount > 0)
                {
                    return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.REQUEST_DECLINED, new WithdrawResponse
                    {
                        ReferenceId = round.BetId.ToString().ToUpper(),
                        Balance = authenticationResponse.Result.Balance
                    });
                }

                //call platform.operator.api for debit

                using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                round.ProviderTransactionId = request.TxnId;
                round.Stake += request.Amount;
                round.IsComplete = isComplete;
                round.BetStatusId = isComplete ? (int)BetStatus.Lost : (int)BetStatus.Running;
                round.BetStatusName = isComplete ? BetStatus.Lost.ToString() : BetStatus.Running.ToString();
                round.ProviderTransactionDate = DateTime.Parse(request.Created.Split('[').First()).ToUniversalTime();

                if (isComplete)
                {
                    round.WinLoss = round.Stake * -1;
                    round.SettlementDate = dateNow;
                }


                if (transactionIds.Contains(request.TxnId) == false)
                {
                    transactionIds.Add(request.TxnId);

                    round.TransactionIds = JsonSerializer.Serialize(transactionIds);
                }

                var transactionEntity = _casinoBettingContext.Transaction.Update(round).Entity;

                _casinoBettingContext.TransactionAudit.Add(new Shared.Data.CasinoBetting.Entities.CasinoBetAudit
                {
                    BetId = transactionEntity.BetId,
                    ProviderTransactionId = transactionEntity.ProviderTransactionId,
                    TransactionType = transactionEntity.TransactionType,
                    ClientId = transactionEntity.ClientId,
                    UserId = transactionEntity.UserId,
                    RoundId = transactionEntity.RoundId,
                    Stake = transactionEntity.Stake,
                    StakeBonus = transactionEntity.StakeBonus,
                    WinLoss = transactionEntity.WinLoss,
                    Payout = transactionEntity.Payout,
                    BetStatusId = transactionEntity.BetStatusId,
                    BetStatusName = transactionEntity.BetStatusName,
                    CurrencyCode = transactionEntity.CurrencyCode,
                    GameId = transactionEntity.GameId,
                    Device = transactionEntity.Device,
                    ClientType = transactionEntity.ClientType,
                    ProviderBetId = transactionEntity.ProviderBetId,
                    Category = transactionEntity.Category,
                    IsComplete = transactionEntity.IsComplete,
                    ExchangeRate = transactionEntity.ExchangeRate,
                    JackpotContributions = transactionEntity.JackpotContributions,
                    ConversionRate = transactionEntity.ConversionRate,
                    BonusType = transactionEntity.BonusType,
                    BonusPromoCode = transactionEntity.BonusPromoCode,
                    JackpotPayout = transactionEntity.JackpotPayout,
                    TableId = transactionEntity.TableId,
                    TransactionIds = transactionEntity.TransactionIds,
                    ProviderTransactionDate = transactionEntity.ProviderTransactionDate,
                    BetDate = transactionEntity.BetDate,
                    SettlementDate = transactionEntity.SettlementDate,
                    Payload = JsonSerializer.Serialize(request)
                });

                _casinoBettingContext.SaveChanges();
                scope.Complete();

                //TODO: get afer balance from response of debit call to operator api
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse
                {
                    ReferenceId = transactionEntity.BetId.ToString().ToUpper(),
                    Balance = authenticationResponse.Result.Balance
                });
            }
        }
        public async Task<Tuple<DepositCode, DepositResponse>> Deposit(DepositRequest request)
        {
            var round = _casinoBettingContext.Transaction.Where(c => c.RoundId == request.RoundId).FirstOrDefault();

            var isComplete = request.Completed.ToLower() == "true" ? true : false;

            if (round != null)
            {
                var dateNow = DateTime.UtcNow;

                var transactionIds = JsonSerializer.Deserialize<List<string>>(round.TransactionIds);

                if (transactionIds.Contains(request.TxnId))
                {
                    return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse
                    {
                        ReferenceId = round.BetId.ToString().ToUpper(),
                        Balance = 0m
                    });
                }

                //call platform.operator.api for credit

                using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                round.ProviderTransactionId = request.TxnId;
                round.TransactionType = request.TxnType;
                round.Payout += request.Amount;
                round.IsComplete = isComplete;
                round.BetStatusId = isComplete ? round.Payout > 0 ? (int)BetStatus.Won : (int)BetStatus.Lost : (int)BetStatus.Running;
                round.BetStatusName = isComplete ? round.Payout > 0 ? BetStatus.Won.ToString() : BetStatus.Lost.ToString() : BetStatus.Running.ToString();
                round.ProviderTransactionDate = DateTime.Parse(request.Created.Split('[').First()).ToUniversalTime();

                if (isComplete)
                {
                    round.WinLoss = round.Payout - round.Stake;
                    round.SettlementDate = dateNow;
                }

                if (transactionIds.Contains(request.TxnId) == false)
                {
                    transactionIds.Add(request.TxnId);

                    round.TransactionIds = JsonSerializer.Serialize(transactionIds);
                }

                var transactionEntity = _casinoBettingContext.Transaction.Update(round).Entity;

                _casinoBettingContext.TransactionAudit.Add(new Shared.Data.CasinoBetting.Entities.CasinoBetAudit
                {
                    BetId = transactionEntity.BetId,
                    ProviderTransactionId = transactionEntity.ProviderTransactionId,
                    TransactionType = transactionEntity.TransactionType,
                    ClientId = transactionEntity.ClientId,
                    UserId = transactionEntity.UserId,
                    RoundId = transactionEntity.RoundId,
                    Stake = transactionEntity.Stake,
                    StakeBonus = transactionEntity.StakeBonus,
                    WinLoss = transactionEntity.WinLoss,
                    Payout = transactionEntity.Payout,
                    BetStatusId = transactionEntity.BetStatusId,
                    BetStatusName = transactionEntity.BetStatusName,
                    CurrencyCode = transactionEntity.CurrencyCode,
                    GameId = transactionEntity.GameId,
                    Device = transactionEntity.Device,
                    ClientType = transactionEntity.ClientType,
                    ProviderBetId = transactionEntity.ProviderBetId,
                    Category = transactionEntity.Category,
                    IsComplete = transactionEntity.IsComplete,
                    ExchangeRate = transactionEntity.ExchangeRate,
                    JackpotContributions = transactionEntity.JackpotContributions,
                    ConversionRate = transactionEntity.ConversionRate,
                    BonusType = transactionEntity.BonusType,
                    BonusPromoCode = transactionEntity.BonusPromoCode,
                    JackpotPayout = transactionEntity.JackpotPayout,
                    TableId = transactionEntity.TableId,
                    TransactionIds = transactionEntity.TransactionIds,
                    ProviderTransactionDate = transactionEntity.ProviderTransactionDate,
                    BetDate = transactionEntity.BetDate,
                    SettlementDate = transactionEntity.SettlementDate,
                    Payload = JsonSerializer.Serialize(request)
                });

                _casinoBettingContext.SaveChanges();
                scope.Complete();

                //TODO: get afer balance from response of credit call to operator api
                return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse
                {
                    ReferenceId = transactionEntity.BetId.ToString().ToUpper()
                });
            }

            return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse());
        }
        public async Task<Tuple<RollbackCode, RollbackResponse>> Rollback(RollbackRequest request)
        {
            var round = _casinoBettingContext.Transaction.Where(c => c.RoundId == request.RoundId).FirstOrDefault();

            var isComplete = request.Completed.ToLower() == "true" ? true : false;

            if (round != null)
            {
                var dateNow = DateTime.UtcNow;

                var transactionIds = JsonSerializer.Deserialize<List<string>>(round.TransactionIds);

                if (transactionIds.Contains(request.TxnId))
                {
                    return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.OK, new RollbackResponse
                    {
                        ReferenceId = round.BetId.ToString().ToUpper(),
                        Balance = 0m
                    });
                }

                //call platform.operator.api for credit

                using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                round.ProviderTransactionId = request.TxnId;
                round.TransactionType = "ROLLBACK";

                if (isComplete)
                {
                    round.Stake -= round.Stake == 0 ? 0 : request.Amount;
                    round.Payout = request.Amount;
                    round.WinLoss = 0;
                    round.BetStatusId = (int)BetStatus.Void;
                    round.BetStatusName = BetStatus.Void.ToString();
                    round.IsComplete = isComplete;
                    round.SettlementDate = dateNow;
                }

                round.ProviderTransactionDate = DateTime.Parse(request.Created.Split('[').First()).ToUniversalTime();

                if (transactionIds.Contains(request.TxnId) == false)
                {
                    transactionIds.Add(request.TxnId);

                    round.TransactionIds = JsonSerializer.Serialize(transactionIds);
                }

                var transactionEntity = _casinoBettingContext.Transaction.Update(round).Entity;

                _casinoBettingContext.TransactionAudit.Add(new Shared.Data.CasinoBetting.Entities.CasinoBetAudit
                {
                    BetId = transactionEntity.BetId,
                    ProviderTransactionId = transactionEntity.ProviderTransactionId,
                    TransactionType = transactionEntity.TransactionType,
                    ClientId = transactionEntity.ClientId,
                    UserId = transactionEntity.UserId,
                    RoundId = transactionEntity.RoundId,
                    Stake = transactionEntity.Stake,
                    StakeBonus = transactionEntity.StakeBonus,
                    WinLoss = transactionEntity.WinLoss,
                    Payout = transactionEntity.Payout,
                    BetStatusId = transactionEntity.BetStatusId,
                    BetStatusName = transactionEntity.BetStatusName,
                    CurrencyCode = transactionEntity.CurrencyCode,
                    GameId = transactionEntity.GameId,
                    Device = transactionEntity.Device,
                    ClientType = transactionEntity.ClientType,
                    ProviderBetId = transactionEntity.ProviderBetId,
                    Category = transactionEntity.Category,
                    IsComplete = transactionEntity.IsComplete,
                    ExchangeRate = transactionEntity.ExchangeRate,
                    JackpotContributions = transactionEntity.JackpotContributions,
                    ConversionRate = transactionEntity.ConversionRate,
                    BonusType = transactionEntity.BonusType,
                    BonusPromoCode = transactionEntity.BonusPromoCode,
                    JackpotPayout = transactionEntity.JackpotPayout,
                    TableId = transactionEntity.TableId,
                    TransactionIds = transactionEntity.TransactionIds,
                    ProviderTransactionDate = transactionEntity.ProviderTransactionDate,
                    BetDate = transactionEntity.BetDate,
                    SettlementDate = transactionEntity.SettlementDate,
                    Payload = JsonSerializer.Serialize(request)
                });

                _casinoBettingContext.SaveChanges();
                scope.Complete();

                //TODO: get afer balance from response of credit call to operator api
                return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.OK, new RollbackResponse 
                {
                    ReferenceId = transactionEntity.BetId.ToString().ToUpper()
                });
            }

            return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.OK, new RollbackResponse());
        }
        public async Task<BonusStatusCode> ProcessBonusStatus(BonusStatusRequest request)
        {
            return BonusStatusCode.OK;
        }
        public async Task<Tuple<BonusRewardCode, BonusRewardResponse>> ProcessBonusReward(BonusRewardRequest request)
        {

            //connect to platform database to validate clientid
            //call platform.operator.api to debit
            //once successful/failed, store credit transaction in CasinoBetting database

            return new Tuple<BonusRewardCode, BonusRewardResponse>(BonusRewardCode.OK, new BonusRewardResponse());
        }
        public async Task<Tuple<GameLobbyCode, GameLobbyResponseDTO>> GetLobbyURL(GameLobbyRequestDTO request)
        {
            var key = "qttoken";
            var token = _memCache.GetCache<AuthenticationResponse>(key);

            if (token == null)
            {
                var initialResult = await _providerRepository.GetToken();

                if (initialResult.Item1 == AuthenticationCode.OK)
                {
                    token = initialResult.Item2;
                    _memCache.AddOrUpdateCache(key, token, 5);
                }
                else
                    return new Tuple<GameLobbyCode, GameLobbyResponseDTO>(GameLobbyCode.PROVIDER_ERROR, new GameLobbyResponseDTO());
            }

            var payload = new GameLobbyRequest
            {
                Token = token.Access_token,
                PlayerId = request.UserId,
                Currency = request.CurrencyCode,
                Country = request.CountryCode,
                Lang = $"{request.LanguageCode}_{request.CountryCode}",
                Mode = request.IsDemo ? "DEMO" : "REAL_ONLY",
                Device = request.IsMobile ? "mobile" : "desktop",
                WalletSessionId = request.OperatorToken,
                Balance = request.ShowBalance,
                GameHistory = request.ShowBetHistory,
                GameTypes = request.GameTypes
            };

            var result = await _providerRepository.GetGameLobbyURL(payload);

            if (result.Item1 == GameLobbyCode.INVALID_TOKEN)
            {
                _memCache.DeleteCache("qttoken");

                 return await GetLobbyURL(request);
            }
            else
            {
                return new Tuple<GameLobbyCode, GameLobbyResponseDTO>(result.Item1, new GameLobbyResponseDTO { Url = result.Item2.Url });
            }
        }

        private async Task<BaseResponseDTO<ClientResponse>> GetClient(string clientId)
        {
            var key = $"cc:{clientId.ToUpper()}";

            var cache = _memCache.GetCache<BaseResponseDTO<ClientResponse>>(key);

            if (cache != null)
                return cache;

            var result = await _platformContext.Client.FindAsync(Guid.Parse(clientId));

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
                        ClientIdRef = result.ClientIdRef,
                        IsAgent = result.IsAgent
                    }
                };

                _memCache.AddOrUpdateCache(key, finalResult, 24);

                return finalResult;
            }
        }
    }

    public class ClientResponse
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public int ClientIdRef { get; set; }
        public bool IsAgent { get; set; }
    }
    public class ForexEURResponse
    {
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
    }
}
