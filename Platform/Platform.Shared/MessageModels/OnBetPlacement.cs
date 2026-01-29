using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Providers;
using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    public class OnBetPlacement
    {
        public OnBetPlacement()
        {
            BetSelections = new List<BetSelectionMessageExtenstion>();
        }
        public Bet Bet { get; set; }
        public bool IsBoosted { get; set; }
        public bool IsBetBuilder { get; set; }
        public string BonusId { get; set; }
        public decimal Odds { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public bool IsOGClient { get; set; }
        public bool WillRecordTopMarket { get; set; }
        public int ChannelTypeId { get; set; }
        public BetAcceptance? DelayedBetAcceptance { get; set; }
        public string? DelayedMessage { get; set; }
        public List<BetSelectionMessageExtenstion> BetSelections { get; set; }
    }
    public class BetSelectionMessageExtenstion
    {
        public BetSelection BetSelection { get; set; }
        public int MarketSelectionCount { get;set; }
    }
    public class OnBetPlacementError
    {
        public string BetId { get; set; }
        public string Error { get; set; }
        public string StackTrace { get; set; }
        public string InnerException { get; set; }
    }
}
