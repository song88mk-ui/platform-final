using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request);
        Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(BetRequestDTO request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request);
        Task<BaseResponseDTO<SettlementHistoryResponseDTO>> GetBetHistorySettled(BetHistorySettledRequestDTO request);
        Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(BetConfigRequestDTO request);
        Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request);
        Task<BaseResponseDTO<ManualSettlementResponseDTO>> ManualSettlement(ManualBetSettlementRequest request);
        Task<BaseResponseDTO<ManualBetDetailsResponseDTO>> GetManualBetDetails(ManualBetDetailsRequest request);
        Task<BaseResponseDTO<BetSelectionDetailsResponseDTO>> GetBetSelections(BetSelectionDetailsRequestDTO request);
        Task<BaseResponseDTO<ResendSettlementResponseDTO>> ResendSettlement(ResendSettlementMessageRequestDTO request);
        Task<BaseResponseDTO<BetCashOutResponseDTO>> CashOutBet(CashoutBetRequestDTO request);
        Task<BaseResponseDTO<CashoutStateResponseDTO>> GetCashOut(CashoutStateRequestDTO request);
    }
}
