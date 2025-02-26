using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCat.API.Repositories
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> PlaceBet(RedCatBetRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> SettleBet(RedCatBetRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidStake(RedCatVoidBetRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettle(RedCatVoidBetRequest request);
    }
}
