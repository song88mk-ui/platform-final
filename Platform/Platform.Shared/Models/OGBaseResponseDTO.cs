using Platform.Shared.Enums;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGBaseResponseDTO
    {
        [JsonPropertyName("rs_code")]
        public string Code { get; set; }
        [JsonPropertyName("rs_message")]
        public string Message { get; set; }
    }
}
