using Platform.Shared.Enums;
using System;

namespace Platform.AgentReports.API.Models.Request
{
    public class WinLossAccountRequestExtended : WinLossAccountRequest
    {
        public long? SelectedUserId { get; set; }
    }
}
