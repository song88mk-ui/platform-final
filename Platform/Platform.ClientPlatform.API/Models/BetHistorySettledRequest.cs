using System;

namespace Platform.ClientPlatform.API.Models
{
    public class BetHistorySettledRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string UserId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
