using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatBetting.API.Repositories
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> PlaceBet(RedCatBetRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> SettleBet(RedCatBetRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidPlacement(RedCatVoidBetRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettlement(RedCatVoidBetRequest request);
    }
}
