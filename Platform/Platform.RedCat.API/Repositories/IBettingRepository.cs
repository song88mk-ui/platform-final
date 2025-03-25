using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCat.API.Repositories
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> PlaceBet(RedCatBetRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> SettleBet(RedCatBetRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidStake(RedCatVoidBetRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettle(RedCatVoidBetRequestDTO request);
    }
}
