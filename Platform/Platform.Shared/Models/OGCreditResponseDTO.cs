using System.Text.Json.Serialization;

namespace Platform.Shared.Models
{
    public class OGCreditResponseDTO : OGBaseResponseDTO
    {
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }
        [JsonPropertyName("amount")]
        public string Amount { get; set; }
        [JsonPropertyName("updated_balance")]
        public string Balance { get; set; }
        [JsonPropertyName("billing_at")]
        public long BillingAt { get; set; }
    }
}
