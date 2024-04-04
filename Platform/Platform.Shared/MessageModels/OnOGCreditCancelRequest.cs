using System.Text.Json.Serialization;

namespace Platform.Shared.MessageModels
{
    public class OnOGCreditCancelRequest
    {
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("timestamp")]
        public long TimeStamp { get; set; }
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }
        [JsonPropertyName("transaction_type")]
        public string TransactionType { get; set; }
        [JsonPropertyName("secondary_info")]
        public object SecondaryInfo { get; set; }
        [JsonPropertyName("other_info")]
        public object OtherInfo { get; set; }
        [JsonPropertyName("remark")]
        public string Remark { get; set; }
        [JsonPropertyName("called_at")]
        public long CalledAt { get; set; }
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }
}
