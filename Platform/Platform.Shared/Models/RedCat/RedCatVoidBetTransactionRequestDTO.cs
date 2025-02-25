namespace Platform.Shared.Models.RedCat
{
    public class RedCatVoidBetTransactionRequestDTO : RedCatBetTransactionRequestDTO
    {
        public long RollbackTransactionId { get; set; }
    }
}
