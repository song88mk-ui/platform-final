using System;

namespace Platform.WebSocket.API.Data
{
    public class WebSocket
    {
        public string ConnectionId { get; set; }
        public string IPAddress { get; set; }
        public string IPCountry { get; set; }
        public string Server { get; set; }
        public string UserAgent { get; set; }
        public string ClientHintUserAgent { get; internal set; }
        public string ClientHintUserAgentMobile { get; internal set; }
        public string ClientHintUserAgentPlatform { get; internal set; }
        public string HttpFetchDestination { get; internal set; }
        public string HttpFetchMode { get; internal set; }
        public string HttpFetchSite { get; internal set; }
        public DateTime ConnectedTimestamp { get; internal set; }
        public DateTime? DisconnectedTimestamp { get; internal set; }
    }
}
