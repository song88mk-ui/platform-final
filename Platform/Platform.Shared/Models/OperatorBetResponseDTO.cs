namespace Platform.Shared.Models
{
    public class OperatorBetResponseDTO
    {
        public int StatusId { get; set; }
        public decimal Balance { get; set; }
        public bool IsCashoutDisabled { get; set; }
        public decimal StakeReal { get; set; }
        public decimal StakeBonus { get; set; }
        public bool IsFreeBet { get; set; }
        public string Message { get; set; }
    }
}
