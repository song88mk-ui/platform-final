using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGAuthenticationRequest
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("game_code")]
        public string GameCode { get; set; }
        [JsonPropertyName("pool_id")]
        public int PoolId { get; set; }
        [JsonPropertyName("betlimit")]
        public int BetLimit { get; set; }
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }
}
