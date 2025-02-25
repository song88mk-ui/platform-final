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

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var betEntity = _redCatBettingContext.Bet.Add(new Bet()
                    {
                        ClientId = request.ClientId,
                        ClientCode = request.ClientCode,
                        Endpoint = request.Endpoint,
                        OperatorToken = request.OperatorToken,
                        CurrencyCode = request.Currency,
                        BetStatusId = (int)BetStatus.Running,
                        BetStatusName = BetStatus.Running.GetDescription(),
                        GameId = request.GameId,
                        BetMarketId = request.BetMarketId,
                        Stake = request.TransactionAmt,
                        BetDate = request.EventDate,
                        BetIdRef = request.BetId.ToString()
                    }).Entity;

                    var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                    {
                        TransactionId = request.TransactionId,
                        BetId = betEntity.BetId,
                        CLientId = betEntity.ClientId,
                        ClientCode = betEntity.ClientCode,
                        Endpoint = betEntity.Endpoint,
                        OperatorToken = betEntity.OperatorToken,
                        CurrencyCode = betEntity.CurrencyCode,
                        BetStatusId = betEntity.BetStatusId,
                        BetStatusName = betEntity.BetStatusName,
                        GameId = betEntity.GameId,
                        BetMarketId = betEntity.BetMarketId,
                        Stake = betEntity.Stake,
                        BetDate = betEntity.BetDate,
                        BetIdRef = betEntity.BetIdRef,
                        Timestamp = timestamp
                    }).Entity;

                    var operatorResponse = await _operatorRepository.ProcessBetPlacement(new RedCatOperatorBetTransactionRequest()
                    {
                        BetId = betEntity.BetId,
                        BetIdRef = betEntity.BetIdRef,
                        ClientId = request.ClientId,
                        ClientCode = request.ClientCode,
                        OperatorToken = request.OperatorToken,
                        Endpoint = request.Endpoint,
                        CurrencyCode = betEntity.CurrencyCode,
                        BetMarketId = request.BetMarketId,
                        GameId = request.GameId,
                        BetStatusId = betEntity.BetStatusId,
                        BetStatusName = betEntity.BetStatusName,
                        Stake = betEntity.Stake,
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
                                StatusCode = operatorResponse.Result.StatusCode,
                                Balance = operatorResponse.Result.Balance,
                            }
                        };
                    }

                    _redCatBettingContext.SaveChanges();
                    scope.Complete();

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result.StatusCode,
                            Balance = operatorResponse.Result.Balance,
                            TransactionRefId = betAuditEntity.Id.ToString()
                        }
                    };
                }
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

                var betStatus = request.TransactionAmt > 0 ? BetStatus.Won : BetStatus.Lost;

                bet.BetStatusId = (int)betStatus;
                bet.BetStatusName = betStatus.GetDescription();

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var operatorResponse = await _operatorRepository.ProcessBetSettlement(new RedCatOperatorBetTransactionRequest()
                    {
                        BetId = bet.BetId,
                        BetIdRef = bet.BetIdRef,
                        ClientId = request.ClientId,
                        ClientCode = request.ClientCode,
                        OperatorToken = request.OperatorToken,
                        Endpoint = request.Endpoint,
                        CurrencyCode = bet.CurrencyCode,
                        BetMarketId = request.BetMarketId,
                        GameId = request.GameId,
                        BetStatusId = bet.BetStatusId,
                        BetStatusName = bet.BetStatusName,
                        Stake = bet.Stake,
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

                    _redCatBettingContext.Update(bet);

                    var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                    {
                        TransactionId = request.TransactionId,
                        BetId = bet.BetId,
                        CLientId = bet.ClientId,
                        ClientCode = bet.ClientCode,
                        Endpoint = bet.Endpoint,
                        OperatorToken = bet.OperatorToken,
                        CurrencyCode = bet.CurrencyCode,
                        BetStatusId = bet.BetStatusId,
                        BetStatusName = bet.BetStatusName,
                        GameId = bet.GameId,
                        BetMarketId = bet.BetMarketId,
                        Stake = bet.Stake,
                        BetDate = bet.BetDate,
                        BetIdRef = bet.BetIdRef,
                        Timestamp = timestamp
                    }).Entity;

                    _redCatBettingContext.SaveChanges();
                    scope.Complete();

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result.StatusCode,
                            Balance = operatorResponse.Result.Balance,
                            TransactionRefId = betAuditEntity.Id.ToString()
                        }
                    };
                }
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
