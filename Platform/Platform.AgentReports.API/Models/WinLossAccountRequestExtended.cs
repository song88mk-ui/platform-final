using Platform.AgentReports.API.Models.Request;

namespace Platform.AgentReports.API.Models
{
    public class WinLossAccountRequestExtended : WinLossAccountRequest
    {
        public long? SelectedUserId { get; set; }
    }
}
