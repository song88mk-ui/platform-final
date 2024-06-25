namespace Platform.Operator.API.Models
{
    public class OddsFeedBetRequest
    {
        public string BetId { get; set; }
        public int BetStatusId { get; set; }
        public string BetStatusName { get; set; }
        public string Reason { get; set; }
        public string Action { get; set; }
    }
}
