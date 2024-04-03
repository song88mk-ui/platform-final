using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGRequestDTO
    {
        public string GameCode { get; set; }
        public long Timestamp { get; set; }

    }
}
