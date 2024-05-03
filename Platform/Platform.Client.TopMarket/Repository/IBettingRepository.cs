using Platform.Shared.MessageModels;
using System.Threading.Tasks;

namespace Platform.ClientTopMarket.Service.Repository
{
    public interface IBettingRepository
    {
        Task ProcessBetPlacement(OnClientTopMarketBetPlacement message);
        Task ProcessBetSettlement(OnClientTopMarketBetSettlement message);
    }
}