namespace Platform.Shared.Models
{
    public class OperatorESportsBetSettlementRequestDTO
    {
        public string BetId { get; set; }
        public string UserId { get; set; }
        public decimal Stake { get; set; }
        public decimal Payout { get; set; }
        public string BetStatus { get; set; }
        public string CurrencyCode { get; set; }
    }
}
