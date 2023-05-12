namespace Platform.QTech.API.Models
{
    public class BonusRewardRequest
    {
        public string PassKey { get; set; }
        public string ContentType { get; set; }
        public string RewardType { get; set; }
        public string RewardTitle { get; set; }
        public string TxnId { get; set; }
        public string PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Created { get; set; }
    }
}
