using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;
using System.Threading.Tasks;
using System;

namespace Platform.QTech.API.Repository
{
    public interface IProviderRepository
    {
        Task<Tuple<AuthenticationCode, AuthenticationResponse>> GetToken();
        Task<AuthTokenRevokeCode> RevokeToken(string token);
        Task<Tuple<GameLobbyCode, GameLobbyResponse>> GetGameLobbyURL(GameLobbyRequest request);
    }
}
