using Microsoft.Extensions.Configuration;
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
using System.Diagnostics;
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
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;
        private readonly IRabbitMQRepository _rabbitMQRepository;

        private readonly IOperatorRepository _operatorRepository;
        private readonly ESportBettingContext _eSportsBettingContext;
        private readonly PlatformContext _platformContext;

        public ESportsBettingRepository(IConfiguration configuration, IOperatorRepository operatorRepository, IRabbitMQRepository rabbitMqRepository, ESportBettingContext eSportsBettingContext, PlatformContext platformContext, IMemCache memCache)
        {
            _configuration = configuration;
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

                var walletResponse = await _operatorRepository.SettleBet(bet.ClientId.ToString().ToUpper(), bet.Endpoint, new OperatorESportsBetSettlementRequestDTO
                {
                    BetId = $"{bet.BetId.ToString().ToUpper()}",
                    UserId = bet.UserId,
                    Payout = request.Amount.Amount,
                    BetStatus = request.Amount.Amount > 0 || bet.Payout > 0 ? BetStatus.Won.ToString() : BetStatus.Lost.ToString(),
                    CurrencyCode = bet.CurrencyCode,
                    TransactionId = bet.ProviderTransactionId
                });

                if (walletResponse.StatusCode == StatusCode.Success)
                {
                    // Refund
                    // Credit
                    // Void
                    // Lost

                    var rate = _platformContext.ForexEUR.Find(authenticationResponse.Result.CurrencyCode);

                    var betStatus = BetStatus.Waiting;
                    switch ((TransactionType)request.TransactionType)
                    {
                        case TransactionType.DebitAfterCredit:
                            betStatus = BetStatus.Cancelled;
                            break;
                        case TransactionType.Block:
                        case TransactionType.DebitWithUpdatedPossibleWin:
                            betStatus = BetStatus.Running;
                            break;
                        case TransactionType.Refund:
                            betStatus = BetStatus.Rejected;
                            break;
                        case TransactionType.Credit:
                            betStatus = BetStatus.Won;
                            break;
                        case TransactionType.Void:
                            betStatus = BetStatus.Void;
                            break;
                        case TransactionType.Lost:
                            betStatus = BetStatus.Lost;
                            break;
                        default:
                            betStatus = BetStatus.Running;
                            break;
                    }

                    using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                    bet.ProviderTransactionId = request.TransactionId;
                    bet.TransactionType = Enum.GetName(typeof(TransactionType), request.TransactionType);
                    bet.Payout += request.Amount.Amount;
                    bet.BetStatusId = (int)betStatus;
                    bet.BetStatusName = betStatus.ToString();
                    bet.ProviderTransactionDate = JsonSerializer.Deserialize<Ticket>(bet.Ticket).Date;

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
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
                        CurrencyCode = transactionEntity.CurrencyCode,
                        Ticket = transactionEntity.Ticket,
                        Description = transactionEntity.Description,
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

        public async Task<DebitUserResponse> DebitUser(DebitUserRequest request)
        {
            //var stopwatch = new Stopwatch();
            try
            {
                //stopwatch.Start();
                if (string.IsNullOrEmpty(request.Token) || request.Token.Contains('|') == false)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                var split = request.Token.Split('|');
                var clientId = split.First();
                var operatorToken = split.Last();

                var clientResponse = await GetClient(clientId);

                if (clientResponse.StatusCode != StatusCode.Success)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                //stopwatch.RecordLap("Initial Validation");
                var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

                if (authenticationResponse.StatusCode != StatusCode.Success)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InvalidToken };

                var dateNow = DateTime.UtcNow;

                //stopwatch.RecordLap("Authentication Response");
                if (authenticationResponse.Result.Balance < request.Amount.Amount)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.InsufficientFunds };

                var bet = _eSportsBettingContext.ESportBet.FirstOrDefault(c => c.ProviderBetId == request.TicketId);

                var betId = bet != null ? bet.BetId : Guid.NewGuid();

                if (bet == null)
                {
                    // Block
                    var walletResponse = await _operatorRepository.PlaceBet(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent, new OperatorESportsBetPlacementRequestDTO
                    {
                        BetId = $"{betId.ToString().ToUpper()}",
                        Stake = request.Amount.Amount,
                        CurrencyCode = authenticationResponse.Result.CurrencyCode
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
                            Stake = request.Amount.Amount,
                            WinLoss = 0,
                            Payout = request.Ticket.PossibleWinAmount.TotalAmount,
                            BetStatusId = (int)BetStatus.Running,
                            BetStatusName = BetStatus.Running.ToString(),
                            Reason = string.Empty,
                            CurrencyCode = authenticationResponse.Result.CurrencyCode,
                            Ticket = JsonSerializer.Serialize(request.Ticket),
                            Description = request.Description,
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
                            BetStatusId = transactionEntity.BetStatusId,
                            BetStatusName = transactionEntity.BetStatusName,
                            Reason = transactionEntity.Reason,
                            CurrencyCode = transactionEntity.CurrencyCode,
                            Ticket = transactionEntity.Ticket,
                            Description = transactionEntity.Description,
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
                    // DebitWithUpdatedPossibleWin
                    // DebitAfterCredit

                    var transactionIds = JsonSerializer.Deserialize<List<string>>(bet.TransactionIds).ToList();

                    var transactionId = transactionIds.Select(s => s.Split('|').First()).Where(c => c.Contains(request.TransactionId)).FirstOrDefault();

                    if (transactionId != null)
                    {
                        return new DebitUserResponse()
                        {
                            OperatorTransactionId = request.TransactionId,
                            ErrorCode = (int)SessionCode.OK
                        };
                    }

                    using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

                    bet.ProviderTransactionId = request.TransactionId;
                    bet.TransactionType = Enum.GetName(typeof(TransactionType), request.TransactionType);
                    bet.Payout += request.Amount.Amount;
                    bet.ProviderTransactionDate = JsonSerializer.Deserialize<Ticket>(bet.Ticket).Date;

                    transactionIds.Add($"{request.TransactionId}|{authenticationResponse.Result.Balance}");

                    bet.TransactionIds = JsonSerializer.Serialize(transactionIds);

                    var transactionEntity = _eSportsBettingContext.ESportBet.Update(bet).Entity;

                    _eSportsBettingContext.ESportBetAudit.Add(new ESportBetAudit
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
                        Stake = transactionEntity.Stake,
                        WinLoss = transactionEntity.WinLoss,
                        Payout = transactionEntity.Payout,
                        BetStatusId = transactionEntity.BetStatusId,
                        BetStatusName = transactionEntity.BetStatusName,
                        Reason = transactionEntity.Reason,
                        CurrencyCode = transactionEntity.CurrencyCode,
                        ProviderBetId = transactionEntity.ProviderBetId,
                        Ticket = transactionEntity.Ticket,
                        Description = transactionEntity.Description,
                        Amount = transactionEntity.Amount,
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
            finally
            {
                //stopwatch.Stop();
                //stopwatch.DisplayLapTimes();
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

        #endregion
    }

    #region Stopwwatch

    public class Stopwatch
    {
        private DateTime startTime;
        private DateTime endTime;
        private bool isRunning;
        private List<TimeSpan> lapTimes;
        private List<string> lapRecords;

        public Stopwatch()
        {
            lapTimes = new List<TimeSpan>();
            lapRecords = new List<string>();
        }

        public void Start()
        {
            if (!isRunning)
            {
                startTime = DateTime.Now;
                isRunning = true;
            }
            else
            {
                Debug.WriteLine("The stopwatch is already running.");
            }
        }

        public void Stop(bool isRecordDisplay = false)
        {
            if (isRunning)
            {
                endTime = DateTime.Now;
                isRunning = false;
                TimeSpan elapsed = endTime - startTime;
                lapTimes.Add(elapsed);
                lapRecords.Add($"Lap end: {elapsed.TotalSeconds} seconds");

                if (isRecordDisplay)
                    this.DisplayLapTimes();
            }
            else
            {
                Console.WriteLine("The stopwatch is not currently running.");
            }
        }

        public void RecordLap(string title = "")
        {
            if (isRunning)
            {
                title = string.IsNullOrEmpty(title) ? (lapRecords.Count() + 1).ToString() : title;

                TimeSpan elapsed = DateTime.Now - startTime;
                lapTimes.Add(elapsed);
                //lapRecords.Add($"Lap {title}: {elapsed.TotalSeconds} seconds {lapRecords.Count() > 0 ? elapsed.TotalSeconds - lapTimes.Last().TotalSeconds : ''}");
            }
            else
            {
                Debug.WriteLine("The stopwatch is not currently running.");
            }
        }

        public void DisplayLapTimes()
        {
            Console.WriteLine("Lap Times:");
            //for (int i = 0; i < lapTimes.Count; i++)
            //{
            //    Debug.WriteLine($"Lap {i + 1}: {lapTimes[i].TotalSeconds} seconds");
            //}
            foreach (var item in lapRecords)
            {
                Debug.WriteLine(item);
            }
        }

        public void Reset()
        {
            lapTimes.Clear();
            lapRecords.Clear();
            isRunning = false;
        }
    }

    #endregion
}
