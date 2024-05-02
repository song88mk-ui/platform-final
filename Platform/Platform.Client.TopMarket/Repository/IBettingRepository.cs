using Platform.Shared.MessageModels;
using System.Threading.Tasks;

namespace Platform.Client.TopMarket.Repository
{
    public interface IBettingRepository
    {
        Task ProcessBetPlacement(OnClientTopMarketBetPlacement message);
        Task ProcessBetSettlement(OnClientTopMarketBetSettlement message);
    }
}