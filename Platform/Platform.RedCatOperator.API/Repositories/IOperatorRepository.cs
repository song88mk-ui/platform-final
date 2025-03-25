using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatOperator.API.Repositories
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetPlacement(RedCatOperatorBetTransactionRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessSettleBet(RedCatOperatorBetTransactionRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidPlacement(RedCatOperatorBetTransactionRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidSettlement(RedCatOperatorBetTransactionRequestDTO request);
    }
}
