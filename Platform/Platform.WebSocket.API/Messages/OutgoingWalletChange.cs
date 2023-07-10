namespace Platform.WebSocket.API.Messages
{
    public class OutgoingWalletChange
    {
        public string UsId { get; set; }
        public decimal Ba { get; set; }
        public decimal? Pa { get; set; }
    }
}
