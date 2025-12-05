using MongoDB.Bson.Serialization.Attributes;
using Platform.Shared.Data.Sportsbook.Entities;
using System.Collections.Generic;
using System;

namespace Platform.Sportsbook.API.Models
{
    public class BetBuilderFixtureProjection
    {
        [BsonId]
        public string Id { get; set; }
        public long FiId { get; set; }
        public string FiSp { get; set; }
    }

    public class BetBuilderMarketProjection
    {
        [BsonId]
        public string Id { get; set; }
        public long MaId { get; set; }
        public string MaTeId { get; set; }
        public string Li { get; set; }
        public string ClCo { get; set; }
    }

    public class BetBuilderSelectionProjection
    {
        [BsonId]
        public string Id { get; set; }
        public long SeId { get; set; }
        public long MaId { get; set; }
        public string SeTeId { get; set; }
        public string MaTeId { get; set; }
        public string SeSp { get; set; }
    }

    public class BetBuilderMarketMappingProjection
    {
        [BsonId]
        public string Id { get; set; }
        public long MaId { get; set; }
        public string MaSp { get; set; }
    }

    public class BetBuilderSelectionProjectionDTO
    {
        public long SeId { get; set; }
        public long MaId { get; set; }
        public string SeTeId { get; set; }
        public string MaTeId { get; set; }
        public string MaSp { get; set; }
    }
}
