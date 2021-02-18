using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class OrderedSportsRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
