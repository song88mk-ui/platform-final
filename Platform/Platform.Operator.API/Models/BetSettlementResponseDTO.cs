namespace Platform.Operator.API.Models
{
    public class BetSettlementResponseDTO
    {
        public int StatusId { get; set; }
        public decimal Balance { get; set; }
        public decimal? Payable { get; set; }
    }
}
