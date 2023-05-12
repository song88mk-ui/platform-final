namespace Platform.QTech.API.Models
{
    public class BonusStatusRequest
    {
        public string PassKey { get; set; }
        public string BonusId { get; set; }
        public string PlayerId { get; set; }
        public string[] GameIds { get; set; }
        public decimal TotalBetValue { get; set; }
        public decimal TotalPayout { get; set; }
        public int[] RoundOptions { get; set; }
        public string Currency { get; set; }
        public string PromoCode { get; set; }
        public string Status { get; set; }
        public int ValidityDays { get; set; }
        public string PromotedDateTime { get; set; }
        public string ClaimedDateTime { get; set; }
        public string FailedDateTime { get; set; }
        public string CompletedDateTime { get; set; }
        public string CancelledDateTime { get; set; }
        public string DeletedDateTime { get; set; }
        public string ExpiredDateTime { get; set; }
        public int ClaimedRoundOption { get; set; }
        public string ClaimedGameId { get; set; }
    }
}
