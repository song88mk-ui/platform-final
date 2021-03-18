using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.Operator.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly PlatformContext _platformContext;

        public PlatformRepository(PlatformContext platformContext)
        {
            _platformContext = platformContext;
        }
        public async Task<AuthenticationResponseDTO> Authenticate(string operatorToken)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken)) return null;

            var isGuid = Guid.TryParse(operatorToken, out Guid guid);

            if (!isGuid)
            {
                return new AuthenticationResponseDTO
                {
                    StatusId = 1002
                };
            }

            var result = await _platformContext.Operator.Where(c => c.OperatorToken == guid).FirstOrDefaultAsync();

            if (result == null)
            {
                return new AuthenticationResponseDTO
                {
                    StatusId = 1002
                };
            }
            else
            {
                return new AuthenticationResponseDTO
                {
                    UserId = result.UserId.ToString().ToUpper(),
                    StatusId = 1000,
                    Balance = result.Balance,
                    CurrencyCode = result.CurrencyCode,
                    LanguageCode = "EN"
                };
            }
        }
        public async Task<OperatorBetResponseDTO> WalletBalance(string operatorToken)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken))
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003 //Internal Error
                };

            var isGuid = Guid.TryParse(operatorToken, out Guid guid);

            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1002 //Invalid Token
                };
            }

            var result = await _platformContext.Operator.Where(c => c.OperatorToken == guid).FirstOrDefaultAsync();

            if (result == null)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1002 //Invalid Token
                };
            }

            return new OperatorBetResponseDTO
            {
                StatusId = 1000,
                Balance = result.Balance
            };

        }
        public async Task<OperatorBetResponseDTO> BetPlacement(OperatorBetRequestDTO request, string operatorToken)
        {
            var isGuid = Guid.TryParse(operatorToken, out Guid guid);

            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1002
                };
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    var result = _platformContext.Operator.Where(c => c.OperatorToken == guid).FirstOrDefault();

                    if (result == null)
                    {
                        return new OperatorBetResponseDTO
                        {
                            StatusId = 1002
                        };
                    }

                    if (result.Balance < request.Stake)
                    {
                        return new OperatorBetResponseDTO
                        {
                            StatusId = 1001
                        };
                    }

                    var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, request.Stake, request.BetStatus);

                    result.Balance = operatorAudit.PostBalance;
                    _platformContext.Operator.Update(result);

                    _platformContext.SaveChanges();
                    scope.Complete();

                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1000,
                        Balance = result.Balance
                    };
                }
                catch (Exception ex)
                {
                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1003
                    };
                }
            }
        }

        public async Task<OperatorBetResponseDTO> BetSettlement(OperatorBetSettlementRequestDTO request)
        {
            var isGuid = Guid.TryParse(request.UserId, out Guid guid);

            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    var result = _platformContext.Operator.Where(c => c.UserId == guid).FirstOrDefault();

                    if (result == null)
                    {
                        return new OperatorBetResponseDTO
                        {
                            StatusId = 1003
                        };
                    }

                    var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, request.Payout, request.BetStatus);

                    result.Balance = operatorAudit.PostBalance;
                    _platformContext.Operator.Update(result);

                    _platformContext.SaveChanges();
                    scope.Complete();

                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1000,
                        Balance = result.Balance
                    };
                }
                catch (Exception ex)
                {
                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1003
                    };
                }
            }
        }
        public async Task<OperatorBetResponseDTO> BetCancellation(OperatorBetCancellationRequestDTO request)
        {
            var isGuid = Guid.TryParse(request.UserId, out Guid guid);

            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    var result = _platformContext.Operator.Where(c => c.UserId == guid).FirstOrDefault();

                    if (result == null)
                    {
                        return new OperatorBetResponseDTO
                        {
                            StatusId = 1003
                        };
                    }

                    result.Balance += request.Payout;
                    _platformContext.Operator.Update(result);

                    _platformContext.SaveChanges();
                    scope.Complete();

                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1000,
                        Balance = result.Balance
                    };
                }
                catch (Exception ex)
                {
                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1003
                    };
                }
            }
        }

        public async Task<BaseResponseDTO<GenerateUserResponseDTO>> AddUser(GenerateUserRequestDTO request)
        {
            if(string.IsNullOrEmpty(request.CurrencyCode) || !Enum.IsDefined(typeof(CurrencyCode), request.CurrencyCode))
                return new BaseResponseDTO<GenerateUserResponseDTO>
                {
                    StatusCode = StatusCode.Unsupported_CurrencyCode,
                    Message = StatusCode.Unsupported_CurrencyCode.ToString()
                };

            GenerateUserResponseDTO response = new GenerateUserResponseDTO();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var operatorEntity = _platformContext.Operator.Update(new Shared.Data.Platform.Entities.Operator
                    {
                        ClientId = request.ClientId,
                        Balance = request.Balance,
                        CurrencyCode = request.CurrencyCode,
                        OperatorToken = Guid.NewGuid(),
                        CreatedDate = DateTime.UtcNow
                    }).Entity;

                    response.ClientId = request.ClientId;
                    response.CurrencyCode = request.CurrencyCode;
                    response.UserId = operatorEntity.UserId;
                    response.OperatorToken = operatorEntity.OperatorToken;
                    response.Balance = operatorEntity.Balance;

                    await _platformContext.SaveChangesAsync();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return new BaseResponseDTO<GenerateUserResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };
                }
            }
            return new BaseResponseDTO<GenerateUserResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = response
            };
        }

        #region Private Method
        private OperatorAudit AddOrUpdateOperatorAudit(Shared.Data.Platform.Entities.Operator _operator, string betId, decimal amount, string betStatus)
        {
            if ((int)Enum.Parse(typeof(BetStatus), betStatus) == (int)BetStatus.Running)
                amount *= -1;

            var prevOperatorAudit = _platformContext.OperatorAudit.AsNoTracking().Where(o => o.UserId == _operator.UserId && o.BetId == Guid.Parse(betId)).FirstOrDefault();
            if (prevOperatorAudit != null && (int)Enum.Parse(typeof(BetStatus), betStatus) != (int)BetStatus.Void)
            {
                switch (prevOperatorAudit.TransactionTypeId)
                {
                    case (int)BetStatus.Void:
                    case (int)BetStatus.Lost:
                    case (int)BetStatus.Won:
                    case (int)BetStatus.HalfLost:
                    case (int)BetStatus.HalfWon:
                        amount += (prevOperatorAudit.Amount * -1);
                        break;
                    default:
                        break;
                }
            }
            
            
            var operatorAuditEntity = _platformContext.OperatorAudit.Update(new OperatorAudit()
            {
                BetId = Guid.Parse(betId),
                UserId = _operator.UserId,
                ClientId = _operator.ClientId,
                OperatorToken = _operator.OperatorToken,
                CreatedDate = DateTime.UtcNow,
                CurrencyCode = _operator.CurrencyCode,
                TransactionTypeId = (int)Enum.Parse(typeof(BetStatus), betStatus),
                TransactionTypeName = betStatus,
                PreBalance = prevOperatorAudit == null ? _operator.Balance : prevOperatorAudit.PostBalance,
                Amount = amount,
                PostBalance = prevOperatorAudit == null ? _operator.Balance + amount : prevOperatorAudit.PostBalance + amount,
            }).Entity;

            _platformContext.SaveChanges();

            return operatorAuditEntity;
        }

        #endregion

    }
}
