using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGWalletResponseDTO : OGBaseResponseDTO
    {
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("balance")]
        public string Balance { get; set; }
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        [JsonPropertyName("currency")]
        public string CurrencyCode { get; set; }
    }
}
