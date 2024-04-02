using System.Text.Json.Serialization;

namespace Platform.API.Models
{
    public class OGAuthenticationResponse : OGBaseResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
