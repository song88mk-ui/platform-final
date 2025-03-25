namespace Platform.RedCat.API.Models
{
    public class RedCatVoidBetTransactionRequest : RedCatBetTransactionRequest
    {
        public string RollbackTransactionId { get; set; }
    }
}
