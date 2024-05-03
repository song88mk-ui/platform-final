using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGDebitResponseDTO : OGBaseResponseDTO
    {
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }
        [JsonPropertyName("updated_balance")]
        public string Balance { get; set; }
    }
}
