using Platform.Shared.Enums;

namespace Platform.API.Models
{
    public class CashoutBetRequest
    {
        public string BetId { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public decimal CashoutStake { get; set; }
        public decimal? CashoutStakePercent { get; set; };
    }
}
