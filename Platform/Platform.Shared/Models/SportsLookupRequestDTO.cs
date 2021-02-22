using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class SportsLookupRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
