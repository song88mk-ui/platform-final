namespace Platform.Shared.Models
{
    public class LoginResponseDTO
    {
        public string OperatorToken { get; set; }
        public string UserId { get; set; }
    }
    public class LogoutResponseDTO
    {
        public int StatusId { get; set; }
    }
}
