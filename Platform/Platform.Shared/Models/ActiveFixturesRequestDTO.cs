using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class ActiveFixturesRequestDTO
    {
        public Guid ClientId { get; set; }
        public string ClientCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string IPCountryCode { get; set; }
        public string ParticipantName { get; set; }
        public DateTime? FromFixtureStartDate { get; set; }
        public DateTime? ToFixtureStartDate { get; set; }
        public string FixtureStatus { get; set; }
    }
}
