using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatOperator.API.Repositories
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetPlacement(RedCatOperatorBetTransactionRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessSettleBet(RedCatOperatorBetTransactionRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidPlacement(RedCatOperatorBetTransactionRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidSettlement(RedCatOperatorBetTransactionRequest request);
    }
}
