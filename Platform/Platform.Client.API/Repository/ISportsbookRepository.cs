using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Client.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(string apiKey, string clientId, LanguageCode languageCode);
        Task<BaseResponseDTO<ActiveFixtureCountResponseDTO>> GetActiveFixtureCount(string apiKey, string clientId, LanguageCode languageCode);
        Task<BaseResponseDTO<SportsLookupResponseDTO>> GetSportsLookup(string apiKey, string clientId, LanguageCode languageCode);
        Task<BaseResponseDTO<SegmentsLookupResponseDTO>> GetSegmentsLookup(string apiKey, string clientId, LanguageCode languageCode);
        Task<BaseResponseDTO<LeaguesLookupResponseDTO>> GetLeaguesLookup(string apiKey, string clientId, LanguageCode languageCode);
    }
}
