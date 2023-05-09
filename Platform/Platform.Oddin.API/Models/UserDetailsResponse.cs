namespace Platform.Oddin.API.Models
{
    public class UserDetailsResponse
    {
        public string UserId { get; set; }
        public string Currency { get; set; }
        public string Language { get; set; }
        public int ErrorCode { get; set; }
    }
}
