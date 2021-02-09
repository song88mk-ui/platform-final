using Microsoft.EntityFrameworkCore;
using Platform.Operator.API.Data;
using Platform.Shared.Enums;
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

        #region Authenticate
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
        #endregion

        #region WalletBalance
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
        #endregion

        #region BetPlacement
        public async Task<OperatorBetResponseDTO> BetPlacement(string operatorToken, decimal stake, string currencyCode)
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

            //validate stake
            if (stake > result.Balance)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1001 //Insuficient Fund
                };
            }

            if (result.CurrencyCode.ToLower() != currencyCode.ToLower())
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1002 //Invalid Token
                };
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    //Update wallet balance
                    result.Balance -= stake;
                    _platformContext.SaveChanges();
                    scope.Complete();

                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1003 //Internal Error
                    };
                }
            }

            return new OperatorBetResponseDTO
            {
                StatusId = 1000,
                Balance = result.Balance
            };

        }
        #endregion

        #region BetSettlement
        public async Task<OperatorBetResponseDTO> BetSettlement(string userId, decimal payout)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId))
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003 //Internal Error
                };

            var isGuid = Guid.TryParse(userId, out Guid guid);
            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003 //Internal Error
                };
            }

            var result = await _platformContext.Operator.Where(c => c.UserId == guid).FirstOrDefaultAsync();
            if (result == null)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003 //Internal Error
                };
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    //Update wallet balance
                    result.Balance += payout;
                    _platformContext.SaveChanges();
                    scope.Complete();

                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1003 //Internal Error
                    };
                }
            }

            return new OperatorBetResponseDTO
            {
                StatusId = 1000,
                Balance = result.Balance
            };

        }
        #endregion

        #region BetCancellation
        public async Task<OperatorBetResponseDTO> BetCancellation(string userId, decimal stake)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId))
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003 //Internal Error
                };

            var isGuid = Guid.TryParse(userId, out Guid guid);
            if (!isGuid)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003 //Internal Error
                };
            }

            var result = await _platformContext.Operator.Where(c => c.UserId == guid).FirstOrDefaultAsync();
            if (result == null)
            {
                return new OperatorBetResponseDTO
                {
                    StatusId = 1003 //Internal Error
                };
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    //Update wallet balance
                    result.Balance += stake;
                    _platformContext.SaveChanges();
                    scope.Complete();

                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return new OperatorBetResponseDTO
                    {
                        StatusId = 1003 //Internal Error
                    };
                }
            }

            return new OperatorBetResponseDTO
            {
                StatusId = 1000,
                Balance = result.Balance
            };

        }
        #endregion
    }
}
