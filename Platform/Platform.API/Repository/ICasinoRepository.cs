using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public interface ICasinoRepository
    {
        Task<BaseResponseDTO<GameLobbyResponseDTO>> GetLobbyURL(GameLobbyRequestDTO request);
    }
}
