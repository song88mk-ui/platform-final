using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Reports.Entities
{
    public class PartnerRevenueShare
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public bool IsGGREnabled { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal CompanyGGR { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal ClientGGR { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal MonthlyFee { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime EffectivityDate { get; set; }
    }
}
