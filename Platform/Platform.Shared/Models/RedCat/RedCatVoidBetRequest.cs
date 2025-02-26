namespace Platform.Shared.Models.RedCat
{
    public class RedCatVoidBetRequest : RedCatBetRequest
    {
        public long RollbackTransactionId { get; set; }
    }
}
