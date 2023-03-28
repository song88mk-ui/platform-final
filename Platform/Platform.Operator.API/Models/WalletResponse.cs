namespace Platform.Operator.API.Models
{
    public class WalletResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public decimal Balance { get; set; }
        public decimal Payable { get; set; }
        public string CurrencyCode { get; set; }
    }
}
