using System.Text.Json.Serialization;

namespace Platform.API.Models
{
    public class OGBaseResponse
    {
        [JsonPropertyName("rs_code")]
        public string Code { get; set; }
        [JsonPropertyName("rs_message")]
        public string Message { get; set; }
    }
}
