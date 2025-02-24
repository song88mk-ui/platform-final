namespace Platform.Shared.Models.RedCat
{
    public class StakeResponseDTO
    {
        public decimal Balance { get; set; }
        public int StatusCode { get; set; }
        public string TransactionRefId { get; set; }
    }
}
