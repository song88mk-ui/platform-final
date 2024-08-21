using System;

namespace Platform.Shared.MessageModels
{
    public class OnOddsFeedBetCancellation
    {
        public string ClientId { get; set; }
        public Guid BetId { get; set; }
        public string Reason { get; set; }
    }
}
