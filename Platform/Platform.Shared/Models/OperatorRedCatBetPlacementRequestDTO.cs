namespace Platform.Shared.Models
{
    public class OperatorRedCatBetRequestDTO
    {
        public string BetId { get; set; }
        public long MarketId { get; set; }
        public string GameId { get; set; }
        public string BetStatus { get; set; }
        public decimal Stake { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
