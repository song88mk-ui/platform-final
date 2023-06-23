using Platform.Shared.MessageModels;

namespace Platform.ESportOperator.Service.Repository
{
    public interface IPlatformRepository
    {
        void ProcessESportBetPlacementRequest(OnESportBetPlacementRequest message, string correlationId);
        void ProcessESportBetPlacementResponse(OnESportBetPlacementResponse message, string correlationId);
        void ProcessESportBetSettlementRequest(OnESportBetSettlementRequest message, string correlationId);
        void ProcessESportBetSettlementResponse(OnESportBetSettlementResponse message, string correlationId);
    }
}
