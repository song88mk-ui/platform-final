using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ManualBetDetailsRequest
    {
        public ManualBetDetailsRequest()
        {
            BetIds = new List<string>();
        }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string ClientIdRefs { get; set; }
        public string SportIds { get; set; }
        public string PlayerNames { get; set; }
        public List<string> BetIds { get; set; }
    }

}
