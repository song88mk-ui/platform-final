using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request);
        Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(BetRequestDTO request);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request);
        Task<BaseResponseDTO<BetConfigFEResponseDTO>> BetConfig(BetConfigRequestDTO request);
        Task<BaseResponseDTO<BetCashOutResponseDTO>> CashOutBet(CashoutBetRequestDTO request);
        Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO request);
        Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request);
        Task<BaseResponseDTO<ManualSettlementResponseDTO>> ManualSettlement(ManualBetSettlementRequest request);
        Task<BaseResponseDTO<ManualBetDetailsResponseDTO>> GetManualBetDetails(ManualBetDetailsRequest request);
        Task<BaseResponseDTO<StatusCode>> ProcessParlayBonus(ParlayBonusRequestDTO request);
        Task<BaseResponseDTO<ParlayBonusResponseDTO>> GetParlayBonus(GetParlayBonusRequestDTO request);
    }
}
