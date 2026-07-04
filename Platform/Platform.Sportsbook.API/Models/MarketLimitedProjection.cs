using MongoDB.Bson.Serialization.Attributes;
using Platform.Shared.Data.Sportsbook.Entities;
using System;
using System.Collections.Generic;

namespace Platform.Sportsbook.API.Models
{
    public class MarketLimitedProjection
    {
        [BsonId]
        public string Id { get; set; }
        public long MaId { get; set; }
        public long FiId { get; set; }
        public string MaTeId { get; set; }
        public string MaNa { get; set; }
        public string Li { get; set; }
        public bool IsHa { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public string ClCo { get; set; }
        public string ClCoMa { get; set; }
        public List<MarketTranslationMDB> MaNaTr { get; set; }
    }
}
