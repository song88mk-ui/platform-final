using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatBetting.API.Repositories
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> PlaceBet(RedCatBetRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> SettleBet(RedCatBetRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidPlacement(RedCatVoidBetRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettlement(RedCatVoidBetRequestDTO request);
    }
}
