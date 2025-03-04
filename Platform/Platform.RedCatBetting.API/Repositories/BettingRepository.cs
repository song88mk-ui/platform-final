using Platform.Shared.Data.RedCatBetting;
using Platform.Shared.Data.RedCatBetting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.RedCatBetting.API.Repositories
{
    public class BettingRepository : IBettingRepository
    {
        private readonly RedCatBettingContext _redCatBettingContext;
        private readonly IOperatorRepository _operatorRepository;

        public BettingRepository(RedCatBettingContext redCatBettingContext, IOperatorRepository operatorRepository)
        {
            _redCatBettingContext = redCatBettingContext;
            _operatorRepository = operatorRepository;
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> PlaceBet(RedCatBetRequest request)
        {
            try
            {
                var timestamp = DateTime.UtcNow;

                var bet = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString());

                if (bet != null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Betting_Restricted,
                        Message = StatusCode.Betting_Restricted.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Betting_Restricted
                        }
                    };

                var betEntity = new Bet();
                var transactionRefId = string.Empty;
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    betEntity = _redCatBettingContext.Bet.Add(new Bet()
                    {
                        ClientId = request.ClientId,
                        ClientCode = request.ClientCode.ToString(),
                        Endpoint = request.Endpoint,
                        MemberCode = request.MemberCode,
                        OperatorId = request.OperatorId,
                        OperatorToken = request.OperatorToken,
                        CurrencyCode = request.CurrencyCode.ToString(),
                        ExchangeRate = request.ExhangeRate,
                        BetStatusId = (int)BetStatus.Running,
                        BetStatusName = BetStatus.Running.GetDescription(),
                        GameId = request.GameId,
                        BetMarketId = request.BetMarketId,
                        Amount = request.TransactionAmt,
                        Payout = 0,
                        BetDate = request.EventDate,
                        BetIdRef = request.BetId.ToString()
                    }).Entity;

                    //var betMapping = _redCatBettingContext.BetMapping.Add(new BetMapping()
                    //{
                    //    BetIdRef = betEntity.BetIdRef,
                    //    BetId = betEntity.BetId,
                    //    ClientId = betEntity.ClientId
                    //});

                    var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                    {
                        TransactionId = request.TransactionId,
                        BetId = betEntity.BetId,
                        CLientId = betEntity.ClientId,
                        ClientCode = betEntity.ClientCode,
                        Endpoint = betEntity.Endpoint,
                        MemberCode = request.MemberCode,
                        OperatorId = request.OperatorId,
                        OperatorToken = betEntity.OperatorToken,
                        CurrencyCode = betEntity.CurrencyCode,
                        ExchangeRate = betEntity.ExchangeRate,
                        BetStatusId = betEntity.BetStatusId,
                        BetStatusName = betEntity.BetStatusName,
                        GameId = betEntity.GameId,
                        BetMarketId = betEntity.BetMarketId,
                        Amount = betEntity.Amount,
                        Payout = betEntity.Payout,
                        BetDate = betEntity.BetDate,
                        BetIdRef = betEntity.BetIdRef,
                        Timestamp = timestamp
                    }).Entity;

                    _redCatBettingContext.SaveChanges();
                    scope.Complete();

                    transactionRefId = betAuditEntity.Id.ToString();
                }

                var operatorResponse = await _operatorRepository.ProcessBetPlacement(new RedCatOperatorBetTransactionRequest()
                {
                    BetId = betEntity.BetId,
                    BetIdRef = betEntity.BetIdRef,
                    ClientId = request.ClientId,
                    ClientCode = request.ClientCode.ToString(),
                    OperatorToken = request.OperatorToken,
                    Endpoint = request.Endpoint,
                    CurrencyCode = betEntity.CurrencyCode,
                    BetMarketId = request.BetMarketId,
                    GameId = request.GameId,
                    BetStatusId = betEntity.BetStatusId,
                    BetStatusName = betEntity.BetStatusName,
                    Stake = betEntity.Amount,
                    BetDate = betEntity.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(timestamp).ToString(),
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result?.StatusCode ?? (int)operatorResponse.StatusCode,
                            Balance = operatorResponse.Result?.Balance ?? 0,
                        }
                    };
                }

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = operatorResponse.StatusCode,
                    Message = operatorResponse.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = operatorResponse.Result.StatusCode,
                        Balance = operatorResponse.Result.Balance,
                        TransactionRefId = transactionRefId
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Error
                    }
                };
            }
        }

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> SettleBet(RedCatBetRequest request)
        {
            try
            {
                var timestamp = DateTime.UtcNow;

                var bet = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString());

                if (bet == null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Bet_Not_Found,
                        Message = StatusCode.Bet_Not_Found.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Bet_Not_Found
                        }
                    };

                var betStatus = request.TransactionAmt > 0 ? BetStatus.Won : BetStatus.Lost;

                bet.BetStatusId = (int)betStatus;
                bet.BetStatusName = betStatus.GetDescription();
                bet.Payout = request.TransactionAmt;

                var transactionRefId = string.Empty;
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    _redCatBettingContext.Update(bet);

                    var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                    {
                        TransactionId = request.TransactionId,
                        BetId = bet.BetId,
                        CLientId = bet.ClientId,
                        ClientCode = bet.ClientCode,
                        Endpoint = bet.Endpoint,
                        MemberCode = request.MemberCode,
                        OperatorId = request.OperatorId,
                        OperatorToken = bet.OperatorToken,
                        CurrencyCode = bet.CurrencyCode,
                        ExchangeRate = bet.ExchangeRate,
                        BetStatusId = bet.BetStatusId,
                        BetStatusName = bet.BetStatusName,
                        GameId = bet.GameId,
                        BetMarketId = bet.BetMarketId,
                        Amount = bet.Amount,
                        Payout = bet.Payout,
                        BetDate = bet.BetDate,
                        BetIdRef = bet.BetIdRef,
                        Timestamp = timestamp
                    }).Entity;

                    _redCatBettingContext.SaveChanges();
                    scope.Complete();

                    transactionRefId = betAuditEntity.Id.ToString();
                }

                var operatorResponse = await _operatorRepository.ProcessBetSettlement(new RedCatOperatorBetTransactionRequest()
                {
                    BetId = bet.BetId,
                    BetIdRef = bet.BetIdRef,
                    ClientId = bet.ClientId,
                    ClientCode = bet.ClientCode,
                    Endpoint = bet.Endpoint,
                    MemberCode = request.MemberCode,
                    OperatorId = request.OperatorId,
                    OperatorToken = bet.OperatorToken,
                    CurrencyCode = bet.CurrencyCode,
                    BetMarketId = request.BetMarketId,
                    GameId = request.GameId,
                    BetStatusId = bet.BetStatusId,
                    BetStatusName = bet.BetStatusName,
                    Payout = request.TransactionAmt,
                    Stake = bet.Amount,
                    BetDate = bet.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(timestamp).ToString(),
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result.StatusCode,
                            Balance = operatorResponse.Result.Balance,
                        }
                    };
                }

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = operatorResponse.StatusCode,
                    Message = operatorResponse.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = operatorResponse.Result.StatusCode,
                        Balance = operatorResponse.Result.Balance,
                        TransactionRefId = transactionRefId
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Error
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidPlacement(RedCatVoidBetRequest request)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                var transactionIdRef = string.Empty;

                var bet = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString());

                if (bet == null || bet.BetStatusId != (int)BetStatus.Running)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Bet_Not_Found,
                        Message = StatusCode.Bet_Not_Found.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Bet_Not_Found
                        }
                    };

                var betStatus = BetStatus.Void;

                bet.Payout = bet.Amount;
                bet.BetStatusId = (int)betStatus;
                bet.BetStatusName = betStatus.GetDescription();

                var transactionRefId = string.Empty;
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    _redCatBettingContext.Update(bet);

                    var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                    {
                        TransactionId = request.TransactionId,
                        BetId = bet.BetId,
                        CLientId = bet.ClientId,
                        ClientCode = bet.ClientCode,
                        Endpoint = bet.Endpoint,
                        MemberCode = request.MemberCode,
                        OperatorId = request.OperatorId,
                        OperatorToken = bet.OperatorToken,
                        CurrencyCode = bet.CurrencyCode,
                        ExchangeRate = bet.ExchangeRate,
                        BetStatusId = bet.BetStatusId,
                        BetStatusName = bet.BetStatusName,
                        GameId = bet.GameId,
                        BetMarketId = bet.BetMarketId,
                        Amount = bet.Amount,
                        Payout = bet.Payout,
                        BetDate = bet.BetDate,
                        BetIdRef = bet.BetIdRef,
                        Timestamp = timestamp
                    }).Entity;

                    _redCatBettingContext.SaveChanges();
                    scope.Complete();

                    transactionRefId = betAuditEntity.Id.ToString();
                }

                var operatorResponse = await _operatorRepository.ProcessBetSettlement(new RedCatOperatorBetTransactionRequest()
                {
                    BetId = bet.BetId,
                    BetIdRef = bet.BetIdRef,
                    ClientId = bet.ClientId,
                    ClientCode = bet.ClientCode,
                    Endpoint = bet.Endpoint,
                    MemberCode = request.MemberCode,
                    OperatorId = request.OperatorId,
                    OperatorToken = bet.OperatorToken,
                    CurrencyCode = bet.CurrencyCode,
                    BetMarketId = request.BetMarketId,
                    GameId = request.GameId,
                    BetStatusId = bet.BetStatusId,
                    BetStatusName = bet.BetStatusName,
                    Payout = bet.Amount,
                    Stake = bet.Amount,
                    BetDate = bet.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(timestamp).ToString(),
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result.StatusCode,
                            Balance = operatorResponse.Result.Balance,
                        }
                    };
                }

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = operatorResponse.StatusCode,
                    Message = operatorResponse.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = operatorResponse.Result.StatusCode,
                        Balance = operatorResponse.Result.Balance,
                        TransactionRefId = transactionRefId
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Error
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettlement(RedCatVoidBetRequest request)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                var transactionIdRef = string.Empty;

                var bet = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString());

                if (bet == null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Bet_Not_Found,
                        Message = StatusCode.Bet_Not_Found.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Bet_Not_Found
                        }
                    };

                var betTransaction = _redCatBettingContext.BetAudit.FirstOrDefault(c => c.BetId == bet.BetId && c.TransactionId == request.RollbackTransactionId);

                if (betTransaction == null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Bet_Not_Found,
                        Message = StatusCode.Bet_Not_Found.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Bet_Not_Found
                        }
                    };

                bet.Payout = betTransaction.Payout;
                bet.BetStatusId = betTransaction.BetStatusId;
                bet.BetStatusName = betTransaction.BetStatusName;

                var transactionRefId = string.Empty;
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    _redCatBettingContext.Update(bet);

                    var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                    {
                        BetId = bet.BetId,
                        TransactionId = request.TransactionId,
                        CLientId = bet.ClientId,
                        ClientCode = bet.ClientCode,
                        Endpoint = bet.Endpoint,
                        MemberCode = request.MemberCode,
                        OperatorId = request.OperatorId,
                        OperatorToken = bet.OperatorToken,
                        CurrencyCode = bet.CurrencyCode,
                        ExchangeRate = bet.ExchangeRate,
                        BetStatusId = bet.BetStatusId,
                        BetStatusName = bet.BetStatusName,
                        GameId = bet.GameId,
                        BetMarketId = bet.BetMarketId,
                        Amount = bet.Amount,
                        Payout = bet.Payout,
                        BetDate = bet.BetDate,
                        BetIdRef = bet.BetIdRef,
                        RollbackTransactionId = request.RollbackTransactionId,
                        Timestamp = timestamp
                    }).Entity;

                    _redCatBettingContext.SaveChanges();
                    scope.Complete();

                    transactionRefId = betAuditEntity.Id.ToString();
                }

                var operatorResponse = await _operatorRepository.ProcessBetSettlement(new RedCatOperatorBetTransactionRequest()
                {
                    BetId = bet.BetId,
                    BetIdRef = bet.BetIdRef,
                    ClientId = bet.ClientId,
                    ClientCode = bet.ClientCode,
                    Endpoint = bet.Endpoint,
                    MemberCode = request.MemberCode,
                    OperatorId = request.OperatorId,
                    OperatorToken = bet.OperatorToken,
                    CurrencyCode = bet.CurrencyCode,
                    BetMarketId = request.BetMarketId,
                    GameId = request.GameId,
                    BetStatusId = bet.BetStatusId,
                    BetStatusName = bet.BetStatusName,
                    Payout = bet.Payout,
                    Stake = bet.Amount,
                    BetDate = bet.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(timestamp).ToString(),
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result.StatusCode,
                            Balance = operatorResponse.Result.Balance,
                        }
                    };
                }

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = operatorResponse.StatusCode,
                    Message = operatorResponse.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = operatorResponse.Result.StatusCode,
                        Balance = operatorResponse.Result.Balance,
                        TransactionRefId = transactionRefId
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Error
                    }
                };
            }
        }
        private long ConvertToEpochTimestamp(DateTime date) => new DateTimeOffset(date).ToUnixTimeMilliseconds();
    }
}
