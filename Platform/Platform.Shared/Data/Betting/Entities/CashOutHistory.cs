using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
    public class CashOutHistory
    {
        public Guid Id { get; set; }
        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string ClientCode { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal OriginalStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal StakeUsed { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PreStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TrueStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TrueStakeUsed { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PreTrueStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostTrueStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal CashoutPercent { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal CashoutAmount { get; set; }
        public DateTime CashoutDate { get; set; }
        public DateTime CashoutDateOffset { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TaxedStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal TaxedWinnings { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal NonTaxedPayout { get; set; }
    }
}
