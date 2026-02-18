using System;
using System.Text.Json.Serialization;

namespace Platform.Shared.Models.Provider.BetGenius
{
    public class BGLiveStreamResponseDto
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("drm")]
        public DrmInfo Drm { get; set; }
    }

    public class DrmInfo
    {
        [JsonPropertyName("widevine")]
        public string Widevine { get; set; }

        [JsonPropertyName("playready")]
        public string Playready { get; set; }

        [JsonPropertyName("fairplay")]
        public string Fairplay { get; set; }

        [JsonPropertyName("fairplayCertificate")]
        public string FairplayCertificate { get; set; }
    }
}
