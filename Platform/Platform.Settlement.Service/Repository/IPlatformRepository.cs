using Platform.Placement.Service.Entities;
using Platform.Settlement.Service.Utility;

namespace Platform.Settlement.Service.Repository
{
    public interface IPlatformRepository
    {
        void ProcessSettlement(OnSettlementMessage settlement, IMessageBroker messageBroker);
    }
}
