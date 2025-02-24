using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatOperator.API.Repositories
{
    public interface IOperatorRepository
    {
        Task<BaseResponseDTO<StakeResponseDTO>> ProcessBetPlacement(RedCatBetRequest request);
    }
}
