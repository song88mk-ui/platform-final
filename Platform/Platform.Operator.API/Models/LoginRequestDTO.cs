namespace Platform.Operator.API.Models
{
    public class LoginRequestDTO
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserAgent { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string Country { get; set; } = "";
    }
}
