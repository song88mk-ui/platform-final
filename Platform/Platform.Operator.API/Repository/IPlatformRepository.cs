using Platform.Operator.API.Models;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Operator.API.Repository
{
    public interface IPlatformRepository
    {
        Task<AuthenticationResponseDTO> Authenticate(string operatorToken);
        Task<CashoutResultResponseDTO> CashOutResult(CashoutResultRequestDTO request);
        Task<OperatorBetResponseDTO> WalletBalance(string operatorToken);
        Task<OGWalletResponseDTO> WalletBalanceOG(string userId);
        Task<OperatorBetResponseDTO> BetPlacement(OperatorBetRequestDTO request, string operatorToken, string clientId);
        Task<OperatorBetResponseDTO> BetFullCashOut(OperatorBetSettlementRequestDTO request, string clientId);
        Task<OperatorBetResponseDTO> BetPartialCashOut(OperatorBetSettlementRequestDTO request, string clientId);
        Task<OperatorBetResponseDTO> BetSettlement(OperatorBetSettlementRequestDTO request, string clientId);
        Task<OperatorBetResponseDTO> BetCancellation(OperatorBetCancellationRequestDTO request, string clientId);
        Task<FixtureScoreResponseDTO> FixtureScore(FixtureScoreRequestDTO request);
        Task<BaseResponseDTO<GenerateUserResponseDTO>> AddUser(GenerateUserRequestDTO request);

        bool AddClient(string clientId);
        bool RemoveClient(string clientId);
        List<string> GetClients();
        bool AddTestConfig(string clientId, bool isFreeBet, decimal stakeRealPercent, decimal stakeBonusPercent);
        bool RemoveTestConfig(string clientId);
        List<TestConfig> GetTestConfig();
        Task<BetPlacementResponseDTO> BetPlacementCasino(OperatorCasinoBetPlacementRequestDTO request, string clientId, string operatorToken);
        Task<BetSettlementResponseDTO> BetSettlementCasino(OperatorCasinoBetSettlementRequestDTO request, string clientId);
        Task<BetPlacementResponseDTO> DebitESports(OperatorESportsDebitRequestDTO request, string clientId, string operatorToken);
        Task<BetSettlementResponseDTO> CreditESports(OperatorESportsCreditRequestDTO request, string clientId);

    }
}
