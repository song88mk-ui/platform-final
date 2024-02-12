namespace Platform.WebSocket.API2.Data
{
    public class WebSocket
    {
        public string ConnectionId { get; set; }
        public string IPAddress { get; set; }
        public string IPCountry { get; set; }
        public string Server { get; set; }
        public string UserAgent { get; set; }
        public string ClientCode { get; set; }
        public string RequestURL { get; set; }
        public DateTime ConnectedTimestamp { get; internal set; }
        public DateTime? DisconnectedTimestamp { get; internal set; }
    }
}
