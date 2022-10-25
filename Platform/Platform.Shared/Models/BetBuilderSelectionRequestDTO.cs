using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class BetBuilderSelectionRequestDTO
    {
        public string FixtureId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}
