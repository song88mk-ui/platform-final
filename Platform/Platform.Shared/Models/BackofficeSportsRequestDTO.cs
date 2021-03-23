using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class BackofficeSportsRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
