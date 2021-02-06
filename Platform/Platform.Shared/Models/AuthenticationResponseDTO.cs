namespace Platform.Shared.Models
{
    public class AuthenticationResponseDTO
    {
        public string UserId { get; set; }
        public int StatusId { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
    }
}
