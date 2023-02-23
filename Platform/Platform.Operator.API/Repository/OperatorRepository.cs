using Platform.Operator.API.Models;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;
using BetPlacementResponseDTO = Platform.Operator.API.Models.BetPlacementResponseDTO;

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
        public async Task<BetPlacementResponseDTO> BetPlacement(OperatorBetRequestDTO request, string clientId, string operatorToken)
        {
            var authResponse = await _authRepository.Authenticate(operatorToken);

            if (authResponse.StatusCode != Enums.AuthStatusCode.Success)
            {
                return new BetPlacementResponseDTO
                {
                    StatusId = 1002
                };
            }

            var betResponse = await _walletRepository.PlaceBet(new CreditOrDebitRequest
            {
                ClientId = Guid.Parse(clientId),
                UserId = authResponse.Result.UserId,
                Amount = request.Stake,
                CurrencyCode = request.CurrencyCode,
                TransactionTypeId = GetTransactionTypeId("Running"),
                TransactionTypeName = "BetPlacement",
                ReferenceId = request.BetId
            });

            return new BetPlacementResponseDTO
            {
                StatusId = betResponse.StatusCode,
                Balance = betResponse.Balance,
                StakeReal = request.Stake,
                StakeBonus = 0,
                IsFreeBet = false
            };
        }
        public async Task<BetSettlementResponseDTO> BetSettlement(OperatorBetSettlementRequestDTO request, string clientId)
        {
            var betResponse = await _walletRepository.SettleBet(new CreditOrDebitRequest
            {
                ClientId = Guid.Parse(clientId),
                UserId = request.UserId,
                Amount = request.Payout,
                CurrencyCode = request.CurrencyCode,
                TransactionTypeId = GetTransactionTypeId(request.BetStatus),
                TransactionTypeName = $"{request.BetStatus}Settlement",
                ReferenceId = request.BetId
            });

            return new BetSettlementResponseDTO
            {
                StatusId = betResponse.StatusCode,
                Balance = betResponse.Balance
            };
        }
        public async Task<BetSettlementResponseDTO> CashOutBet(OperatorBetSettlementRequestDTO request, string clientId)
        {
            var betResponse = await _walletRepository.CashOutBet(new CreditOrDebitRequest
            {
                ClientId = Guid.Parse(clientId),
                UserId = request.UserId,
                Amount = request.Payout,
                CurrencyCode = request.CurrencyCode,
                TransactionTypeId = GetTransactionTypeId(request.BetStatus),
                TransactionTypeName = $"{request.BetStatus}",
                ReferenceId = request.BetId
            });

            return new BetSettlementResponseDTO
            {
                StatusId = betResponse.StatusCode,
                Balance = betResponse.Balance
            };
        }
        public async Task<BetSettlementResponseDTO> BetCancellation(OperatorBetCancellationRequestDTO request, string clientId)
        {
            var betResponse = await _walletRepository.CancelBet(new CreditOrDebitRequest
            {
                ClientId = Guid.Parse(clientId),
                UserId = request.UserId,
                Amount = request.Payout,
                CurrencyCode = request.CurrencyCode,
                TransactionTypeId = GetTransactionTypeId(request.BetStatus),
                TransactionTypeName = $"{request.BetStatus}",
                ReferenceId = request.BetId
            });

            return new BetSettlementResponseDTO
            {
                StatusId = betResponse.StatusCode,
                Balance = betResponse.Balance
            };
        }

        private static int GetTransactionTypeId(string betStatus)
        {
            switch (betStatus)
            {
                case "Won":
                    return 3000;
                case "HalfWon":
                    return 3001;
                case "Lost":
                    return 4000;
                case "HalfLost":
                    return 4001;
                case "Void":
                    return 5000;
                case "Cancelled":
                    return 7000;
                case "CashedOut":
                    return 8000;
                default:
                    return 2000;
            }
        }
    }
}
