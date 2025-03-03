namespace Platform.Shared.Models.RedCat
{
    public class RedCatVoidBetTransactionRequestDTO : RedCatBetTransactionRequestDTO
    {
        public string RollbackTransactionId { get; set; }
    }
}
