namespace Platform.Shared.Models
{
    public class BetConfigResponseDTO
    {
        public decimal MinBet { get; set; }
        public decimal MaxBet { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MaxFixtureBet { get; set; }
        public decimal ReferenceBet { get; set; }
        public decimal MaxLiability { get; set; }
        public bool IsMaxTier { get; set; }
    }
}
