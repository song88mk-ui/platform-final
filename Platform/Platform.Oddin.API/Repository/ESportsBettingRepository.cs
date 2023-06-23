using Platform.Oddin.API.Enums;
using Platform.Oddin.API.Models;
using Platform.Shared.Data.ESportBetting;
using Platform.Shared.Data.ESportBetting.SQLEntities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;
using TransactionType = Platform.Oddin.API.Enums.TransactionType;

namespace Platform.Oddin.API.Repository
{
    public class ESportsBettingRepository : IESportsBettingRepository
    {
        private readonly IMemCache _memCache;
        private readonly IRabbitMQRepository _rabbitMQRepository;

        private readonly IOperatorRepository _operatorRepository;
        private readonly ESportBettingContext _eSportsBettingContext;
        private readonly PlatformContext _platformContext;

        public ESportsBettingRepository(IOperatorRepository operatorRepository, IRabbitMQRepository rabbitMqRepository, ESportBettingContext eSportsBettingContext, PlatformContext platformContext, IMemCache memCache)
        {
            _memCache = memCache;
            _operatorRepository = operatorRepository;
            _rabbitMQRepository = rabbitMqRepository;

            _eSportsBettingContext = eSportsBettingContext;
            _platformContext = platformContext;
        }

        public async Task<UserDetailsResponse> GetUserDetails(UserDetailsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return new UserDetailsResponse()
                    {
                        ErrorCode = (int)SessionCode.InvalidToken
                    };
                }

                if (request.Token.Contains('|') == false)
                {
                    return new UserDetailsResponse()
                    {
                        ErrorCode = (int)SessionCode.InvalidToken
                    };
                }

                var split = request.Token.Split('|');
                var clientId = split.First();
                var operatorToken = split.Last();

                var clientResponse = await GetClient(clientId);

                if (clientResponse.StatusCode != StatusCode.Success)
                {
                    return new UserDetailsResponse()
                    {
                        ErrorCode = (int)SessionCode.InvalidToken
                    };
                }

                var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

                if (authenticationResponse.StatusCode != StatusCode.Success)
                {
                    return new UserDetailsResponse()
                    {
                        ErrorCode = (int)SessionCode.InvalidToken
                    };
                }

