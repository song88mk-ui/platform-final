using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;

namespace Platform.ClientSportsbook.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(Guid clientId, string ipCountryCode, LanguageCode languageCode);
    }
}
