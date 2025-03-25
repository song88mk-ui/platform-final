namespace Platform.Shared.Models.RedCat
{
    public class RedCatVoidBetRequestDTO : RedCatBetRequestDTO
    {
        public long RollbackTransactionId { get; set; }
    }
}
