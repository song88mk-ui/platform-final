using System.Collections.Generic;

namespace Platform.Shared.Models
{
    // BO-125: bets whose Cancelled status must be sent to the provider (GTS) again. Only the ids are
    // taken from the caller. The bet's status and data are read from the betting database, so a bet
    // that is not actually Cancelled is refused no matter what the caller believes.
    public class ResendProviderCancellationRequestDTO
    {
        public ResendProviderCancellationRequestDTO()
        {
            BetIds = new List<string>();
        }

        public List<string> BetIds { get; set; }
    }
}
