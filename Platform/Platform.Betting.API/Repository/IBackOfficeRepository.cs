using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IBackOfficeRepository
    {
        Task<BaseResponseDTO<BetConfigLeagueResponse>> GetTierMaxStake(BetConfigLeagueRequestDTO request);
    }
}
