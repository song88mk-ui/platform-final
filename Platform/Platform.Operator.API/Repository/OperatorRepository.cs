using Platform.Operator.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public class OperatorRepository : IOperatorRepository
    {
        private readonly IAuthRepository _authRepository;
        private readonly IWalletRepository _walletRepository;

        public OperatorRepository(IAuthRepository authRepository, IWalletRepository walletRepository)
        {
            _authRepository = authRepository;
            _walletRepository = walletRepository;
        }
        public async Task<AuthResponseDTO> Authenticate(string clientId, string operatorToken)
        {
            var authResponse = await _authRepository.Authenticate(operatorToken);

            if (authResponse.StatusCode != Enums.AuthStatusCode.Success)
            {
                return new AuthResponseDTO
                {
                    StatusId = 1002
                };
            }

            var walletResponse = await _walletRepository.GetUserWallet(new UserWalletRequest { ClientId = Guid.Parse(clientId), UserId = authResponse.Result.UserId });

            if (walletResponse.StatusCode != 1000)
            {
                return new AuthResponseDTO
                {
                    StatusId = 1003
                };
            }

            return new AuthResponseDTO
            {
                StatusId = 1000,
                UserId = authResponse.Result.UserId,
                Balance = walletResponse.Balance,
                CurrencyCode = walletResponse.CurrencyCode,
                LanguageCode = "EN"
            };
        }
    }
}
