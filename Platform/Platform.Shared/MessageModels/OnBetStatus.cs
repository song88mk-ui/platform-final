using Platform.Shared.Enums;

namespace Platform.Shared.MessageModels
{
    public class OnBetStatus
    {
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public string BetId { get; set; }
        public BetStatus BetStatus { get; set; }
    }
}
