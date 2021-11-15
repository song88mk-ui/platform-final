using Platform.Shared.Data.Platform.Entities;
using System;

namespace Platform.Shared.Models
{
    public class ClientOddsModel
    {
        public string ClientCode { get; set; }
        public decimal Odds { get; set; }
        public decimal OldOdds { get; set; }
        public string SelectionTemplateId { get; set; }
    }


}
