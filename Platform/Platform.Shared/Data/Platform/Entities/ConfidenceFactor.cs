namespace Platform.Shared.Data.Platform.Entities
{
    public class ConfidenceFactor
    {
        public long Timestamp { get; set; }
        public string UserId { get; set; }
        public double CurrentCCF { get; set; }
        public double PreviousCCF { get; set; }
        public bool IsSpecificSport { get; set; }
        public string SportId { get; set; }
        public string SportName { get; set; }
        public string AppliedTo { get; set; }
    }
}
