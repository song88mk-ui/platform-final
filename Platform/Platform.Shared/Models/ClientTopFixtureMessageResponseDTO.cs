using Platform.Shared.Enums;

namespace Platform.Shared.Models
{
    public class ClientTopFixtureMessageResponseDTO
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}
