namespace Platform.Operator.API.Models
{
    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Provider { get; set; }
    }
}
