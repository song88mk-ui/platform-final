using Platform.ClientPlatform.API.Models;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<BaseResponseDTO<TWResponseDTO>> GetClientTopFixtures(string apiKey, string clientId, string countryCode, string languageCode);
        Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(string apiKey, string clientId, string languageCode, string operatorToken, BetHistoryRequest request);
        Task<BaseResponseDTO<SettlementHistoryResponseDTO>> GetSettlementHistory(string apiKey, string clientId, string languageCode, string ipAddress, SettlementHistoryRequest request);
        Task<BaseResponseDTO<BetCashOutResponseDTO>> CashoutBet(string apiKey, string clientId, string operatorToken, CashoutBetRequest request);
    }
}
