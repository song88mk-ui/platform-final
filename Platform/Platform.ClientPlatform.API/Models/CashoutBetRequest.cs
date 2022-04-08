using Platform.Shared.Enums;

namespace Platform.ClientPlatform.API.Models
{
    public class CashoutBetRequest
    {
        public string BetId { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public decimal CashoutStake { get; set; }
    }
}
