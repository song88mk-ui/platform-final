using System;
using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Request
{
    public class CancelBetHistoryRequest
    {
        public CancelBetHistoryRequest()
        {
            BetIds = new List<string>();
        }

        public DateTime FromSettledDate { get; set; }
        public DateTime ToSettledDate { get; set; }
        public string MemberName { get; set; }
        public string PartnerName { get; set; }
        public List<string> BetIds { get; set; }
    }
}
