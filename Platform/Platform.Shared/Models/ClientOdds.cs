using Platform.Shared.Data.Platform.Entities;
using System;

namespace Platform.Shared.Models
{
    public class ClientOdds
    {
        public string ClientCode { get; set; }
        public decimal Odds { get; set; }
        public string SelectionTemplateId { get; set; }
    }


}
