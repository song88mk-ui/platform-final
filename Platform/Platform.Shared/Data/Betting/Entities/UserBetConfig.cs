using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Platform.Shared.Data.Betting.Entities
{
    public class UserBetConfig
    {
        [Key]
        public string UserId { get; set; }
        public Guid ClientId { get; set; }
        public string CurrencyCode { get; set; }
        public string VerticalConfig { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
