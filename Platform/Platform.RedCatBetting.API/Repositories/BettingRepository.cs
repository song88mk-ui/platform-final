using Google.Protobuf.WellKnownTypes;
using Platform.Shared.Data.RedCatBetting;
using Platform.Shared.Data.RedCatBetting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System;
using System.Collections.Generic;
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

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> PlaceBet(RedCatBetRequestDTO request)
        {
            try
            {
                var timestamp = DateTime.UtcNow;

                var betAudit = _redCatBettingContext.BetAudit.FirstOrDefault(c => c.TransactionId == request.TransactionId);
                if (betAudit != null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Record_Is_Already_Existing,
                        Message = StatusCode.Record_Is_Already_Existing.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Record_Is_Already_Existing
                        }
                    };

                var bet = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString());

                if (bet != null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Record_Is_Already_Existing,
                        Message = StatusCode.Record_Is_Already_Existing.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Record_Is_Already_Existing
                        }
                    };

                var betEntity = new Bet();
                var betSelectionEntity = new BetSelection();
                var transactionRefId = string.Empty;

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var betStatus = BetStatus.Running;

                    betEntity = _redCatBettingContext.Bet.Add(new Bet()
                    {
                        ClientId = request.ClientId,
                        ClientCode = request.ClientCode.ToString(),
                        Endpoint = request.Endpoint,
                        MemberCode = request.MemberCode,
                        OperatorId = request.OperatorId,
                        OperatorToken = request.OperatorToken,
                        CurrencyCode = request.CurrencyCode.ToString(),
                        LanguageCode = request.LanguageCode.ToString(),
                        ExchangeRate = request.ExhangeRate,
                        BetStatusId = (int)betStatus,
                        BetStatusName = betStatus.GetDescription(),
                        Reason = "1024 - Ticket accepted",
                        Amount = request.TransactionAmt,
                        Payout = 0,
                        WinLoss = 0,
                        CompanyShare = 0,
                        IsResettled = false,
                        BetIdRef = request.BetId.ToString(),
                        //BetDate = request.EventDate,
                        BetDate = request.BetDate,
                        BetDateOffset = request.BetDate.BetOffset(),
                        SettlementDate = request.BetDate,
                        SettlementDateOffset = request.BetDate.BetOffset(),
                    }).Entity;

                    betSelectionEntity = _redCatBettingContext.BetSelection.Add(new BetSelection()
                    {
                        BetId = betEntity.BetId,
                        MemberCode = betEntity.MemberCode,
                        GameId = request.GameId,
                        BetMarketId = request.BetMarketId,
                        BetSelectionStatusId = (int)betStatus,
                        BetSelectionStatusName = betStatus.GetDescription(),
                        IsSettled = false,
                        IsResettled = false,
                        BetSelectionIdRef = string.Empty
                    }).Entity;

                    var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                    {
                        TransactionId = request.TransactionId,
                        BetId = betEntity.BetId,
                        ClientId = betEntity.ClientId,
                        ClientCode = betEntity.ClientCode,
                        Endpoint = betEntity.Endpoint,
                        MemberCode = betEntity.MemberCode,
                        OperatorId = betEntity.OperatorId,
                        OperatorToken = betEntity.OperatorToken,
                        CurrencyCode = betEntity.CurrencyCode,
                        LanguageCode = betEntity.LanguageCode,
                        ExchangeRate = betEntity.ExchangeRate,
                        BetStatusId = betEntity.BetStatusId,
                        BetStatusName = betEntity.BetStatusName,
                        Reason = betEntity.Reason,
                        Amount = betEntity.Amount,
                        Payout = betEntity.Payout,
                        WinLoss = betEntity.WinLoss,
                        CompanyShare = betEntity.CompanyShare,
                        IsResettled = betEntity.IsResettled,
                        BetIdRef = betEntity.BetIdRef,
                        BetDate = betEntity.BetDate,
                        BetDateOffset = betEntity.BetDateOffset,
                        SettlementDate = betEntity.SettlementDate,
                        SettlementDateOffset = betEntity.SettlementDateOffset,
                        Timestamp = timestamp
                    }).Entity;

                    var betSelectionAudit = _redCatBettingContext.BetSelectionAudit.Add(new BetSelectionAudit()
                    {
                        BetSelectionId = betSelectionEntity.BetSelectionId,
                        BetId = betSelectionEntity.BetId,
                        MemberCode = betSelectionEntity.MemberCode,
                        GameId = betSelectionEntity.GameId,
                        BetMarketId = betSelectionEntity.BetMarketId,
                        BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                        BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                        IsSettled = betSelectionEntity.IsSettled,
                        IsResettled = betSelectionEntity.IsResettled,
                        BetSelectionIdRef = betSelectionEntity.BetSelectionIdRef
                    });

                    _redCatBettingContext.SaveChanges();
                    scope.Complete();

                    transactionRefId = betAuditEntity.Id.ToString();
                }

                var operatorResponse = await _operatorRepository.ProcessBetPlacement(new RedCatOperatorBetTransactionRequestDTO()
                {
                    BetId = betEntity.BetId,
                    BetIdRef = betEntity.BetIdRef,
                    ClientId = request.ClientId,
                    ClientCode = request.ClientCode.ToString(),
                    Endpoint = request.Endpoint,
                    MemberCode = request.MemberCode,
                    OperatorId = request.OperatorId,
                    OperatorToken = request.OperatorToken,
                    CurrencyCode = betEntity.CurrencyCode,
                    BetStatusId = betEntity.BetStatusId,
                    BetStatusName = betEntity.BetStatusName,
                    Stake = betEntity.Amount,
                    IsAgent = request.IsAgent,
                    BetDate = betEntity.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(betEntity.SettlementDate).ToString(),
                    BetSelections = new List<RedCatOperatorBetSelectionRequestDTO>()
                    {
                        new RedCatOperatorBetSelectionRequestDTO()
                        {
                            BetSelectionId = betSelectionEntity.BetSelectionId.ToString(),
                            BetMarketId = betSelectionEntity.BetMarketId,
                            GameId = betSelectionEntity.GameId,
                            BetSelectionStatus = betSelectionEntity.BetSelectionStatusName
                        }
                    }
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        var betStatus = BetStatus.Rejected;

                        betEntity.BetStatusId = (int)betStatus;
                        betEntity.BetStatusName = betStatus.GetDescription();
                        betEntity.Reason = $"{operatorResponse.StatusCode} - {operatorResponse.Message}";

                        betSelectionEntity.BetSelectionStatusId = betEntity.BetStatusId;
                        betSelectionEntity.BetSelectionStatusName = betEntity.BetStatusName;

                        _redCatBettingContext.Bet.Update(betEntity);
                        _redCatBettingContext.BetSelection.Update(betSelectionEntity);

                        var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                        {
                            TransactionId = request.TransactionId,
                            BetId = betEntity.BetId,
                            ClientId = betEntity.ClientId,
                            ClientCode = betEntity.ClientCode,
                            Endpoint = betEntity.Endpoint,
                            MemberCode = betEntity.MemberCode,
                            OperatorId = betEntity.OperatorId,
                            OperatorToken = betEntity.OperatorToken,
                            CurrencyCode = betEntity.CurrencyCode,
                            LanguageCode = betEntity.LanguageCode,
                            ExchangeRate = betEntity.ExchangeRate,
                            BetStatusId = betEntity.BetStatusId,
                            BetStatusName = betEntity.BetStatusName,
                            Reason = betEntity.Reason,
                            Amount = betEntity.Amount,
                            Payout = betEntity.Payout,
                            WinLoss = betEntity.WinLoss,
                            CompanyShare = betEntity.CompanyShare,
                            IsResettled = betEntity.IsResettled,
                            BetIdRef = betEntity.BetIdRef,
                            BetDate = betEntity.BetDate,
                            BetDateOffset = betEntity.BetDateOffset,
                            SettlementDate = betEntity.SettlementDate,
                            SettlementDateOffset = betEntity.SettlementDateOffset,
                            Timestamp = timestamp
                        }).Entity;

                        var betSelectionAudit = _redCatBettingContext.BetSelectionAudit.Add(new BetSelectionAudit()
                        {
                            BetSelectionId = betSelectionEntity.BetSelectionId,
                            BetId = betSelectionEntity.BetId,
                            MemberCode = betSelectionEntity.MemberCode,
                            GameId = betSelectionEntity.GameId,
                            BetMarketId = betSelectionEntity.BetMarketId,
                            BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                            BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                            IsSettled = betSelectionEntity.IsSettled,
                            IsResettled = betSelectionEntity.IsResettled,
                            BetSelectionIdRef = betSelectionEntity.BetSelectionIdRef
                        });

                        _redCatBettingContext.SaveChanges();
                        scope.Complete();

                        transactionRefId = betAuditEntity.Id.ToString();
                    }

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result?.StatusCode ?? (int)operatorResponse.StatusCode,
                            Balance = operatorResponse.Result?.Balance ?? 0,
                            TransactionRefId = transactionRefId
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

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> SettleBet(RedCatBetRequestDTO request)
        {
            try
            {
                var betAudit = _redCatBettingContext.BetAudit.FirstOrDefault(c => c.TransactionId == request.TransactionId);
                if (betAudit != null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Record_Is_Already_Existing,
                        Message = StatusCode.Record_Is_Already_Existing.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Record_Is_Already_Existing
                        }
                    };

                var betEntity = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString() && c.ClientId == request.ClientId && c.BetStatusId < (int)BetStatus.Void);

                if (betEntity == null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Bet_Not_Found,
                        Message = StatusCode.Bet_Not_Found.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Bet_Not_Found
                        }
                    };

                var betSelections = _redCatBettingContext.BetSelection.Where(c => c.BetId == betEntity.BetId).ToList();

                var betStatus = request.TransactionAmt > 0 ? BetStatus.Won : BetStatus.Lost;
                var betStatusId = (int)betStatus;
                var betStatusName = betStatus.GetDescription();

                if (betSelections.Any())
                {
                    foreach (var selection in betSelections)
                    {
                        selection.IsSettled = true;
                        selection.BetSelectionStatusId = betStatusId;
                        selection.BetSelectionStatusName = betStatusName;
                    }
                }

                betEntity.BetStatusId = betStatusId;
                betEntity.BetStatusName = betStatusName;
                betEntity.Payout = request.TransactionAmt;
                betEntity.WinLoss = request.TransactionAmt - betEntity.Amount;
                betEntity.SettlementDate = DateTime.UtcNow;
                betEntity.SettlementDateOffset = betEntity.SettlementDate.BetOffset();

                var transactionRefId = string.Empty;
                var operatorResponse = await _operatorRepository.ProcessBetSettlement(new RedCatOperatorBetTransactionRequestDTO()
                {
                    BetId = betEntity.BetId,
                    BetIdRef = betEntity.BetIdRef,
                    ClientId = request.ClientId,
                    ClientCode = request.ClientCode.ToString(),
                    Endpoint = request.Endpoint,
                    MemberCode = request.MemberCode,
                    OperatorId = request.OperatorId,
                    OperatorToken = request.OperatorToken,
                    CurrencyCode = betEntity.CurrencyCode,
                    BetStatusId = betEntity.BetStatusId,
                    BetStatusName = betEntity.BetStatusName,
                    Stake = betEntity.Amount,
                    Payout = betEntity.Payout,
                    IsAgent = request.IsAgent,
                    BetDate = betEntity.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(betEntity.SettlementDate).ToString(),
                    BetSelections = betSelections.Select(betSelecion =>
                        new RedCatOperatorBetSelectionRequestDTO()
                        {
                            BetSelectionId = betSelecion.BetSelectionId.ToString(),
                            BetMarketId = betSelecion.BetMarketId,
                            GameId = betSelecion.GameId,
                            BetSelectionStatus = betSelecion.BetSelectionStatusName
                        }).ToList()
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    betEntity.Reason = $"{operatorResponse.StatusCode} - {operatorResponse.Message}";
                    transactionRefId = UpdateBetEntity(request.TransactionId, betEntity, betSelections);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result?.StatusCode ?? (int)operatorResponse.StatusCode,
                            Balance = operatorResponse.Result?.Balance ?? 0,
                            TransactionRefId = transactionRefId
                        }
                    };
                }

                betEntity.Reason = "1024 - Ticket accepted";
                transactionRefId = UpdateBetEntity(request.TransactionId, betEntity, betSelections);

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

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidPlacement(RedCatVoidBetRequestDTO request)
        {
            try
            {
                var transactionIdRef = string.Empty;

                var betAudit = _redCatBettingContext.BetAudit.FirstOrDefault(c => c.TransactionId == request.TransactionId);
                if (betAudit != null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Record_Is_Already_Existing,
                        Message = StatusCode.Record_Is_Already_Existing.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Record_Is_Already_Existing
                        }
                    };

                var betEntity = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString() && c.ClientId == request.ClientId && c.BetStatusId < (int)BetStatus.Void);

                if (betEntity == null || betEntity.BetStatusId != (int)BetStatus.Running)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Bet_Not_Found,
                        Message = StatusCode.Bet_Not_Found.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Bet_Not_Found
                        }
                    };

                var betSelections = _redCatBettingContext.BetSelection.Where(c => c.BetId == betEntity.BetId).ToList();

                var betStatus = BetStatus.Void;
                var betStatusId = (int)betStatus;
                var betStatusName = betStatus.GetDescription();

                if (betSelections.Any())
                {
                    foreach (var selection in betSelections)
                    {
                        selection.IsSettled = true;
                        selection.BetSelectionStatusId = betStatusId;
                        selection.BetSelectionStatusName = betStatusName;
                    }
                }

                betEntity.Payout = betEntity.Amount;
                betEntity.BetStatusId = (int)betStatus;
                betEntity.BetStatusName = betStatus.GetDescription();
                betEntity.WinLoss = 0;
                betEntity.SettlementDate = DateTime.UtcNow;
                betEntity.SettlementDateOffset = betEntity.SettlementDate.BetOffset();

                var transactionRefId = string.Empty;

                var operatorResponse = await _operatorRepository.ProcessBetSettlement(new RedCatOperatorBetTransactionRequestDTO()
                {
                    BetId = betEntity.BetId,
                    BetIdRef = betEntity.BetIdRef,
                    ClientId = request.ClientId,
                    ClientCode = request.ClientCode.ToString(),
                    Endpoint = request.Endpoint,
                    MemberCode = request.MemberCode,
                    OperatorId = request.OperatorId,
                    OperatorToken = request.OperatorToken,
                    CurrencyCode = betEntity.CurrencyCode,
                    BetStatusId = betEntity.BetStatusId,
                    BetStatusName = betEntity.BetStatusName,
                    Stake = betEntity.Amount,
                    Payout = betEntity.Payout,
                    IsAgent = request.IsAgent,
                    BetDate = betEntity.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(betEntity.SettlementDate).ToString(),
                    BetSelections = betSelections.Select(betSelecion =>
                        new RedCatOperatorBetSelectionRequestDTO()
                        {
                            BetSelectionId = betSelecion.BetSelectionId.ToString(),
                            BetMarketId = betSelecion.BetMarketId,
                            GameId = betSelecion.GameId,
                            BetSelectionStatus = betSelecion.BetSelectionStatusName
                        }).ToList()
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    betEntity.Reason = $"{operatorResponse.StatusCode} - {operatorResponse.Message}";
                    transactionRefId = UpdateBetEntity(request.TransactionId, betEntity, betSelections);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result?.StatusCode ?? (int)operatorResponse.StatusCode,
                            Balance = operatorResponse.Result?.Balance ?? 0,
                            TransactionRefId = transactionRefId
                        }
                    };
                }

                betEntity.Reason = "1024 - Ticket accepted";
                transactionRefId = UpdateBetEntity(request.TransactionId, betEntity, betSelections);

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

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettlement(RedCatVoidBetRequestDTO request)
        {
            try
            {
                var transactionIdRef = string.Empty;

                var betAudit = _redCatBettingContext.BetAudit.FirstOrDefault(c => c.TransactionId == request.TransactionId);
                if (betAudit != null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Record_Is_Already_Existing,
                        Message = StatusCode.Record_Is_Already_Existing.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Record_Is_Already_Existing
                        }
                    };

                var betEntity = _redCatBettingContext.Bet.FirstOrDefault(c => c.BetIdRef == request.BetId.ToString() && c.ClientId == request.ClientId && c.BetStatusId < (int)BetStatus.Void);

                if (betEntity == null)
                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Bet_Not_Found,
                        Message = StatusCode.Bet_Not_Found.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)StatusCode.Bet_Not_Found
                        }
                    };

                var betTransaction = _redCatBettingContext.BetAudit.FirstOrDefault(c => c.BetId == betEntity.BetId && c.TransactionId == request.RollbackTransactionId);

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

                var betSelections = _redCatBettingContext.BetSelection.Where(c => c.BetId == betEntity.BetId).ToList();

                var betStatus = BetStatus.Running;
                var betStatusId = (int)betStatus;
                var betStatusName = betStatus.GetDescription();

                if (betSelections.Any())
                {
                    foreach (var selection in betSelections)
                    {
                        selection.IsSettled = false;
                        selection.IsResettled = true;
                        selection.BetSelectionStatusId = betStatusId;
                        selection.BetSelectionStatusName = betStatusName;
                    }
                }

                betEntity.Payout = request.TransactionAmt;
                betEntity.BetStatusId = (int)betStatus;
                betEntity.BetStatusName = betStatus.GetDescription();
                betEntity.IsResettled = true;
                betEntity.WinLoss = 0;
                betEntity.SettlementDate = DateTime.UtcNow;
                betEntity.SettlementDateOffset = betEntity.SettlementDate.BetOffset();

                var transactionRefId = string.Empty;

                var operatorResponse = await _operatorRepository.ProcessBetSettlement(new RedCatOperatorBetTransactionRequestDTO()
                {
                    BetId = betEntity.BetId,
                    BetIdRef = betEntity.BetIdRef,
                    ClientId = request.ClientId,
                    ClientCode = request.ClientCode.ToString(),
                    Endpoint = request.Endpoint,
                    MemberCode = request.MemberCode,
                    OperatorId = request.OperatorId,
                    OperatorToken = request.OperatorToken,
                    CurrencyCode = betEntity.CurrencyCode,
                    BetStatusId = betEntity.BetStatusId,
                    BetStatusName = betEntity.BetStatusName,
                    Stake = betEntity.Amount,
                    Payout = betEntity.Payout,
                    IsAgent = request.IsAgent,
                    BetDate = betEntity.BetDate,
                    CorrelationId = ConvertToEpochTimestamp(betEntity.SettlementDate).ToString(),
                    BetSelections = betSelections.Select(betSelecion =>
                        new RedCatOperatorBetSelectionRequestDTO()
                        {
                            BetSelectionId = betSelecion.BetSelectionId.ToString(),
                            BetMarketId = betSelecion.BetMarketId,
                            GameId = betSelecion.GameId,
                            BetSelectionStatus = betSelecion.BetSelectionStatusName
                        }).ToList()
                });

                if (operatorResponse.StatusCode != StatusCode.Success)
                {
                    betEntity.Reason = $"{operatorResponse.StatusCode} - {operatorResponse.Message}";
                    transactionRefId = UpdateBetEntity(request.TransactionId, betEntity, betSelections, betTransaction.TransactionId);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = operatorResponse.StatusCode,
                        Message = operatorResponse.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = operatorResponse.Result?.StatusCode ?? (int)operatorResponse.StatusCode,
                            Balance = operatorResponse.Result?.Balance ?? 0,
                            TransactionRefId = transactionRefId
                        }
                    };
                }

                betEntity.Reason = "1024 - Ticket accepted";
                transactionRefId = UpdateBetEntity(request.TransactionId, betEntity, betSelections, betTransaction.TransactionId);

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

        private string UpdateBetEntity(long transactionId, Bet betEntity, List<BetSelection> betSelections, long? rollbackTransactionId = null)
        {
            var transactionRefId = string.Empty;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                _redCatBettingContext.Bet.Update(betEntity);

                var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
                {
                    TransactionId = transactionId,
                    BetId = betEntity.BetId,
                    ClientId = betEntity.ClientId,
                    ClientCode = betEntity.ClientCode,
                    Endpoint = betEntity.Endpoint,
                    MemberCode = betEntity.MemberCode,
                    OperatorId = betEntity.OperatorId,
                    OperatorToken = betEntity.OperatorToken,
                    CurrencyCode = betEntity.CurrencyCode,
                    LanguageCode = betEntity.LanguageCode,
                    ExchangeRate = betEntity.ExchangeRate,
                    BetStatusId = betEntity.BetStatusId,
                    BetStatusName = betEntity.BetStatusName,
                    Reason = betEntity.Reason,
                    Amount = betEntity.Amount,
                    Payout = betEntity.Payout,
                    WinLoss = betEntity.WinLoss,
                    CompanyShare = betEntity.CompanyShare,
                    IsResettled = betEntity.IsResettled,
                    BetIdRef = betEntity.BetIdRef,
                    BetDate = betEntity.BetDate,
                    BetDateOffset = betEntity.BetDateOffset,
                    SettlementDate = betEntity.SettlementDate,
                    SettlementDateOffset = betEntity.SettlementDateOffset,
                    RollbackTransactionId = rollbackTransactionId,
                    Timestamp = DateTime.UtcNow
                }).Entity;

                if (betSelections.Any())
                    foreach (var betSelectionEntity in betSelections)
                    {
                        _redCatBettingContext.BetSelection.Update(betSelectionEntity);

                        var betSelectionAudit = _redCatBettingContext.BetSelectionAudit.Add(new BetSelectionAudit()
                        {
                            BetSelectionId = betSelectionEntity.BetSelectionId,
                            BetId = betSelectionEntity.BetId,
                            MemberCode = betSelectionEntity.MemberCode,
                            GameId = betSelectionEntity.GameId,
                            BetMarketId = betSelectionEntity.BetMarketId,
                            BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                            BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                            IsSettled = betSelectionEntity.IsSettled,
                            IsResettled = betSelectionEntity.IsResettled,
                            BetSelectionIdRef = betSelectionEntity.BetSelectionIdRef
                        });
                    }

                _redCatBettingContext.SaveChanges();
                scope.Complete();

                transactionRefId = betAuditEntity.Id.ToString();
            }

            return transactionRefId;
        }
    }
}