                return new UserDetailsResponse()
                {
                    UserId = $"{clientResponse.Result.ClientCode}-{authenticationResponse.Result.UserId}",
                    Currency = authenticationResponse.Result.CurrencyCode,
                    Language = authenticationResponse.Result.LanguageCode,
                    ErrorCode = (int)SessionCode.OK
                };
            }
            catch
            {
                return new UserDetailsResponse()
                {
                    ErrorCode = (int)SessionCode.OtherError
                };
            }
        }

        public async Task<DebitUserResponse> DebitUser(DebitUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token) || request.Token.Contains('|') == false)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                var split = request.Token.Split('|');
                var clientId = split.First();
                var operatorToken = split.Last();

                var clientResponse = await GetClient(clientId);

                if (clientResponse.StatusCode != StatusCode.Success)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

                if (authenticationResponse.StatusCode != StatusCode.Success)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                if (authenticationResponse.Result.Balance < request.Amount.Amount)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InsufficientFunds };

                if (authenticationResponse.Result.CurrencyCode != request.Currency)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidCurrency };

                var bet = _eSportsBettingContext.ESportBet.FirstOrDefault(c => c.ProviderBetId == request.TicketId);

                if (bet == null)
                {
                    var betId = Guid.NewGuid();

                    var dateNow = DateTime.UtcNow;
                    var betStatus = GetBetStatus(request.TransactionType);
                    var odds = request.Ticket.Bets.FirstOrDefault()?.TotalOdds ?? 0m;

                    var walletPlacementRequest = new OperatorESportsBetPlacementRequestDTO
                    {
                        TransactionId = request.TransactionId,
                        BetId = $"{betId.ToString().ToUpper()}",
                        UserId = authenticationResponse.Result.UserId,
                        CurrencyCode = authenticationResponse.Result.CurrencyCode,
                        Stake = request.Amount.Amount,
                        Odds = odds,
                        BetStatus = betStatus.ToString(),
                        StakeTax = 0,
                        WinningsTax = 0,
                        TransactionDate = dateNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff")
                    };

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnESportBetPlacementRequest
                        {
                            TransactionId = request.TransactionId,
                            BetId = betId,
                            ClientCode = clientResponse.Result.ClientCode,
                            ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                            Endpoint = $"{clientResponse.Result.Endpoint}/v1/esports/bet/placement",
                            OperatorToken = operatorToken,
                            Payload = walletPlacementRequest,
                        }),
                        CorrelationId = request.TransactionId.ToUpper(),
                        MessageType = "OnESportBetPlacementRequest",
                        RoutingKey = "r.bet.esportoperator"
                    });

                    var walletResponse = await _operatorRepository.PlaceBet(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent, walletPlacementRequest);

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnESportBetPlacementResponse
                        {
                            TransactionId = request.TransactionId,
                            BetId = betId,
                            ClientCode = clientResponse.Result.ClientCode,
                            ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                            Endpoint = $"{clientResponse.Result.Endpoint}/v1/esports/bet/placement",
                            OperatorToken = operatorToken,
                            Payload = walletResponse.Result,
                        }),
                        CorrelationId = request.TransactionId.ToUpper(),
                        MessageType = "OnESportBetPlacementResponse",
                        RoutingKey = "r.bet.esportoperator"
                    });

                    if (walletResponse.StatusCode == StatusCode.Success)
                    {
                        var rate = _platformContext.ForexEUR.Find(authenticationResponse.Result.CurrencyCode);

                        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                        var transactionEntity = _eSportsBettingContext.ESportBet.Add(new ESportBet
                        {
                            BetId = betId,
                            ProviderTransactionId = request.TransactionId,
                            ProviderBetId = request.TicketId,
                            TransactionType = Enum.GetName(typeof(TransactionType), request.TransactionType),
                            ClientId = clientId,
                            ClientCode = clientResponse.Result.ClientCode,
                            ClientName = clientResponse.Result.ClientName,
                            Endpoint = clientResponse.Result.Endpoint,
                            UserId = authenticationResponse.Result.UserId,
                            OperatorToken = operatorToken,
                            IsAgent = clientResponse.Result.IsAgent,
                            Stake = request.Ticket.Stake.Amount,
                            WinLoss = 0,
                            Payout = request.Ticket.PossibleWinAmount.Amount,
                            Odds = request.Ticket.Bets.FirstOrDefault()?.TotalOdds ?? 0m,
                            BetStatusId = (int)betStatus,
                            BetStatusName = betStatus.ToString(),
                            Reason = request.Description,
                            CurrencyCode = authenticationResponse.Result.CurrencyCode,
                            Ticket = JsonSerializer.Serialize(request.Ticket),
                            Amount = request.Amount.Amount.ToString(),
                            TransactionIds = JsonSerializer.Serialize(new List<string> { $"{request.TransactionId}|{walletResponse.Result.Balance}" }),
                            ProviderTransactionDate = request.Ticket.Date,
                            BetDate = dateNow,
                            SettlementDate = dateNow
                        }).Entity;

                        _eSportsBettingContext.ESportBetAudit.Add(new ESportBetAudit
                        {
                            BetId = transactionEntity.BetId,
                            ProviderTransactionId = transactionEntity.ProviderTransactionId,
                            ProviderBetId = transactionEntity.ProviderBetId,
                            TransactionType = transactionEntity.TransactionType,
                            ClientId = transactionEntity.ClientId,
                            ClientCode = transactionEntity.ClientCode,
                            ClientName = transactionEntity.ClientName,
                            Endpoint = transactionEntity.Endpoint,
                            UserId = transactionEntity.UserId,
                            OperatorToken = transactionEntity.OperatorToken,
                            IsAgent = transactionEntity.IsAgent,
                            Stake = transactionEntity.Stake,
                            WinLoss = transactionEntity.WinLoss,
                            Payout = transactionEntity.Payout,
                            Odds = transactionEntity.Odds,
                            BetStatusId = transactionEntity.BetStatusId,
                            BetStatusName = transactionEntity.BetStatusName,
                            Reason = transactionEntity.Reason,
                            CurrencyCode = transactionEntity.CurrencyCode,
                            Ticket = transactionEntity.Ticket,
                            Amount = transactionEntity.Amount,
                            TransactionIds = transactionEntity.TransactionIds,
                            ProviderTransactionDate = transactionEntity.ProviderTransactionDate,
                            BetDate = transactionEntity.BetDate,
                            SettlementDate = transactionEntity.SettlementDate,
                            Payload = JsonSerializer.Serialize(request)
                        });

                        _eSportsBettingContext.SaveChanges();
                        scope.Complete();

                        _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                        {
                            Payload = JsonSerializer.Serialize(new OnWalletChange
                            {
                                ClientId = transactionEntity.ClientId.ToString().ToUpper(),
                                UserId = transactionEntity.UserId,
                                Balance = walletResponse.Result.Balance
                            }),
                            CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                            MessageType = "OnWalletChange",
                            RoutingKey = $"r.wallet.{transactionEntity.ClientCode.ToLower()}"
                        });
                    }
                    else
                    {
                        switch (walletResponse.StatusCode)
                        {
                            case StatusCode.Insufficient_Funds:
                                return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InsufficientFunds };
                            case StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers:
                                return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };
                            default:
                                return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.OtherError };
                        }
                    }
                }
                else
                {
                    if (request.TransactionId == ((int)TransactionType.DebitAfterCredit).ToString())
                    {
                        return new DebitUserResponse()
                        {
                            OperatorTransactionId = request.TransactionId,
                            ErrorCode = (int)SessionCode.OK
                        };
                    }

                    var transactionIds = JsonSerializer.Deserialize<List<string>>(bet.TransactionIds).ToList();
                    var validTransactionTypeId = new int[] { (int)TransactionType.Block, (int)TransactionType.DebitWithUpdatedPossibleWin };
                    
                    if (!validTransactionTypeId.Contains(request.TransactionType))
                    {
                        var transactionId = transactionIds.Select(s => s.Split('|').First()).Where(c => c.Contains(request.TransactionId)).FirstOrDefault();

                        if (transactionId != null)
                        {
                            return new DebitUserResponse()
                            {
                                OperatorTransactionId = request.TransactionId,
                                ErrorCode = (int)SessionCode.OK
                            };
                        }
                    }

                    using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                    var odds = request.Ticket.Bets.FirstOrDefault()?.TotalOdds ?? 0m;

                    bet.ProviderTransactionId = request.TransactionId;
                    bet.TransactionType = Enum.GetName(typeof(TransactionType), request.TransactionType);
                    bet.Payout = request.Ticket.PossibleWinAmount.Amount;
                    bet.ProviderTransactionDate = request.Ticket.Date;
                    bet.Odds = odds;
                    bet.Reason = request.Description;

                    //transactionIds.Add($"{request.TransactionId}|{authenticationResponse.Result.Balance}");
                    //bet.TransactionIds = JsonSerializer.Serialize(transactionIds);

                    var transactionEntity = _eSportsBettingContext.ESportBet.Update(bet).Entity;

                    _eSportsBettingContext.ESportBetAudit.Add(new ESportBetAudit
                    {
                        BetId = transactionEntity.BetId,
                        ProviderTransactionId = transactionEntity.ProviderTransactionId,
                        ProviderBetId = transactionEntity.ProviderBetId,
                        TransactionType = transactionEntity.TransactionType,
                        ClientId = transactionEntity.ClientId,
                        ClientCode = transactionEntity.ClientCode,
                        ClientName = transactionEntity.ClientName,
                        Endpoint = transactionEntity.Endpoint,
                        UserId = transactionEntity.UserId,
                        OperatorToken = transactionEntity.OperatorToken,
                        IsAgent = transactionEntity.IsAgent,
                        Stake = transactionEntity.Stake,
                        WinLoss = transactionEntity.WinLoss,
                        Payout = transactionEntity.Payout,
                        Odds = transactionEntity.Odds,
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
                        CurrencyCode = transactionEntity.CurrencyCode,
                        Ticket = transactionEntity.Ticket,
                        Amount = transactionEntity.Amount,
                        TransactionIds = transactionEntity.TransactionIds,
                        ProviderTransactionDate = transactionEntity.ProviderTransactionDate,
                        BetDate = transactionEntity.BetDate,
                        SettlementDate = transactionEntity.SettlementDate,
                        Payload = JsonSerializer.Serialize(request)
                    });

                    _eSportsBettingContext.SaveChanges();
                    scope.Complete();
                }

                return new DebitUserResponse()
                {
                    OperatorTransactionId = request.TransactionId,
                    ErrorCode = (int)SessionCode.OK
                };
            }
            catch (Exception)
            {
                return new DebitUserResponse()
                {
                    OperatorTransactionId = request.TransactionId,
                    ErrorCode = (int)SessionCode.OtherError
                };
            }
        }

        public async Task<CreditUserResponse> CreditUser(CreditUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token) || request.Token.Contains('|') == false)
                    return new CreditUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                var split = request.Token.Split('|');
                var clientId = split.First();
                var operatorToken = split.Last();

                var clientResponse = await GetClient(clientId);

                if (clientResponse.StatusCode != StatusCode.Success)
                    return new CreditUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

                if (authenticationResponse.StatusCode != StatusCode.Success)
                    return new CreditUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                if (authenticationResponse.Result.CurrencyCode != request.Currency)
                    return new CreditUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidCurrency };

                var dateNow = DateTime.UtcNow;
                var bet = _eSportsBettingContext.ESportBet.FirstOrDefault(c => c.ProviderBetId == request.TicketId);

                if (bet == null)
                {
                    return new CreditUserResponse()
                    {
                        OperatorTransactionId = request.TransactionId,
                        ErrorCode = (int)SessionCode.OK
                    };
                }

                var transactionIds = JsonSerializer.Deserialize<List<string>>(bet.TransactionIds).ToList();
                var transactionId = transactionIds.Select(s => s.Split('|').First()).Where(c => c.Contains(request.TransactionId)).FirstOrDefault();

                if (transactionId != null)
                {
                    return new CreditUserResponse()
                    {
                        OperatorTransactionId = request.TransactionId,
                        ErrorCode = (int)SessionCode.OK
                    };
                }

                var betStatus = GetBetStatus(request.TransactionType);

                var walletSettlementRequest = new OperatorESportsBetSettlementRequestDTO
                {
                    TransactionId = bet.ProviderTransactionId,
                    BetId = $"{bet.BetId.ToString().ToUpper()}",
                    UserId = bet.UserId,
                    CurrencyCode = bet.CurrencyCode,
                    Stake = bet.Stake,
                    Odds = bet.Odds,
                    BetStatus = betStatus.ToString(),
                    StakeTax = 0,
                    WinningsTax = 0,
                    Payout = request.Amount.Amount,
                    TransactionDate = dateNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff")
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(new OnESportBetSettlementRequest
                    {
                        TransactionId = request.TransactionId,
                        BetId = bet.BetId,
                        ClientCode = clientResponse.Result.ClientCode,
                        ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                        Endpoint = $"{clientResponse.Result.Endpoint}/v1/esports/bet/settlement",
                        OperatorToken = operatorToken,
                        Payload = walletSettlementRequest,
                    }),
                    CorrelationId = request.TransactionId.ToUpper(),
                    MessageType = "OnESportBetSettlementRequest",
                    RoutingKey = "r.bet.esportoperator"
                });

                var walletResponse = await _operatorRepository.SettleBet(bet.ClientId.ToString().ToUpper(), bet.Endpoint, walletSettlementRequest);

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(new OnESportBetSettlementResponse
                    {
                        TransactionId = request.TransactionId,
                        BetId = bet.BetId,
                        ClientCode = clientResponse.Result.ClientCode,
                        ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                        Endpoint = $"{clientResponse.Result.Endpoint}/v1/esports/bet/settlement",
                        OperatorToken = operatorToken,
                        Payload = walletResponse.Result,
                    }),
                    CorrelationId = request.TransactionId.ToUpper(),
                    MessageType = "OnESportBetSettlementResponse",
                    RoutingKey = "r.bet.esportoperator"
                });

                if (walletResponse.StatusCode == StatusCode.Success)
                {
                    var rate = _platformContext.ForexEUR.Find(authenticationResponse.Result.CurrencyCode);

                    using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                    bet.ProviderTransactionId = request.TransactionId;
                    bet.TransactionType = Enum.GetName(typeof(TransactionType), request.TransactionType);
                    bet.Payout = request.Amount.Amount;
                    bet.BetStatusId = (int)betStatus;
                    bet.BetStatusName = betStatus.ToString();
                    bet.ProviderTransactionDate = request.Ticket.Date;
                    bet.Reason = request.Description;

                    if (betStatus != BetStatus.Running)
                    {
                        bet.WinLoss = bet.Payout - bet.Stake;
                        bet.SettlementDate = dateNow;
                    }

                    transactionIds.Add($"{request.TransactionId}|{walletResponse.Result.Balance}");

                    bet.TransactionIds = JsonSerializer.Serialize(transactionIds);

                    var transactionEntity = _eSportsBettingContext.ESportBet.Update(bet).Entity;

                    _eSportsBettingContext.ESportBetAudit.Add(new ESportBetAudit
                    {
                        BetId = transactionEntity.BetId,
                        ProviderTransactionId = transactionEntity.ProviderTransactionId,
                        ProviderBetId = transactionEntity.ProviderBetId,
                        TransactionType = transactionEntity.TransactionType,
                        ClientId = transactionEntity.ClientId,
                        ClientCode = transactionEntity.ClientCode,
                        ClientName = transactionEntity.ClientName,
                        Endpoint = transactionEntity.Endpoint,
                        UserId = transactionEntity.UserId,
                        OperatorToken = transactionEntity.OperatorToken,
                        IsAgent = transactionEntity.IsAgent,
                        Stake = transactionEntity.Stake,
                        WinLoss = transactionEntity.WinLoss,
                        Payout = transactionEntity.Payout,
                        Odds = transactionEntity.Odds,
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
                        CurrencyCode = transactionEntity.CurrencyCode,
                        Ticket = transactionEntity.Ticket,
                        Amount = transactionEntity.Amount,
                        TransactionIds = transactionEntity.TransactionIds,
                        ProviderTransactionDate = transactionEntity.ProviderTransactionDate,
                        BetDate = transactionEntity.BetDate,
                        SettlementDate = transactionEntity.SettlementDate,
                        Payload = JsonSerializer.Serialize(request)
                    });

                    _eSportsBettingContext.SaveChanges();
                    scope.Complete();

                    if (bet.BetStatusId != (int)BetStatus.Running)
                    {
                        _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                        {
                            Payload = JsonSerializer.Serialize(new OnWalletChange
                            {
                                ClientId = transactionEntity.ClientId.ToString().ToUpper(),
                                UserId = transactionEntity.UserId,
                                Balance = walletResponse.Result.Balance
                            }),
                            CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                            MessageType = "OnWalletChange",
                            RoutingKey = $"r.wallet.{transactionEntity.ClientCode.ToLower()}"
                        });
                    }

                    return new CreditUserResponse()
                    {
                        OperatorTransactionId = request.TransactionId,
                        ErrorCode = (int)SessionCode.OK
                    };
                }
                else
                {
                    switch (walletResponse.StatusCode)
                    {
                        case StatusCode.Insufficient_Funds:
                            return new CreditUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InsufficientFunds };
                        case StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers:
                            return new CreditUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };
                        default:
                            return new CreditUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.OtherError };
                    }
                }
            }
            catch (Exception)
            {
                return new CreditUserResponse()
                {
                    OperatorTransactionId = request.TransactionId,
                    ErrorCode = (int)SessionCode.OtherError
                };
            }
        }

        #region Utilities

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

        private BetStatus GetBetStatus(int transactionType)
        {
            var retval = BetStatus.Waiting;

            switch ((TransactionType)transactionType)
            {
                case TransactionType.DebitAfterCredit:
                    retval = BetStatus.Cancelled;
                    break;
                case TransactionType.Block:
                case TransactionType.DebitWithUpdatedPossibleWin:
                    retval = BetStatus.Running;
                    break;
                case TransactionType.Refund:
                    retval = BetStatus.Rejected;
                    break;
                case TransactionType.Credit:
                    retval = BetStatus.Won;
                    break;
                case TransactionType.Void:
                    retval = BetStatus.Void;
                    break;
                case TransactionType.Lost:
                    retval = BetStatus.Lost;
                    break;
                default:
                    retval = BetStatus.Running;
                    break;
            }

            return retval;
        }

        #endregion
    }
}
