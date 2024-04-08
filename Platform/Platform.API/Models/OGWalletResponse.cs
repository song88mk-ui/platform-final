using Platform.Shared.Models;
using System.Text.Json.Serialization;

namespace Platform.API.Models
{
    public class OGWalletResponse : OGBaseResponseDTO
    {
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        [JsonPropertyName("currency")]
        public string CurrencyCode { get; set; }
    }
}
