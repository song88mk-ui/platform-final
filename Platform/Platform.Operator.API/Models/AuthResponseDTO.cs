namespace Platform.Operator.API.Models
{
    public class AuthResponseDTO
    {
        public int StatusId { get; set; }
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageCode { get; set; }
    }
}
