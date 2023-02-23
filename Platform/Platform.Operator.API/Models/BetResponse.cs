namespace Platform.Operator.API.Models
{
    public class BetResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }

        public decimal Balance { get; set; }
    }
}
