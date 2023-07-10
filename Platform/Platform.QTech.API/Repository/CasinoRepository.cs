using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.CasinoBetting;
using Platform.Shared.Data.CasinoBetting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Reports.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
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
using TransactionOptions = System.Transactions.TransactionOptions;

namespace Platform.QTech.API.Repository
{
    public class CasinoRepository : ICasinoRepository
    {
        private readonly IConfiguration _configuration;

        private static IMongoDatabase _db;
        private static IMongoCollection<BettingStatus> bettingStatusCollection;

        private readonly IProviderRepository _providerRepository;
        private readonly IOperatorRepository _operatorRepository;
        private readonly IRabbitMQRepository _rabbitMQRepository;
        private readonly IMemCache _memCache;
        private readonly PlatformContext _platformContext;
        private readonly CasinoBettingContext _casinoBettingContext;

        public CasinoRepository(IConfiguration configuration, IProviderRepository providerRepository, IMemCache memCache, PlatformContext platformContext, CasinoBettingContext casinoBettingContext, IOperatorRepository operatorRepository, IRabbitMQRepository rabbitMQRepository)
        {
            _configuration = configuration;
            _providerRepository = providerRepository;
            _memCache = memCache;
            _platformContext = platformContext;
            _casinoBettingContext = casinoBettingContext;
            _operatorRepository = operatorRepository;
            _rabbitMQRepository = rabbitMQRepository;

            if (_db == null)
            {
                var client = new MongoClient(_configuration["ConnectionStrings:MongoDB-Casino"]);
                _db = client.GetDatabase("Casino");

                bettingStatusCollection = _db.GetCollection<BettingStatus>("BettingStatus").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
            }
        }
        public async Task<Tuple<SessionCode, GameSessionResponse>> GetSession(GameSessionRequest request)
        {
            if (string.IsNullOrEmpty(request.WalletSession))
                return new Tuple<SessionCode, GameSessionResponse>(SessionCode.INVALID_TOKEN, new GameSessionResponse());

            if (request.WalletSession.Contains('|') == false)
                return new Tuple<SessionCode, GameSessionResponse>(SessionCode.INVALID_TOKEN, new GameSessionResponse());

            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<SessionCode, GameSessionResponse>(SessionCode.INVALID_TOKEN, new GameSessionResponse());

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
            if (string.IsNullOrEmpty(request.WalletSession))
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.OK, new BalanceResponse());

