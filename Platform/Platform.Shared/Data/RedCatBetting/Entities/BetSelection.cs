using System;

namespace Platform.Shared.Data.RedCatBetting.Entities
{
    public class BetSelection
    {
        public Guid BetSelectionId { get; set; }
        public Guid BetId { get; set; }
        public string MemberCode { get; set; }
        public string GameId { get; set; }
        public long BetMarketId { get; set; }
        public int BetSelectionStatusId { get; set; }
        public string BetSelectionStatusName { get; set; }
        public bool IsSettled { get; set; }
        public bool IsResettled { get; set; }
        public string BetSelectionIdRef { get; set; }
    }
}
