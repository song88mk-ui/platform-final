using System;

namespace Platform.Shared.MessageModels
{
    public class BetCancellationPayload
    {
        public string UserId { get; set; }

        public Guid BetId { get; set; }

        public string Reason { get; set; }
    }
}
