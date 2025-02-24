using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatBetting.API.Repositories
{
    public interface IBettingRepository
    {
        Task<BaseResponseDTO<StakeResponseDTO>> PlaceBet(RedCatBetRequest request);
    }
}
