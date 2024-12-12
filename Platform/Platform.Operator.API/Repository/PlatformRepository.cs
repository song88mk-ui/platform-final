using Microsoft.EntityFrameworkCore;
using Platform.Operator.API.Models;
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
        public async Task<CashoutResultResponseDTO> CashOutResult(CashoutResultRequestDTO request)
        {
            try
            {
                return new CashoutResultResponseDTO
                {
                    StatusId = 1000
                };
            }
            catch (Exception ex)
            {
                return new CashoutResultResponseDTO
                {
                    StatusId = 1003
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

        public async Task<OGWalletResponseDTO> WalletBalanceOG(string userId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId))
                return new OGWalletResponseDTO
                {
                    Code = "S-101" //Internal Error
                };

            var result = await _platformContext.Operator.Where(c => c.UserId.ToString().ToLower() == userId.ToLower()).FirstOrDefaultAsync();

            if (result == null)
            {
                return new OGWalletResponseDTO
                {
                    Code = "S-102" //Invalid Token
                };
            }

            return new OGWalletResponseDTO
            {
                Code = "S-100",
                Balance = result.Balance.ToString(),
                CurrencyCode = result.CurrencyCode,
                PlayerCode = userId
            };

        }

        public async Task<OperatorBetResponseDTO> BetPlacement(OperatorBetRequestDTO request, string operatorToken, string clientId)
        {
            //
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1002
                };
            }

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

                    var testConfig = _platformContext.TestConfig.Find(id);


                    if (request.IsBetBuilder == false && testConfig != null && testConfig.ClientId == id)
                    {
                        
                        if (request.IsBetBuilder == true && (testConfig.StakeBonusPercent > 0 || testConfig.IsFreeBet))
                        {
                            return new OperatorBetResponseDTO
                            {
                                StatusId = 1003,
                                Balance = result.Balance,
                                IsFreeBet = testConfig.IsFreeBet,
                                StakeReal = (request.Stake * (testConfig.StakeRealPercent / 100)),
                                StakeBonus = (request.Stake * (testConfig.StakeBonusPercent / 100))
                            };
                        }

                        if (request.IsBoosted == true && (testConfig.StakeBonusPercent > 0 || testConfig.IsFreeBet))
                        {
                            return new OperatorBetResponseDTO
                            {
                                StatusId = 1003,
                                Balance = result.Balance,
                                IsFreeBet = testConfig.IsFreeBet,
                                StakeReal = (request.Stake * (testConfig.StakeRealPercent / 100)),
                                StakeBonus = (request.Stake * (testConfig.StakeBonusPercent / 100))
                            };
                        }

                        var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, 0, request.Stake, request.BetStatus, testConfig.IsFreeBet);

                        result.Balance = operatorAudit.PostBalance;
                        _platformContext.Operator.Update(result);

                        _platformContext.SaveChanges();

                        scope.Complete();

                        return new OperatorBetResponseDTO
                        {
                            StatusId = 1000,
                            Balance = result.Balance,
                            IsFreeBet = testConfig.IsFreeBet,
                            StakeReal = (request.Stake * (testConfig.StakeRealPercent / 100)),
                            StakeBonus = (request.Stake * (testConfig.StakeBonusPercent / 100))
                        };
                    }
                    else
                    {

                        var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, 0, request.Stake, request.BetStatus);

                        result.Balance = operatorAudit.PostBalance;
                        _platformContext.Operator.Update(result);

                        _platformContext.SaveChanges();

                        scope.Complete();

                        return new OperatorBetResponseDTO
                        {
                            StatusId = 1000,
                            Balance = result.Balance,
                            IsFreeBet = false,
                            StakeReal = request.Stake,
                            StakeBonus = 0m
                        };
                    }

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

        public async Task<OperatorBetResponseDTO> BetFullCashOut(OperatorBetSettlementRequestDTO request, string clientId)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

            var isGuid = Guid.TryParse(request.UserId, out Guid guid);

            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

            if (request.IsBetBuilder)
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

                    var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, request.Payout, request.Stake, request.BetStatus);

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

        public async Task<OperatorBetResponseDTO> BetPartialCashOut(OperatorBetSettlementRequestDTO request, string clientId)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

            var isGuid = Guid.TryParse(request.UserId, out Guid guid);

            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

            if (request.IsBetBuilder)
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

                    var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, request.Payout, request.Stake, request.BetStatus, false, true);

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

        public async Task<OperatorBetResponseDTO> BetSettlement(OperatorBetSettlementRequestDTO request, string clientId)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

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

                    var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, request.Payout, request.Stake, request.BetStatus);

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
        public async Task<OperatorBetResponseDTO> BetCancellation(OperatorBetCancellationRequestDTO request, string clientId)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003
                };
            }

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

                    var operatorAudit = AddOrUpdateOperatorAudit(result, request.BetId, request.Payout, request.Stake, request.BetStatus);

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

        public async Task<FixtureScoreResponseDTO> FixtureScore(FixtureScoreRequestDTO request)
        {
            try
            {
                return new FixtureScoreResponseDTO
                {
                    StatusId = 1000
                };
            }
            catch (Exception ex)
            {
                return new FixtureScoreResponseDTO
                {
                    StatusId = 1003
                };
            }
        }

        public async Task<BaseResponseDTO<GenerateUserResponseDTO>> AddUser(GenerateUserRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.CurrencyCode) || !Enum.IsDefined(typeof(CurrencyCode), request.CurrencyCode))
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
        public async Task<BetPlacementResponseDTO> BetPlacementCasino(OperatorCasinoBetPlacementRequestDTO request, string clientId, string operatorToken)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new BetPlacementResponseDTO
                {
                    StatusId = 1002
                };
            }

            var isGuid = Guid.TryParse(operatorToken, out Guid guid);

            if (!isGuid)
            {
                return new BetPlacementResponseDTO
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
                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1002
                        };
                    }

                    if (result.Balance < request.Stake)
                    {
                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1001
                        };
                    }

                    var testConfig = _platformContext.TestConfig.Find(id);

                    var betId = request.BetId.Split('|').FirstOrDefault();

                    if (testConfig != null && testConfig.ClientId == id)
                    {

                        var operatorAudit = AddOrUpdateOperatorAudit(result, betId, 0, request.Stake, BetStatus.Running.ToString(), testConfig.IsFreeBet);

                        result.Balance = operatorAudit.PostBalance;
                        _platformContext.Operator.Update(result);

                        _platformContext.SaveChanges();

                        scope.Complete();

                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1000,
                            Balance = result.Balance,
                            IsFreeBet = testConfig.IsFreeBet,
                            StakeReal = (request.Stake * (testConfig.StakeRealPercent / 100)),
                            StakeBonus = (request.Stake * (testConfig.StakeBonusPercent / 100))
                        };
                    }
                    else
                    {

                        var operatorAudit = AddOrUpdateOperatorAudit(result, betId, 0, request.Stake, BetStatus.Running.ToString());

                        result.Balance = operatorAudit.PostBalance;
                        _platformContext.Operator.Update(result);

                        _platformContext.SaveChanges();

                        scope.Complete();

                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1000,
                            Balance = result.Balance,
                            IsFreeBet = false,
                            StakeReal = request.Stake,
                            StakeBonus = 0m
                        };
                    }

                }
                catch (Exception ex)
                {
                    return new BetPlacementResponseDTO
                    {
                        StatusId = 1003
                    };
                }
            }
            
        }
        public async Task<BetSettlementResponseDTO> BetSettlementCasino(OperatorCasinoBetSettlementRequestDTO request, string clientId)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new BetSettlementResponseDTO
                {
                    StatusId = 1003
                };
            }

            var isGuid = Guid.TryParse(request.UserId, out Guid guid);

            if (!isGuid)
            {
                return new BetSettlementResponseDTO
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
                        return new BetSettlementResponseDTO
                        {
                            StatusId = 1003
                        };
                    }

                    var betId = request.BetId.Split('|').FirstOrDefault();

                    var operatorAudit = AddOrUpdateOperatorAudit(result, betId, request.Payout, request.Stake, request.BetStatus);

                    result.Balance = operatorAudit.PostBalance;
                    _platformContext.Operator.Update(result);

                    _platformContext.SaveChanges();
                    scope.Complete();

                    return new BetSettlementResponseDTO
                    {
                        StatusId = 1000,
                        Balance = result.Balance
                    };
                }
                catch (Exception ex)
                {
                    return new BetSettlementResponseDTO
                    {
                        StatusId = 1003
                    };
                }
            }
        }
        public async Task<BetPlacementResponseDTO> DebitESports(OperatorESportsDebitRequestDTO request, string clientId, string operatorToken)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new BetPlacementResponseDTO
                {
                    StatusId = 1002
                };
            }

            var isGuid = Guid.TryParse(operatorToken, out Guid guid);

            if (!isGuid)
            {
                return new BetPlacementResponseDTO
                {
                    StatusId = 1002
                };
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    // idempotency check
                    if (!string.IsNullOrEmpty(request.TransactionId) && _platformContext.OperatorAudit.FirstOrDefault(c => c.TransactionId == request.TransactionId) != null)
                    {
                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1000
                        };
                    }

                    var result = _platformContext.Operator.Where(c => c.OperatorToken == guid).FirstOrDefault();

                    if (result == null)
                    {
                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1002
                        };
                    }

                    if (result.Balance < request.Amount)
                    {
                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1001
                        };
                    }

                    var testConfig = _platformContext.TestConfig.Find(id);

                    if (testConfig != null && testConfig.ClientId == id)
                    {

                        var operatorAudit = AddOrUpdateOperatorAuditForESports(result, request.BetId, 0, request.Amount, BetStatus.Running.ToString(), testConfig.IsFreeBet, request.TransactionId);

                        result.Balance = operatorAudit.PostBalance;
                        _platformContext.Operator.Update(result);

                        _platformContext.SaveChanges();

                        scope.Complete();

                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1000,
                            Balance = result.Balance,
                            IsFreeBet = testConfig.IsFreeBet,
                            StakeReal = (request.Amount * (testConfig.StakeRealPercent / 100)),
                            StakeBonus = (request.Amount * (testConfig.StakeBonusPercent / 100))
                        };
                    }
                    else
                    {
                        var operatorAudit = AddOrUpdateOperatorAuditForESports(result, request.BetId, 0, request.Amount, BetStatus.Running.ToString(), transactionId: request.TransactionId);

                        result.Balance = operatorAudit.PostBalance;
                        _platformContext.Operator.Update(result);

                        _platformContext.SaveChanges();

                        scope.Complete();

                        return new BetPlacementResponseDTO
                        {
                            StatusId = 1000,
                            Balance = result.Balance,
                            IsFreeBet = false,
                            StakeReal = request.Amount,
                            StakeBonus = 0m
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new BetPlacementResponseDTO
                    {
                        StatusId = 1003
                    };
                }
            }

        }
        public async Task<BetSettlementResponseDTO> CreditESports(OperatorESportsCreditRequestDTO request, string clientId)
        {
            Guid.TryParse(clientId, out Guid id);

            var res = _platformContext.NegativeTest.Find(id);

            if (res != null && res.ClientId == id)
            {
                return new BetSettlementResponseDTO
                {
                    StatusId = 1003
                };
            }

            var isGuid = Guid.TryParse(request.UserId, out Guid guid);

            if (!isGuid)
            {
                return new BetSettlementResponseDTO
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
                        return new BetSettlementResponseDTO
                        {
                            StatusId = 1003
                        };
                    }

                    var operatorAudit = AddOrUpdateOperatorAuditForESports(result, request.BetId, request.Amount, 0, request.BetStatus, transactionId: request.TransactionId);

                    result.Balance = operatorAudit.PostBalance;
                    _platformContext.Operator.Update(result);

                    _platformContext.SaveChanges();
                    scope.Complete();

                    return new BetSettlementResponseDTO
                    {
                        StatusId = 1000,
                        Balance = result.Balance
                    };
                }
                catch (Exception ex)
                {
                    return new BetSettlementResponseDTO
                    {
                        StatusId = 1003
                    };
                }
            }
        }
        #region Private Method

        private OperatorAudit AddOrUpdateOperatorAudit(Shared.Data.Platform.Entities.Operator _operator, string betId, decimal payout, decimal stake, string betStatus, bool isFreebet = false, bool isPartialCashout = false)
        {
            var amount = payout;
            if ((int)Enum.Parse(typeof(BetStatus), betStatus) == (int)BetStatus.Running && isPartialCashout == false)
            {
                if (isFreebet)
                    amount = 0;
                else
                    amount = stake * -1;
            } 

            var postBalance = _operator.Balance;
            var prevOperatorAudit = _platformContext.OperatorAudit.AsNoTracking().FirstOrDefault(o => o.UserId == _operator.UserId && o.BetId == Guid.Parse(betId));

            if (prevOperatorAudit != null)
            {
                if (prevOperatorAudit.TransactionTypeId != (int)Enum.Parse(typeof(BetStatus), betStatus))
                {
                    switch (prevOperatorAudit.TransactionTypeId)
                    {
                        case (int)BetStatus.CashedOut:
                        case (int)BetStatus.Void:
                        case (int)BetStatus.Lost:
                        case (int)BetStatus.Won:
                        case (int)BetStatus.HalfLost:
                        case (int)BetStatus.HalfWon:

                            var hasVoid = _platformContext.OperatorAudit.AsNoTracking().Any(o => o.UserId == _operator.UserId && o.BetId == Guid.Parse(betId) && o.TransactionTypeId == (int)BetStatus.Void);
                            if ((int)Enum.Parse(typeof(BetStatus), betStatus) != (int)BetStatus.Void && prevOperatorAudit.TransactionTypeId != (int)BetStatus.Void)
                            {
                                postBalance += (prevOperatorAudit.Amount * -1);

                                if (hasVoid)
                                    amount = payout - stake;

                            }
                            else if ((int)Enum.Parse(typeof(BetStatus), betStatus) == (int)BetStatus.Void)
                            {
                                if (hasVoid)
                                    amount = (payout - stake) + (prevOperatorAudit.Amount * -1);
                                else
                                    amount = payout + (prevOperatorAudit.Amount * -1);
                            }
                            else
                            {
                                amount = payout - stake;
                            }
                            break;
                        default:
                            break;
                    }
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
                PreBalance = postBalance,
                Amount = amount,
                PostBalance = postBalance + amount,
                TransactionId = string.Empty,
            }).Entity;

            _platformContext.SaveChanges();

            return operatorAuditEntity;
        }

        private OperatorAudit AddOrUpdateOperatorAuditForESports(Shared.Data.Platform.Entities.Operator _operator, string betId, decimal payout, decimal stake, string betStatus, bool isFreebet = false, string transactionId = null, bool isPartialCashout = false)
        {
            var amount = payout;

            if ((int)Enum.Parse(typeof(BetStatus), betStatus) == (int)BetStatus.Running && isPartialCashout == false)
            {
                amount = stake * -1;
            }

            var postBalance = _operator.Balance;

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
                PreBalance = postBalance,
                Amount = amount,
                PostBalance = postBalance + amount,
                TransactionId = !string.IsNullOrEmpty(transactionId) ? transactionId : string.Empty,
            }).Entity;

            _platformContext.SaveChanges();

            return operatorAuditEntity;
        }

        #endregion

        public bool AddClient(string clientId)
        {
            var isValid = Guid.TryParse(clientId, out Guid id);

            if (isValid)
            {
                var result = _platformContext.NegativeTest.Find(id);

                if (result == null)
                {
                    _platformContext.NegativeTest.Add(new NegativeTest { ClientId = id });
                    _platformContext.SaveChanges();
                }

                return true;
            }
            else
                return false;
        }

        public bool RemoveClient(string clientId)
        {
            var isValid = Guid.TryParse(clientId, out Guid id);

            if (isValid)
            {
                var result = _platformContext.NegativeTest.Find(id);

                if (result != null)
                {
                    _platformContext.NegativeTest.Remove(result);
                    _platformContext.SaveChanges();
                }

                return true;
            }
            else
                return false;
        }

        public List<string> GetClients()
        {

            var result = _platformContext.NegativeTest.ToList();

            if (result.Count == 0)
                return new List<string>();
            else
                return result.Select(s => s.ClientId.ToString()).ToList();
        }

        public bool AddTestConfig(string clientId, bool isFreeBet, decimal stakeRealPercent, decimal stakeBonusPercent)
        {
            var isValid = Guid.TryParse(clientId, out Guid id);

            if (isValid)
            {
                var result = _platformContext.TestConfig.Find(id);

                if (result == null)
                {
                    _platformContext.TestConfig.Add(new TestConfig
                    {
                        ClientId = id,
                        IsFreeBet = isFreeBet,
                        StakeRealPercent = stakeRealPercent,
                        StakeBonusPercent = stakeBonusPercent
                    });
                    _platformContext.SaveChanges();
                }

                return true;
            }
            else
                return false;
        }

        public bool RemoveTestConfig(string clientId)
        {
            var isValid = Guid.TryParse(clientId, out Guid id);

            if (isValid)
            {
                var result = _platformContext.TestConfig.Find(id);

                if (result != null)
                {
                    _platformContext.TestConfig.Remove(result);
                    _platformContext.SaveChanges();
                }

                return true;
            }
            else
                return false;
        }

        public List<TestConfig> GetTestConfig()
        {

            var result = _platformContext.TestConfig.ToList();

            if (result.Count == 0)
                return new List<TestConfig>();
            else
                return result.ToList();
        }
    }
}
