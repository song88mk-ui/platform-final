namespace Platform.Sportsbook.API.Models
{
    public class LivestreamResponse
    {
        public string Url { get; set; }
        public Error Error { get; set; }
    }

    public sealed class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
