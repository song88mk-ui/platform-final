using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Shared.Data.Betting.Entities
{
    public class League
    {
        [Key]
        public int Id { get; set; }
        public int SegmentId { get; set; }
        public string Segment { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int Tier { get; set; }
        public int SportId { get; set; }
        public int SortOrder { get; set; }
    }
}
