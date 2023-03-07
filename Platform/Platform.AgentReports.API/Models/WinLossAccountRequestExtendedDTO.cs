using Platform.AgentReports.API.Models.Request;

namespace Platform.AgentReports.API.Models
{
    public class WinLossAccountRequestExtendedDTO : WinLossAccountRequest
    {
        public long? SelectedUserId { get; set; }
    }
}
