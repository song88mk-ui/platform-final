namespace Platform.WebSocket.API.Messages
{
    public class OutgoingCashOutChange
    {
        public string UsId { get; set; }
        public string BeId { get; set; }
        public int CaOuStId { get; set; }
        public string CaOuStNa { get; set; }
        public decimal CaOuSt { get; set; }
    }
}
