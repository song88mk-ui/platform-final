using Platform.Shared.Enums;

namespace Platform.API.Models
{
    public class BetResponse
    {
        public string UserId { get; set; }
        public string BetId { get; set; }
        public BetStatus BetStatus { get; set; }
    }
}
