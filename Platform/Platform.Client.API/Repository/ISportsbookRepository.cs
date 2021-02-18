using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Client.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(string apiKey, string clientId, LanguageCode languageCode);
    }
}
