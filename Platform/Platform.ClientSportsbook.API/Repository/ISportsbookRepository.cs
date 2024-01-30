using Platform.ClientSportsbook.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;

namespace Platform.ClientSportsbook.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(ClientKeyFilter clientKeyFilter);
        Task<BaseResponseDTO<ActiveFixtureDetailsResponseDTO>> GetActiveFixtureDetails(ClientKeyFilter clientKeyFilter, string fixtureId);
    }
}
