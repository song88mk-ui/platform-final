namespace Platform.Shared.Models
{
    public class MessageRequestDTO
    {
        public string Payload { get; set; }
        public string MessageType { get; set; }
        public string RoutingKey { get; set; }
        public string CorrelationId { get; set; }
    }
    public class MessageResponseDTO
    {
        public bool IsSuccess { get; set; }
    }
}
