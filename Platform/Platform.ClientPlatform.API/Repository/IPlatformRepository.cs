using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Repository
{
    public interface IPlatformRepository
    {
        Task<BaseResponseDTO<TWResponseDTO>> GetClientTopFixtures(string clientId, string apiKey, string countryCode, string languageCode);
    }
}
