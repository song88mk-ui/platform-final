namespace Platform.API.Models
{
    public class AuthenticationResponse
    {
        public string UserId { get; set; }
        public int StatusId { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
    }
}
