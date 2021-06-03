using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
    public class CashOutHistory
    {
        public long Id { get; set; }
        public Guid BetId { get; set; }
        public string ClientCode { get; set; }
        public string ClientId { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal OriginalStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal StakeUsed { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PreStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal PostStake { get; set; }
        [Column(TypeName = "decimal(26,12)")]
        public decimal CashedOutAmount { get; set; }
        public DateTime CashoutDate { get; set; }
        public DateTime CashoutDateOffset { get; set; }
        public int CashoutTypeId { get; set; }
        public string CashoutTypeName { get; set; }
    }
}
