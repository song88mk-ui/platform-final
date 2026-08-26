using System;

namespace Platform.Shared.MessageModels
{
    // Slim delayed-verdict relay: published by Platform.Provider.Service when the GTS Ably
    // channel delivers the verdict for a "cashout-" prefixed assessment (AssessmentDelayed),
    // consumed by Platform.CashOut.Service to resume the parked cashout.
    public class OnBetCashOutDelayedVerdict
    {
        public Guid BetId { get; set; }
        public bool Accepted { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
