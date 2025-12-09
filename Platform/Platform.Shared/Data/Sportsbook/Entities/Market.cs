using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Data.Sportsbook.Entities
{
    [BsonIgnoreExtraElements]
    public class Market
    {
        [BsonId]
        public string Id { get; set; }
        public int ChTyId { get; set; }
        public long MaId { get; set; }
        public int SpId { get; set; }
        public int SegId { get; set; }
        public int LeId { get; set; }
        public long FiId { get; set; }
        public string MaTeId { get; set; }
        public string MaNa { get; set; }
        public string Li { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public int MaSoOr { get; set; }
        public DateTime FiStDa { get; set; }
        public int FiStId { get; set; }
        public int CaStId { get; set; }
        public string CaStNa { get; set; }
        public string ClCo { get; set; }
        public string ClCoMa { get; set; }
        public bool IsBo { get; set; }
        public string ClBo { get; set; }
        public List<MarketTranslationMDB> MaNaTr { get; set; }
    }

    public class MarketFilter
    {
        [BsonId]
        public string Id { get; set; }
        public long MaId { get; set; }
        public long FiId { get; set; }
        public string MaTeId { get; set; }
        public string MaNa { get; set; }
        public string Li { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public int CaStId { get; set; }
        public string ClCo { get; set; }
        public string ClCoMa { get; set; }
        public bool IsBo { get; set; }
        public List<MarketTranslationMDB> MaNaTr { get; set; }
    }

    public class MarketCount
    {
        [BsonId]
        public string Id { get; set; }
        public long MaId { get; set; }
        public long FiId { get; set; }
    }

    public class MarketTranslationMDB
    {
        [BsonId]
        public string LaCo { get; set; }
        public string MaNa { get; set; }
    }

    public class MarketProbabilityFilter
    {
        public long MaId { get; set; }
        public int MaStId { get; set; }
    }

    public class CashOutMarketFilter
    {
        public long MaId { get; set; }
        public int MaStId { get; set; }
    }
}
