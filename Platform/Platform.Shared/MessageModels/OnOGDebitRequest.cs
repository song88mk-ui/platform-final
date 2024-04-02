using Newtonsoft.Json;
using Platform.Shared.Models;
using System;

namespace Platform.Shared.MessageModels
{
    public class OnOGDebitRequest
    {
        [JsonProperty("player_code")]
        public string PlayerCode { get; set; }
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }
        [JsonProperty("total_amount")]
        public decimal TotalAmount { get; set; }
        [JsonProperty("transaction_type")]
        public string TransactionType { get; set; }
        [JsonProperty("game_code")]
        public string GameCode { get; set; }
        [JsonProperty("round_id")]
        public string RoundId { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("called_at")]
        public long CalledAt { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }
        [JsonProperty("records")]
        public string Records { get; set; }
    }
}
