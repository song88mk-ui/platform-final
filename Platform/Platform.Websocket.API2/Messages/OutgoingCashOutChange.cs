namespace Platform.WebSocket.API2.Messages
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
