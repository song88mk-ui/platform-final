using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Platform;
using Platform.Shared.Models;
using System;
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
                    CurrencyCode = result.CurrencyCode
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

                    result.Balance -= request.Stake;
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
    }
}
