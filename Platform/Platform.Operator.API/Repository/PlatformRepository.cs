using Microsoft.EntityFrameworkCore;
using Platform.Operator.API.Data;
using Platform.Shared.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            if(stake > result.Balance)
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

            //Update wallet balance
            result.Balance -= stake;
            _platformContext.SaveChanges();

            return new OperatorBetResponseDTO
            {
                StatusId = 1000,
                Balance = result.Balance
            };
        }
    }
}