            if (request.WalletSession.Contains('|') == false)
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.OK, new BalanceResponse());

            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.REQUEST_DECLINED, new BalanceResponse());

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != StatusCode.Success)
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.OK, new BalanceResponse());

            return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.OK, new BalanceResponse
            {
                Balance = authenticationResponse.Result.Balance,
                Currency = authenticationResponse.Result.CurrencyCode
            });
        }
        public async Task<Tuple<WithdrawCode, WithdrawResponse>> Withdraw(WithdrawRequest request)
        {
            if (string.IsNullOrEmpty(request.WalletSession))
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INVALID_TOKEN, new WithdrawResponse());

            if (request.WalletSession.Contains('|') == false)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INVALID_TOKEN, new WithdrawResponse());

            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.REQUEST_DECLINED, new WithdrawResponse());

            if (request.Amount <= 0)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.REQUEST_DECLINED, new WithdrawResponse());

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSt == "betstop");
            var tempBettingStatus = await bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            if (tempBettingStatus.IsBeSt)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.REQUEST_DECLINED, new WithdrawResponse());

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != StatusCode.Success)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INVALID_TOKEN, new WithdrawResponse());

            if (authenticationResponse.Result.Balance < request.Amount)
                return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INSUFFICIENT_FUNDS, new WithdrawResponse());

            var round = _casinoBettingContext.CasinoBet.Where(c => c.RoundId == request.RoundId).FirstOrDefault();

            var dateNow = DateTime.UtcNow;

            var isComplete = request.Completed.ToLower() == "true" ? true : false;

            if (round == null)
            {
                var betId = Guid.NewGuid();

                var walletResponse = await _operatorRepository.PlaceBet(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent, new OperatorCasinoBetPlacementRequestDTO
                {
                    BetId = $"{betId.ToString().ToUpper()}|{request.TxnId}",
                    Stake = request.Amount,
                    CurrencyCode = authenticationResponse.Result.CurrencyCode
                });

                if (walletResponse.StatusCode == StatusCode.Success)
                {
                    var rate = _platformContext.ForexEUR.Find(authenticationResponse.Result.CurrencyCode);

                    using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                    var transactionEntity = _casinoBettingContext.CasinoBet.Add(new CasinoBet
                    {
                        BetId = betId,
                        ProviderTransactionId = request.TxnId,
                        TransactionType = request.TxnType,
                        ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                        ClientCode = clientResponse.Result.ClientCode.ToUpper(),
                        ClientName = clientResponse.Result.ClientName,
                        Endpoint = clientResponse.Result.Endpoint,
                        UserId = authenticationResponse.Result.UserId,
                        OperatorToken = operatorToken,
                        IsAgent = clientResponse.Result.IsAgent,
                        RoundId = request.RoundId,
                        Stake = request.Amount,
                        StakeBonus = request.BonusBetAmount,
                        WinLoss = isComplete ? request.Amount * -1 : 0,
                        Payout = 0,
                        BetStatusId = isComplete ? (int)BetStatus.Lost : (int)BetStatus.Running,
                        BetStatusName = isComplete ? BetStatus.Lost.ToString() : BetStatus.Running.ToString(),
                        Reason = string.Empty,
                        CurrencyCode = authenticationResponse.Result.CurrencyCode,
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
                        TransactionIds = JsonSerializer.Serialize(new List<string> { $"{request.TxnId}|{walletResponse.Result.Balance}" }),
                        ProviderTransactionDate = DateTime.Parse(request.Created.Split('[').First()).ToUniversalTime(),
                        BetDate = dateNow,
                        SettlementDate = dateNow
                    }).Entity;

                    _casinoBettingContext.CasinoBetAudit.Add(new CasinoBetAudit
                    {
                        BetId = transactionEntity.BetId,
                        ProviderTransactionId = transactionEntity.ProviderTransactionId,
                        TransactionType = transactionEntity.TransactionType,
                        ClientId = transactionEntity.ClientId,
                        ClientCode = transactionEntity.ClientCode,
                        ClientName = transactionEntity.ClientName,
                        Endpoint = transactionEntity.Endpoint,
                        UserId = transactionEntity.UserId,
                        OperatorToken = transactionEntity.OperatorToken,
                        IsAgent = transactionEntity.IsAgent,
                        RoundId = transactionEntity.RoundId,
                        Stake = transactionEntity.Stake,
                        StakeBonus = transactionEntity.StakeBonus,
                        WinLoss = transactionEntity.WinLoss,
                        Payout = transactionEntity.Payout,
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
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

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnWalletChange
                        {
                            ClientId = transactionEntity.ClientId.ToString().ToUpper(),
                            UserId = transactionEntity.UserId,
                            Balance = walletResponse.Result.Balance,
                            Payable = walletResponse.Result.Payable ?? 0m
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnWalletChange",
                        RoutingKey = $"r.wallet.{transactionEntity.ClientCode.ToLower()}"
                    });

                    return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse 
                    {
                        ReferenceId = betId.ToString().ToUpper(),
                        Balance = walletResponse.Result.Balance
                    });
                }
                else
                {
                    switch (walletResponse.StatusCode)
                    {
                        case StatusCode.Insufficient_Funds:
                            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INSUFFICIENT_FUNDS, new WithdrawResponse());
                        case StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers:
                            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INVALID_TOKEN, new WithdrawResponse());
                        default:
                            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.REQUEST_DECLINED, new WithdrawResponse());
                    }
                }
            }
            else
            {
                var transactionIds = JsonSerializer.Deserialize<List<string>>(round.TransactionIds);

                var transactionId = transactionIds.Where(c => c.Contains(request.TxnId)).FirstOrDefault();

                if (transactionId != null)
                {
                    return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse
                    {
                        ReferenceId = round.BetId.ToString().ToUpper(),
                        Balance = Convert.ToDecimal(transactionId.Split('|').Last()) 
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

                var walletResponse = await _operatorRepository.PlaceBet(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent, new OperatorCasinoBetPlacementRequestDTO
                {
                    BetId = $"{round.BetId.ToString().ToUpper()}|{request.TxnId}",
                    Stake = request.Amount,
                    CurrencyCode = authenticationResponse.Result.CurrencyCode
                });

                if (walletResponse.StatusCode == StatusCode.Success)
                {
                    using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                    round.ProviderTransactionId = request.TxnId;
                    round.TransactionType = request.TxnType;
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
                        transactionIds.Add($"{request.TxnId}|{walletResponse.Result.Balance}" );

                        round.TransactionIds = JsonSerializer.Serialize(transactionIds);
                    }

                    var transactionEntity = _casinoBettingContext.CasinoBet.Update(round).Entity;

                    _casinoBettingContext.CasinoBetAudit.Add(new CasinoBetAudit
                    {
                        BetId = transactionEntity.BetId,
                        ProviderTransactionId = transactionEntity.ProviderTransactionId,
                        TransactionType = transactionEntity.TransactionType,
                        ClientId = transactionEntity.ClientId,
                        ClientCode = transactionEntity.ClientCode,
                        ClientName = transactionEntity.ClientName,
                        Endpoint = transactionEntity.Endpoint,
                        UserId = transactionEntity.UserId,
                        OperatorToken = transactionEntity.OperatorToken,
                        IsAgent = transactionEntity.IsAgent,
                        RoundId = transactionEntity.RoundId,
                        Stake = transactionEntity.Stake,
                        StakeBonus = transactionEntity.StakeBonus,
                        WinLoss = transactionEntity.WinLoss,
                        Payout = transactionEntity.Payout,
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
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

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnWalletChange
                        {
                            ClientId = transactionEntity.ClientId.ToString().ToUpper(),
                            UserId = transactionEntity.UserId,
                            Balance = walletResponse.Result.Balance,
                            Payable = walletResponse.Result.Payable ?? 0m
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnWalletChange",
                        RoutingKey = $"r.wallet.{transactionEntity.ClientCode.ToLower()}"
                    });

                    return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse
                    {
                        ReferenceId = transactionEntity.BetId.ToString().ToUpper(),
                        Balance = walletResponse.Result.Balance
                    });
                }
                else
                {
                    switch (walletResponse.StatusCode)
                    {
                        case StatusCode.Insufficient_Funds:
                            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INSUFFICIENT_FUNDS, new WithdrawResponse());
                        case StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers:
                            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.INVALID_TOKEN, new WithdrawResponse());
                        default:
                            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.REQUEST_DECLINED, new WithdrawResponse());
                    }
                }
            }
        }
        public async Task<Tuple<DepositCode, DepositResponse>> Deposit(DepositRequest request)
        {
            var round = _casinoBettingContext.CasinoBet.Where(c => c.RoundId == request.RoundId).FirstOrDefault();

            var isComplete = request.Completed.ToLower() == "true" ? true : false;

            if (round != null)
            {
                var dateNow = DateTime.UtcNow;

                var transactionIds = JsonSerializer.Deserialize<List<string>>(round.TransactionIds);

                var transactionId = transactionIds.Where(c => c.Contains(request.TxnId)).FirstOrDefault();

                if (transactionId != null)
                {
                    return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse
                    {
                        ReferenceId = round.BetId.ToString().ToUpper(),
                        Balance = Convert.ToDecimal(transactionId.Split('|').Last())
                    });
                }

                var walletResponse = await _operatorRepository.SettleBet(round.ClientId.ToString().ToUpper(), round.Endpoint, new OperatorCasinoBetSettlementRequestDTO
                {
                    BetId = $"{round.BetId.ToString().ToUpper()}|{request.TxnId}",
                    UserId = round.UserId,
                    Payout = request.Amount,
                    BetStatus = isComplete ? request.Amount > 0 || round.Payout > 0 ? BetStatus.Won.ToString() : BetStatus.Lost.ToString() : BetStatus.Running.ToString(),
                    CurrencyCode = round.CurrencyCode
                });

                if (walletResponse.StatusCode == StatusCode.Success)
                {
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
                        transactionIds.Add($"{request.TxnId}|{walletResponse.Result.Balance}" );

                        round.TransactionIds = JsonSerializer.Serialize(transactionIds);
                    }

                    var transactionEntity = _casinoBettingContext.CasinoBet.Update(round).Entity;

                    _casinoBettingContext.CasinoBetAudit.Add(new Shared.Data.CasinoBetting.Entities.CasinoBetAudit
                    {
                        BetId = transactionEntity.BetId,
                        ProviderTransactionId = transactionEntity.ProviderTransactionId,
                        TransactionType = transactionEntity.TransactionType,
                        ClientId = transactionEntity.ClientId,
                        ClientCode = transactionEntity.ClientCode,
                        ClientName = transactionEntity.ClientName,
                        Endpoint = transactionEntity.Endpoint,
                        UserId = transactionEntity.UserId,
                        OperatorToken = transactionEntity.OperatorToken,
                        IsAgent = transactionEntity.IsAgent,
                        RoundId = transactionEntity.RoundId,
                        Stake = transactionEntity.Stake,
                        StakeBonus = transactionEntity.StakeBonus,
                        WinLoss = transactionEntity.WinLoss,
                        Payout = transactionEntity.Payout,
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
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

                    if (isComplete)
                    {
                        _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                        {
                            Payload = JsonSerializer.Serialize(transactionEntity),
                            CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                            MessageType = "OnCasinoBetSettlement",
                            RoutingKey = "r.bet.reports.agent"
                        });

                        _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                        {
                            Payload = JsonSerializer.Serialize(new OnWalletChange
                            {
                                ClientId = transactionEntity.ClientId.ToString().ToUpper(),
                                UserId = transactionEntity.UserId,
                                Balance = walletResponse.Result.Balance,
                                Payable = walletResponse.Result.Payable ?? 0m
                            }),
                            CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                            MessageType = "OnWalletChange",
                            RoutingKey = $"r.wallet.{transactionEntity.ClientCode.ToLower()}"
                        });
                    }

                    return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse
                    {
                        ReferenceId = transactionEntity.BetId.ToString().ToUpper(),
                        Balance = walletResponse.Result.Balance
                    });
                }
                else
                {
                    return new Tuple<DepositCode, DepositResponse>(DepositCode.REQUEST_DECLINED, new DepositResponse());
                }
            }

            return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse());
        }
        public async Task<Tuple<RollbackCode, RollbackResponse>> Rollback(RollbackRequest request)
        {
            var round = _casinoBettingContext.CasinoBet.Where(c => c.RoundId == request.RoundId).FirstOrDefault();

            var isComplete = request.Completed.ToLower() == "true" ? true : false;

            if (round != null)
            {
                var dateNow = DateTime.UtcNow;

                var transactionIds = JsonSerializer.Deserialize<List<string>>(round.TransactionIds);

                var transactionId = transactionIds.Where(c => c.Contains(request.TxnId)).FirstOrDefault();

                if (transactionId != null)
                {
                    return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.OK, new RollbackResponse
                    {
                        ReferenceId = round.BetId.ToString().ToUpper(),
                        Balance = Convert.ToDecimal(transactionId.Split('|').Last())
                    });
                }

                var walletResponse = await _operatorRepository.SettleBet(round.ClientId.ToString().ToUpper(), round.Endpoint, new OperatorCasinoBetSettlementRequestDTO
                {
                    BetId = $"{round.BetId.ToString().ToUpper()}|{request.TxnId}",
                    UserId = round.UserId,
                    Payout = request.Amount,
                    BetStatus = isComplete ? BetStatus.Void.ToString() : BetStatus.Running.ToString(),
                    CurrencyCode = round.CurrencyCode
                });

                if (walletResponse.StatusCode == StatusCode.Success)
                {
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
                        transactionIds.Add($"{request.TxnId}|{walletResponse.Result.Balance}" );

                        round.TransactionIds = JsonSerializer.Serialize(transactionIds);
                    }

                    var transactionEntity = _casinoBettingContext.CasinoBet.Update(round).Entity;

                    _casinoBettingContext.CasinoBetAudit.Add(new Shared.Data.CasinoBetting.Entities.CasinoBetAudit
                    {
                        BetId = transactionEntity.BetId,
                        ProviderTransactionId = transactionEntity.ProviderTransactionId,
                        TransactionType = transactionEntity.TransactionType,
                        ClientId = transactionEntity.ClientId,
                        ClientCode = transactionEntity.ClientCode,
                        ClientName = transactionEntity.ClientName,
                        Endpoint = transactionEntity.Endpoint,
                        UserId = transactionEntity.UserId,
                        OperatorToken = transactionEntity.OperatorToken,
                        IsAgent = transactionEntity.IsAgent,
                        RoundId = transactionEntity.RoundId,
                        Stake = transactionEntity.Stake,
                        StakeBonus = transactionEntity.StakeBonus,
                        WinLoss = transactionEntity.WinLoss,
                        Payout = transactionEntity.Payout,
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
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

                    if (isComplete)
                    {
                        _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                        {
                            Payload = JsonSerializer.Serialize(transactionEntity),
                            CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                            MessageType = "OnCasinoBetSettlement",
                            RoutingKey = "r.bet.reports.agent"
                        });

                        _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                        {
                            Payload = JsonSerializer.Serialize(new OnWalletChange
                            {
                                ClientId = transactionEntity.ClientId.ToString().ToUpper(),
                                UserId = transactionEntity.UserId,
                                Balance = walletResponse.Result.Balance,
                                Payable = walletResponse.Result.Payable ?? 0m
                            }),
                            CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                            MessageType = "OnWalletChange",
                            RoutingKey = $"r.wallet.{transactionEntity.ClientCode.ToLower()}"
                        });
                    }

                    return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.OK, new RollbackResponse
                    {
                        ReferenceId = transactionEntity.BetId.ToString().ToUpper(),
                        Balance = walletResponse.Result.Balance
                    });
                }
                else
                {
                    return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.REQUEST_DECLINED, new RollbackResponse());
                }
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
        public async Task<BaseResponseDTO<CasinoBetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request)
        {
            try
            {
                request.Page = request.Page <= 0 ? 1 : request.Page;
                request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;
                request.PageSize = request.PageSize > 50 ? 50 : request.PageSize;

                var skip = (request.Page - 1) * request.PageSize;
                double rowCount = 0;
                decimal winLoss = 0m;
                var result = new List<CasinoBetHistoryDTO>();
                var transactions = new List<CasinoBet>();

                if (request.IsCurrent)
                {
                    var openBets = new List<int> { (int)BetStatus.Running };
                    var openBetsResponse = _casinoBettingContext.CasinoBet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && openBets.Contains(c.BetStatusId)).ToList();

                    if (openBetsResponse.Count > 0)
                        transactions.AddRange(openBetsResponse);

                    rowCount = Convert.ToDouble(transactions.Count());
                }
                else
                {
                    if ((request.ToDate - request.FromDate).TotalDays > 30)
                        request.FromDate = request.ToDate.AddDays(-30);

                    if (request.CasinoBetStatusFilter == CasinoBetStatusFilter.All)
                    {
                        var betStatusIds = new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Rejected };
                        var rawAll = _casinoBettingContext.CasinoBet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && !betStatusIds.Contains(c.BetStatusId) && c.BetDate >= request.FromDate && c.BetDate <= request.ToDate);

                        rowCount = rawAll.Count();
                        //winLoss = rawAll.Sum(c => c.WinLoss);
                        transactions = rawAll.OrderByDescending(o => o.BetDate).Skip(skip).Take(request.PageSize).ToList();
                    }

                    if (request.CasinoBetStatusFilter == CasinoBetStatusFilter.Won)
                    {
                        var betStatusIds = new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Running, (int)BetStatus.Lost };
                        var rawOpen = _casinoBettingContext.CasinoBet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && !betStatusIds.Contains(c.BetStatusId) && c.BetDate >= request.FromDate && c.BetDate <= request.ToDate);

                        rowCount = rawOpen.Count();
                        //winLoss = rawOpen.Sum(c => c.WinLoss);
                        transactions = rawOpen.OrderByDescending(o => o.BetDate).Skip(skip).Take(request.PageSize).ToList();
                    }

                    if (request.CasinoBetStatusFilter == CasinoBetStatusFilter.Lost)
                    {
                        var betStatusIds = new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Running, (int)BetStatus.Rejected, (int)BetStatus.Won };
                        var rawSettled = _casinoBettingContext.CasinoBet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && !betStatusIds.Contains(c.BetStatusId) && c.BetDate >= request.FromDate && c.BetDate <= request.ToDate);

                        rowCount = rawSettled.Count();
                        //winLoss = rawSettled.Sum(c => c.WinLoss);
                        transactions = rawSettled.OrderByDescending(o => o.BetDate).Skip(skip).Take(request.PageSize).ToList();
                    }
                }

                if (transactions.Count == 0)
                {
                    return new BaseResponseDTO<CasinoBetHistoryResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new CasinoBetHistoryResponseDTO
                        {
                            CurrentPage = 1,
                            PageCount = 1,
                            PageSize = 1,
                            RowCount = 0,
                            TotalCommission = 0,
                            TotalWinLoss = 0,
                            TotalWLC = 0,
                            Bets = new List<CasinoBetHistoryDTO>()
                        }
                    };
                }                

                foreach (var bet in transactions)
                {
                    ProductLookup productLookup = GetProductLookUp(bet.Category, bet.GameId);

                    result.Add(new CasinoBetHistoryDTO
                    {
                        BetId = bet.BetId.ToString(),
                        Stake = bet.Stake,                        
                        Payout = bet.Payout,                        
                        BetStatusId = bet.BetStatusId,
                        BetStatusName = bet.BetStatusName,
                        TransactionDate = bet.BetDate,
                        ProductName = productLookup.ProductTypeName,
                        GameName = productLookup.ProductName,
                        GameType = productLookup.GameType,
                        ProviderName = productLookup.ProviderName,
                        ProviderBetId = bet.ProviderBetId                        
                    });
                }

                return new BaseResponseDTO<CasinoBetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new CasinoBetHistoryResponseDTO
                    {
                        CurrentPage = request.Page,
                        PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                        PageSize = request.PageSize,
                        RowCount = Convert.ToInt32(rowCount),
                        TotalCommission = 0,
                        TotalWinLoss = winLoss,
                        TotalWLC = winLoss,
                        Bets = result.OrderByDescending(o => o.TransactionDate).ToList()
                    }
                };
            }
            catch (Exception ex)
            {
                throw;
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

        private ProductLookup GetProductLookUp(string category, string gameId)
        {
            var categoryType = GetProduct(category);
            var gameType = category.Replace("CASINO/LIVECASINO/", "").Replace("CASINO/VIRTUAL_SPORTS/", "").Replace("CASINO/", "");
            var provider = !string.IsNullOrEmpty(gameId) ? gameId.Split("-").FirstOrDefault() : string.Empty;
            var productName = !string.IsNullOrEmpty(gameId) ? gameId.Split("-").LastOrDefault() : string.Empty;

            return new ProductLookup()
            {
                ProductId = (int)categoryType,
                ProductTypeName = categoryType.ToString(),
                GameType = gameType,
                ProviderName = provider,
                ProductName = productName
            };
        }

        private Product GetProduct(string category)
        {
            var newCategory = Product.Casino;

            if (category.ToLower().Contains("virtual"))
            {
                newCategory = Product.VirtualGames;
            }
            else if (category.ToLower().Contains("live"))
            {
                newCategory = Product.LiveCasino;
            }
            return newCategory;
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
    public class Idempotence
    {
        public string TI { get; set; }
        public string B { get; set; }
    }
}
