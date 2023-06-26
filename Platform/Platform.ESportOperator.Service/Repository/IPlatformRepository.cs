using Platform.Shared.MessageModels;

namespace Platform.ESportOperator.Service.Repository
{
    public interface IPlatformRepository
    {
        void ProcessESportDebitRequest(OnESportDebitRequest message, string correlationId);
        void ProcessESportDebitResponse(OnESportDebitResponse message, string correlationId);
        void ProcessESportCreditRequest(OnESportCreditRequest message, string correlationId);
        void ProcessESportCreditResponse(OnESportCreditResponse message, string correlationId);
    }
}
