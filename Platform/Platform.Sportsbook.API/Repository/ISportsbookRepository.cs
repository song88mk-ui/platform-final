using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<LivestreamResponseDTO>> GetLivestream(LivestreamRequestDTO request);
    }
}
