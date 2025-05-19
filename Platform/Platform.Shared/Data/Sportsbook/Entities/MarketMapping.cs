using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Data.Sportsbook.Entities
{
    [BsonIgnoreExtraElements]
    public class MarketMapping
    {
        [BsonId]
        public long MaId { get; set; }

        public string PrFiId { get; set; }

        public string MaTeId { get; set; }

        public string MaSp { get; set; }
    }

    public class MarketMappingFilter
    {
        [BsonId]
        public long MaId { get; set; }
        public string PrFiId { get; set; }
        public string MaTeId { get; set; }
        public string MaSp { get; set; }
    }
}
