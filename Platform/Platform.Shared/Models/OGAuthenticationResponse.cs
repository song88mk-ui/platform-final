using Platform.Shared.Models;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGAuthenticationResponse : OGBaseResponseDTO
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
