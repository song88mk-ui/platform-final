using Platform.ClientSportsbook.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Threading.Tasks;

namespace Platform.ClientSportsbook.API.Repository
{
    public interface ISportsbookRepository
    {
        Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetClientActiveFixtures(ClientKeyFilter clientKeyFilter, ActiveFixtureRequest request);
        Task<BaseResponseDTO<TWLimitedResponseDTO>> GetActiveFixtureDetails(ClientKeyFilter clientKeyFilter, string fixtureId);
        Task<BaseResponseDTO<bool>> SubscribeToMarkets(string clientCode, MarketsSubscriptionRequestDTO request);
        Task<BaseResponseDTO<bool>> UnsubscribeToMarkets(string clientCode, MarketsSubscriptionRequestDTO request);
        Task<BaseResponseDTO<TWLimitedResponseDTO>> GetMarketsSubscription(ClientKeyFilter clientKeyFilter);
    }
}
