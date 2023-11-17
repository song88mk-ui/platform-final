using Platform.ClientPlatform.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Repository
{
    public interface IReadOnlyRepository
    {
        Task<BaseResponseDTO<TWResponseDTO>> GetClientTopFixtures(string apiKey, string clientId, string countryCode, string languageCode);
        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(string apiKey, string clientId, string countryCode, string languageCode, ActiveFixturesFilterRequestDTO request);
        Task<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>> GetActiveFixtureDetails(string apiKey, string clientId, string fixtureId, string ipCountryCode, string languageCode);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string languageCode, string operatorToken, BetHistoryRequest request);
        Task<BaseResponseDTO<SettlementHistoryResponseDTO>> GetBetHistorySettled(string apiKey, string clientId, string languageCode, string ipAddress, BetHistorySettledRequest request);
        Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(string apiKey, string clientId, string operatorToken, string ipAddress);
    }
}
