using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Models
{
    public class ActiveFixturesFilterRequestDTO
    {
        public string ParticipantName { get; set; }
        public DateTime? FromFixtureStartDate { get; set; }
        public DateTime? ToFixtureStartDate { get; set; }
        public string FixtureStatus { get; set; }
    }
}
