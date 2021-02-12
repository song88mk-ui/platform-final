namespace Platform.Shared.Models
{
    public class WalletBalanceResponseDTO
    {
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
    }
}
