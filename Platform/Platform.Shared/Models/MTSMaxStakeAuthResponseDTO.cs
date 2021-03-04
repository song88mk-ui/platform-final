using System;
using System.Collections.Generic;
using System.Text;
using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
namespace Platform.Shared.Models
{
    public class MTSMaxStakeAuthResponseDTO
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresInSeconds { get; set; }
    }

    public class MTSMaxStakeResponseDTO
    {
        [JsonProperty("ticketId")]
        public string TicketId { get; set; }

        [JsonProperty("maxStake")]
        public string MaxStake { get; set; }

        //[JsonProperty("timestampUtc")]
        //public long timestampUtc { get; set; }
    }

    public class MTSMaxStakeRequestDTO
    {
        [JsonProperty("timestampUtc")]
        public long TimeStamp { get; set; }

        [JsonProperty("bets")]
        public List<MTSBetDTO> Bets { get; set; }

        [JsonProperty("ticketId")]
        public string TicketId { get; set; }

        [JsonProperty("selections")]
        public List<MTSBetSelectionDTO> Selections { get; set; }

        [JsonProperty("sender")]
        public SenderDTO Sender { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; } = "2.2";

        [JsonProperty("testSource")]
        public bool TestSource { get; set; } = false;

        [JsonProperty("oddsChange")]
        public string OddsChange { get; set; } = "any";
    }

    public class MTSBetDTO
    {
        [JsonProperty("stake")]
        public MTSStakeDTO Stake { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("selectedSystems")]
        public int[] SelectedSystems { get; set; } = new int[] { 1 };
    }

    public class MTSBetSelectionDTO
    {
        [JsonProperty("eventId")]
        public string EventId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("odds")]
        public decimal Odds { get; set; }
    }

    public class MTSStakeDTO
    {
        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "total";
    }

    public class SenderDTO
    {
        [JsonProperty("currency")]
        public string Currency { get; set; } = "EUR";

        [JsonProperty("channel")]
        public string Channel { get; set; } = "internet";

        [JsonProperty("bookmakerId")]
        public int BookmakerId { get; set; }

        [JsonProperty("endCustomer")]
        public EndCustomerDTO EndCustomer { get; set; }

        [JsonProperty("limitId")]
        public int LimitId { get; set; }
    }

    public class EndCustomerDTO
    {
        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("languageId")]
        public string LanguageId { get; set; } = "EN";

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; } = "deviceId";

        [JsonProperty("id")]
        public string Id { get; set; } = "endCustomer";
    }
}
