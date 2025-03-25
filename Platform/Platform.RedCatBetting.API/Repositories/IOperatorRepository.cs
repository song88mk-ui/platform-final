using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatBetting.API.Repositories
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetPlacement(RedCatOperatorBetTransactionRequestDTO request);
        Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetSettlement(RedCatOperatorBetTransactionRequestDTO request);
    }
}
