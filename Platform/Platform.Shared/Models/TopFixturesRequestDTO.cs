using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class TopFixturesRequestDTO
    {
        public Guid ClientId { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string ClientCode { get; set; }
    }
}
