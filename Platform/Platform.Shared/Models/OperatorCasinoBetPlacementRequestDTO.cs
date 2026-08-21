namespace Platform.Shared.Models
{
    public class OperatorCasinoBetPlacementRequestDTO
    {
        public string BetId { get; set; }
        public decimal Stake { get; set; }
        public string CurrencyCode { get; set; }
    }
}
