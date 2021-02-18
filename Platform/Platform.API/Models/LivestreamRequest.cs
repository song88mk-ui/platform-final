namespace Platform.API.Models
{
    public class LivestreamRequest
    {
        public string ProviderFixtureId { get; set; }
        public string IPAddress { get; set; }
        public bool IsMobile { get; set; }
    }
}
