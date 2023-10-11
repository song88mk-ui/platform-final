using System;

namespace Platform.Shared.Data.Betting.Entities
{
    public class BetInfo
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public string UserAgent { get; set; }
    }
}
