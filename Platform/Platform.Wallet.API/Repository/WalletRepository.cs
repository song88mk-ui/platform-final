using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Platform.Shared.Models;
using Platform.Wallet.API.Data;
using Platform.Wallet.API.Models;
using System;
using System.Threading.Tasks;

namespace Platform.Wallet.API.Repository
{
    public class WalletRepository : IWalletRepository
    {
        private readonly WalletContext _walletContext;
        
        public WalletRepository(WalletContext walletContext)
        {
            _walletContext = walletContext;
        }

        public async Task<Tuple<int, string, decimal>> CreditUserWallet(CreditOrDebitRequestDTO request)
        {
            var param = new SqlParameter[]
            {
                new SqlParameter
                {
                    ParameterName = "@ClientId",
                    SqlDbType = System.Data.SqlDbType.UniqueIdentifier,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ClientId
                },
                new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.UserId
                },
                new SqlParameter
                {
                    ParameterName = "@Amount",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.Amount
                },
                new SqlParameter
                {
                    ParameterName = "@CurrencyCode",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.CurrencyCode
                },
                new SqlParameter
                {
                    ParameterName = "@Direction",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = 1
                },
                new SqlParameter
                {
                    ParameterName = "@TransactionTypeId",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.TransactionTypeId
                },
                new SqlParameter
                {
                    ParameterName = "@TransactionTypeName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.TransactionTypeName
                },
                new SqlParameter
                {
                    ParameterName = "@ReferenceId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 100,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ReferenceId
                },
                new SqlParameter
                {
                    ParameterName = "@StatusCode",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@StatusName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@Balance",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Output,
                }
            };

            await _walletContext.Database.ExecuteSqlRawAsync($"EXEC CreditOrDebitUserWallet @ClientId, @UserId, @Amount, @CurrencyCode, @Direction, @TransactionTypeId, @TransactionTypeName, @ReferenceId, @StatusCode OUTPUT, @StatusName OUTPUT, @Balance OUTPUT", param);

            var statusCode = (int)param[8].Value;
            var statusName = (string)param[9].Value;
            var balance = (decimal)param[10].Value;

            return new Tuple<int, string, decimal>(statusCode, statusName, balance);
        }
        public async Task<Tuple<int, string, decimal>> DebitUserWallet(CreditOrDebitRequestDTO request)
        {
            var param = new SqlParameter[]
            {
                new SqlParameter
                {
                    ParameterName = "@ClientId",
                    SqlDbType = System.Data.SqlDbType.UniqueIdentifier,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ClientId
                },
                new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.UserId
                },
                new SqlParameter
                {
                    ParameterName = "@Amount",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.Amount
                },
                new SqlParameter
                {
                    ParameterName = "@CurrencyCode",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.CurrencyCode
                },
                new SqlParameter
                {
                    ParameterName = "@Direction",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = 0
                },
                new SqlParameter
                {
                    ParameterName = "@TransactionTypeId",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.TransactionTypeId
                },
                new SqlParameter
                {
                    ParameterName = "@TransactionTypeName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.TransactionTypeName
                },
                new SqlParameter
                {
                    ParameterName = "@ReferenceId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 100,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ReferenceId
                },
                new SqlParameter
                {
                    ParameterName = "@StatusCode",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@StatusName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@Balance",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Output,
                }
            };

            await _walletContext.Database.ExecuteSqlRawAsync($"EXEC CreditOrDebitUserWallet @ClientId, @UserId, @Amount, @CurrencyCode, @Direction, @TransactionTypeId, @TransactionTypeName, @ReferenceId, @StatusCode OUTPUT, @StatusName OUTPUT, @Balance OUTPUT", param);

            var statusCode = (int)param[8].Value;
            var statusName = (string)param[9].Value;
            var balance = (decimal)param[10].Value;

            return new Tuple<int, string, decimal>(statusCode, statusName, balance);
        }
        public async Task<Tuple<int, string, decimal, decimal>> TransferUserWallet(TransferUserWalletRequest request)
        {
            var param = new SqlParameter[]
            {
                new SqlParameter
                {
                    ParameterName = "@ClientId",
                    SqlDbType = System.Data.SqlDbType.UniqueIdentifier,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ClientId
                },
                new SqlParameter
                {
                    ParameterName = "@FromUserId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.FromUserId
                },
                new SqlParameter
                {
                    ParameterName = "@ToUserId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ToUserId
                },
                new SqlParameter
                {
                    ParameterName = "@Amount",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.Amount
                },
                new SqlParameter
                {
                    ParameterName = "@CurrencyCode",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.CurrencyCode
                },
                new SqlParameter
                {
                    ParameterName = "@TransactionTypeId",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.TransactionTypeId
                },
                new SqlParameter
                {
                    ParameterName = "@TransactionTypeName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.TransactionTypeName
                },
                new SqlParameter
                {
                    ParameterName = "@ReferenceId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 100,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ReferenceId
                },
                new SqlParameter
                {
                    ParameterName = "@StatusCode",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@StatusName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@FromBalance",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@ToBalance",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Output,
                }
            };

            await _walletContext.Database.ExecuteSqlRawAsync($"EXEC TransferUserWallet @ClientId, @FromUserId, @ToUserId, @Amount, @CurrencyCode, @TransactionTypeId, @TransactionTypeName, @ReferenceId, @StatusCode OUTPUT, @StatusName OUTPUT, @FromBalance OUTPUT, @ToBalance OUTPUT", param);

            var statusCode = (int)param[8].Value;
            var statusName = (string)param[9].Value;
            var fromBalance = (decimal)param[10].Value;
            var toBalance = (decimal)param[11].Value;

            return new Tuple<int, string, decimal, decimal>(statusCode, statusName, fromBalance, toBalance);
        }

        public async Task<Tuple<int, string, decimal, string>> GetUserWallet(UserWalletRequest request)
        {
            var param = new SqlParameter[]
            {
                new SqlParameter
                {
                    ParameterName = "@ClientId",
                    SqlDbType = System.Data.SqlDbType.UniqueIdentifier,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.ClientId
                },
                new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Input,
                    Value = request.UserId
                },
                new SqlParameter
                {
                    ParameterName = "@StatusCode",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@StatusName",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@Balance",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 26,
                    Scale = 12,
                    Direction = System.Data.ParameterDirection.Output,
                },
                new SqlParameter
                {
                    ParameterName = "@CurrencyCode",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 50,
                    Direction = System.Data.ParameterDirection.Output
                }
            };

            await _walletContext.Database.ExecuteSqlRawAsync($"EXEC GetUserWallet @ClientId, @UserId, @StatusCode OUTPUT, @StatusName OUTPUT, @Balance OUTPUT, @CurrencyCode OUTPUT", param);

            var statusCode = (int)param[2].Value;
            var statusName = (string)param[3].Value;
            var balance = (decimal)param[4].Value;
            var currencyCode = (string)param[5].Value;

            return new Tuple<int, string, decimal, string>(statusCode, statusName, balance, currencyCode);
        }
    }
}
