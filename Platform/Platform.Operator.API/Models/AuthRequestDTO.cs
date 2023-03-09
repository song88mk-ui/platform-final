namespace Platform.Operator.API.Models
{
    public class AuthRequestDTO
    {
        public string ClientId { get; set; }
        public string OperatorToken { get; set; }
        public string IsAgent { get; set; }
    }
}
