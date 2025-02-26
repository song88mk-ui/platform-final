namespace Platform.Shared.Models
{
    public class OperatorRedCatBetRequestDTO
    {
        public string BetId { get; set; }
        public string UserId { get; set; }
        public long MarketId { get; set; }
        public string GameId { get; set; }
        public string BetStatus { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
    public class OperatorRedCatBetSettlementRequestDTO : OperatorRedCatBetRequestDTO
    {
        public decimal Payout { get; set; }
    }
}
