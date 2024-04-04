using System.Text.Json.Serialization;

namespace Platform.Shared.MessageModels
{
    public class OnOGCreditRequest
    {
        [JsonPropertyName("player_code")]
        public string PlayerCode { get; set; }
        [JsonPropertyName("timestamp")]
        public long TimeStamp { get; set; }
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }
        [JsonPropertyName("transaction_type")]
        public string TransactionType { get; set; }
        [JsonPropertyName("game_code")]
        public string GameCode { get; set; }
        [JsonPropertyName("round_id")]
        public string RoundId { get; set; }
        [JsonPropertyName("bet_place")]
        public string BetPlace { get; set; }
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
        [JsonPropertyName("winlose_amount")]
        public string WinloseAmount { get; set; }
        [JsonPropertyName("effective_amount")]
        public string EffectiveAmount { get; set; }
        [JsonPropertyName("result_url")]
        public string ResultUrl { get; set; }
        [JsonPropertyName("secondary_info")]
        public string SecondaryInfo { get; set; }
        [JsonPropertyName("other_info")]
        public string OtherInfo { get; set; }
        [JsonPropertyName("remark")]
        public string Remark { get; set; }
        [JsonPropertyName("called_at")]
        public long CalledAt { get; set; }
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }
}
