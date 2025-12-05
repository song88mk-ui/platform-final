using MongoDB.Bson.Serialization.Attributes;
using Platform.Shared.Data.Sportsbook.Entities;
using System.Collections.Generic;

namespace Platform.Sportsbook.API.Models
{
    public class LeaguesLookupProjection
    {
        [BsonId]
        public string Id { get; set; }
        public long FiId { get; set; }
        public int FiStId { get; set; }
        public int FiTyId { get; set; }
        public int SpId { get; set; }
        public string SpNa { get; set; }
        public int SegId { get; set; }
        public string SegNa { get; set; }
        public int LeId { get; set; }
        public string LeNa { get; set; }
        public string ClCo { get; set; }
        public List<SportTranslationMDB> SpNaTr { get; set; }
        public List<SegmentTranslationMDB> SegNaTr { get; set; }
        public List<LeagueTranslationMDB> LeNaTr { get; set; }
    }
}
