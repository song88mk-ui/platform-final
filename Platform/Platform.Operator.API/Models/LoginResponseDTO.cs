namespace Platform.Operator.API.Models
{
    public class LoginResponseDTO
    {
        public int StatusId { get; set; }
        public string OperatorToken { get; set; }
        public string Userid { get; set; }
    }

    public class LogoutResponseDTO
    {
        public int StatusId { get; set; }
    }
}
