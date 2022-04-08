using Platform.Shared.Enums;
using System;

namespace Platform.ClientPlatform.API.Models
{
    public class BetHistoryRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public BetStatusFilter BetStatusFilter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool IsCurrent { get; set; }
    }
}
