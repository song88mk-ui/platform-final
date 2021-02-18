namespace Platform.Shared.Models
{
    public class LivestreamResponseDTO
    {
        public string LiveStreamURL { get; set; }
    }

    public class LivestreamRequestDTO
    {
        public string ProviderFixtureId { get; set; }
        public string IPAddress { get; set; }
        public bool IsMobile { get; set; }
    }
}
