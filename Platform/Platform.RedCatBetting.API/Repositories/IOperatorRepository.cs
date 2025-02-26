using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatBetting.API.Repositories
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetPlacement(RedCatOperatorBetTransactionRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetSettlement(RedCatOperatorBetTransactionRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidPlacement(RedCatOperatorBetTransactionRequest request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidSettlement(RedCatOperatorBetTransactionRequest request);
    }
}
