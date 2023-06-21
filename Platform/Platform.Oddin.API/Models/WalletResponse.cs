namespace Platform.Oddin.API.Models
{
    public class WalletResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public decimal Balance { get; set; }
    }
}
