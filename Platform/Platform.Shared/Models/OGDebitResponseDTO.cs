using Platform.Shared.Enums;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGDebitResponseDTO : OGBaseResponseDTO
    {
        [JsonPropertyName("rs_code")]
        public string Code { get; set; }
        [JsonPropertyName("rs_message")]
        public string Message { get; set; }
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }
        [JsonPropertyName("updated_balance")]
        public decimal Balance { get; set; }
    }
}
