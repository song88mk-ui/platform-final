using MongoDB.Bson.Serialization.Attributes;
using Platform.Shared.Data.Sportsbook.Entities;
using System;
using System.Collections.Generic;

namespace Platform.Sportsbook.API.Models
{
    public class ActiveFixtureProjection
    {
        [BsonId]
        public string Id { get; set; }
        public int ChTyId { get; set; }
        public long FiId { get; set; }
        public string PrFiId { get; set; }
        public int FiStId { get; set; }
        public string ClCo { get; set; } = default!;
        public int SpId { get; set; }
        public string SpNa { get; set; }
        public int SegId { get; set; }
        public string SegNa { get; set; }
        public int LeId { get; set; }
        public string LeNa { get; set; }
        public string FiNa { get; set; }
        public int FiTyId { get; set; }
        public string FiTyNa { get; set; }
        public DateTime FiStDa { get; set; }
        public string FiStNa { get; set; }
        public bool IsOfLi { get; set; }
        public string LiId { get; set; }
        public string GeAl { get; set; }
        public List<SportTranslationMDB> SpNaTr { get; set; }
        public List<SegmentTranslationMDB> SegNaTr { get; set; }
        public List<LeagueTranslationMDB> LeNaTr { get; set; }
        public List<FixtureTranslationMDB> FiNaTr { get; set; }
    }
}
