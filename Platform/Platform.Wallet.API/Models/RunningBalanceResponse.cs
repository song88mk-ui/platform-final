namespace Platform.Wallet.API.Models
{
    public class RunningBalanceResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal CashBalance { get; set; }
    }
}
