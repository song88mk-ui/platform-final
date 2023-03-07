using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Shared.Data.Reports.Entities
{
    public class PartnerRevenueShare
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public bool IsGGREnabled { get; set; }
        public decimal CompanyGGR { get; set; }
        public decimal ClientGGR { get; set; }
        public decimal MonthlyFee { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime EffectivityDate { get; set; }
    }
}
