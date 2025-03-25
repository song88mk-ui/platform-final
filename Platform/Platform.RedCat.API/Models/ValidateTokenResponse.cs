namespace Platform.RedCat.API.Models
{
    public class ValidateTokenResponse
    {
        public string MemberCode { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }
}
