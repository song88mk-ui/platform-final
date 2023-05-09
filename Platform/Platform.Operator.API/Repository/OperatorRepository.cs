using Platform.Operator.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;
using LoginResponseDTO = Platform.Operator.API.Models.LoginResponseDTO;
using LogoutResponseDTO = Platform.Operator.API.Models.LogoutResponseDTO;

namespace Platform.Operator.API.Repository
{
    public class OperatorRepository : IOperatorRepository
    {
        private readonly IAuthRepository _authRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IReportsRepository _reportsRepository;

        public OperatorRepository(IAuthRepository authRepository, IWalletRepository walletRepository, IReportsRepository reportsRepository)
        {
            _authRepository = authRepository;
            _walletRepository = walletRepository;
            _reportsRepository = reportsRepository;
        }
        public async Task<LoginResponseDTO> Login(LoginRequestDTO request, string userAgent = "", string ipAddress = "", string country = "")
        {
            var loginResponse = await _authRepository.Login(new LoginRequest { UserName = request.UserName, Password = request.Password, Provider = "1002" });

            if (loginResponse.StatusCode != Enums.AuthStatusCode.Success)
            {
                return new LoginResponseDTO
                {
                    StatusId = (int)loginResponse.StatusCode,
                    OperatorToken = loginResponse.Result.OperatorToken
                };
            }

            return new LoginResponseDTO
            {
                StatusId = 1000,
                OperatorToken = loginResponse.Result.OperatorToken
            };
        }
        public async Task<LogoutResponseDTO> Logout(string operatorToken)
        {
            var loginResponse = await _authRepository.Logout(operatorToken);

            if (loginResponse.StatusCode != Enums.AuthStatusCode.Success)
            {
                return new LogoutResponseDTO
                {
                    StatusId = 1002
                };
            }

            return new LogoutResponseDTO
            {
                StatusId = 1000
            };
        }
        public async Task<AuthResponseDTO> Authenticate(AuthRequestDTO request)
        {
            var authResponse = await _authRepository.Authenticate(request.OperatorToken, request.IsAgent);

            if (authResponse.StatusCode != Enums.AuthStatusCode.Success)
            {
                return new AuthResponseDTO
                {
                    StatusId = 1002
                };
            }

            var walletResponse = await _walletRepository.GetUserWallet(new UserWalletRequest { ClientId = Guid.Parse(request.ClientId), UserId = authResponse.Result.UserId });

            if (walletResponse.StatusCode != 1000)
            {
                return new AuthResponseDTO
                {
                    StatusId = 1003
                };
            }

            var reportResponse = await _reportsRepository.GetRunningBalance(authResponse.Result.UserId);

            if (reportResponse.StatusCode == 1000)
            {
                walletResponse.Payable = reportResponse.CashBalance;
            }

            return new AuthResponseDTO
            {
                StatusId = 1000,
                UserId = authResponse.Result.UserId,
                Balance = walletResponse.Balance,
                Payable = walletResponse.Payable,
                CurrencyCode = walletResponse.CurrencyCode,
                LanguageCode = GetLanguageCode(walletResponse.CurrencyCode)
            };
        }

        private static string GetLanguageCode(string currencyCode)
        {
            switch (currencyCode)
            {
                case "KRW":
                    return "KO";
                case "VND":
                    return "VI";
                case "THB":
                    return "TH";
                case "CNY":
                    return "ZH";
                case "MYR":
                    return "MS";
                case "KHR":
                    return "KH";
                case "IDR":
                    return "ID";

                //case "INR":
                //    return "KH";
                //case "JPY":
                //    return "KH";
                //case "MMK":
                //    return "KH";
                //case "HKD":
                //    return "KH";
                //case "PHP":
                //    return "KH";
                //case "AED":
                //    return "KH";
                //case "TRY":
                //    return "KH";
                //case "TWD":
                //    return "KH";
                //case "NZD":
                //    return "KH";
                //case "ZAR":
                //    return "KH";
                //case "KES":
                //    return "KH";
                //case "TZS":
                //    return "KH";
                //case "GHS":
                //    return "KH";
                //case "ZMW":
                //    return "KH";
                //case "MZN":
                //    return "KH";
                //case "CDF":
                //    return "KH";
                //case "ETB":
                //    return "KH";
                //case "MWK":
                //    return "KH";
                //case "XAF":
                //    return "KH";
                //case "NGN":
                //    return "KH";
                //case "BTC":
                //    return "KH";
                //case "ETH":
                //    return "KH";
                //case "PKR":
                //    return "KH";
                //case "BRL":
                //    return "KH";
                //case "CLP":
                //    return "KH";
                //case "PEN":
                //    return "KH";
                //case "MXN":
                //    return "KH";

                default:
                    return "EN";
            }
        }

        public async Task<BetPlacementResponseDTO> BetPlacement(OperatorBetRequestDTO request, string clientId, string operatorToken, string isAgent)
        {
            var authResponse = await _authRepository.Authenticate(operatorToken, isAgent);

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
        public async Task<BetSettlementResponseDTO> CashOutBet(OperatorBetSettlementRequestDTO request, string clientId, string operatorToken, string isAgent)
        {
            var authResponse = await _authRepository.Authenticate(operatorToken, isAgent);

            if (authResponse.StatusCode != Enums.AuthStatusCode.Success)
            {
                return new BetSettlementResponseDTO
                {
                    StatusId = 1002
                };
            }

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
