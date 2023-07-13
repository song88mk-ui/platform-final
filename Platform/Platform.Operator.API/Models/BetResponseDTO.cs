namespace Platform.Operator.API.Models
{
    public class BetPlacementResponseDTO
    {
        public int StatusId { get; set; }
        public decimal Balance { get; set; }
        public decimal StakeReal { get; set; }
        public decimal StakeBonus { get; set; }
        public bool IsFreeBet { get; set; }
    }
}
