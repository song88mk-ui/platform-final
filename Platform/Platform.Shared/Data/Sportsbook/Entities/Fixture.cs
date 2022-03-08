using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Platform.Shared.Data.Sportsbook.Entities
{
    public class Fixture
    {
        [BsonId]
        public long FiId { get; set; }

        public string PrFiId { get; set; }

        public int SpId { get; set; }

        public string SpNa { get; set; }

        public int SegId { get; set; }

        public string SegNa { get; set; }

        public int LeId { get; set; }

        public string LeNa { get; set; }

        public string FiNa { get; set; }

        public DateTime FiStDa { get; set; }

        public int FiStId { get; set; }

        public string FiStNa { get; set; }

        public int FiTyId { get; set; }

        public string FiTyNa { get; set; }

        public string Sc { get; set; }

        public bool IsOfLi { get; set; }

        public int HoReCa { get; set; }

        public int AwReCa { get; set; }

        public int CuPe { get; set; }

        public int CuMi { get; set; }

        public string FiSp { get; set; }

        public bool IsHoFa { get; set; }

        public int FiSoOr { get; set; }

        public int ToTi { get; set; }

        public string LiId { get; set; }

        public string StUr { get; set; }

        public bool IsSu { get; set; }
        public string ClCo { get; set; }
        public string GeAl { get; set; }
        public bool IsBo { get; set; }
        public string ClBo { get; set; }
        public List<SportTranslationMDB> SpNaTr { get; set; }
        public List<SegmentTranslationMDB> SegNaTr { get; set; }
        public List<LeagueTranslationMDB> LeNaTr { get; set; }
        public List<FixtureTranslationMDB> FiNaTr { get; set; }
    }

    public class FixtureFilter
    {
        [BsonId]
        public long FiId { get; set; }

        public string PrFiId { get; set; }

        public int SpId { get; set; }

        public string SpNa { get; set; }

        public int SegId { get; set; }

        public string SegNa { get; set; }

        public int LeId { get; set; }

        public string LeNa { get; set; }

        public string FiNa { get; set; }

        public DateTime FiStDa { get; set; }

        public int FiStId { get; set; }

        public string FiStNa { get; set; }

        public int FiTyId { get; set; }

        public string FiTyNa { get; set; }

        public string Sc { get; set; }

        public bool IsOfLi { get; set; }

        public int HoReCa { get; set; }

        public int AwReCa { get; set; }

        public int CuPe { get; set; }

        public int CuMi { get; set; }

        public string FiSp { get; set; }

        public bool IsHoFa { get; set; }

        public int FiSoOr { get; set; }

        public int ToTi { get; set; }

        public string LiId { get; set; }

        public string StUr { get; set; }

        public bool IsSu { get; set; }
        public List<SportTranslationMDB> SpNaTr { get; set; }
        public List<SegmentTranslationMDB> SegNaTr { get; set; }
        public List<LeagueTranslationMDB> LeNaTr { get; set; }
        public List<FixtureTranslationMDB> FiNaTr { get; set; }
    }

    public class SportTranslationMDB
    {
        [BsonId]
        public string LaCo { get; set; }
        public string SpNa { get; set; }
    }

    public class SegmentTranslationMDB
    {
        [BsonId]
        public string LaCo { get; set; }
        public string SegNa { get; set; }
    }

    public class LeagueTranslationMDB
    {
        [BsonId]
        public string LaCo { get; set; }
        public string LeNa { get; set; }
    }

    public class FixtureTranslationMDB
    {
        [BsonId]
        public string LaCo { get; set; }
        public string FiNa { get; set; }
    }

    public class FixtureProbabilityFilter
    {
        [BsonId]
        public long FiId { get; set; }
        public bool IsSu { get; set; }
        public int FiStId { get; set; }
    }

    public class FixtureClientSportFilter
    {
        [BsonId]
        public long FiId { get; set; }
        public int FiStId { get; set; }
        public int FiTyId { get; set; }
        public int SpId { get; set; }
        public string SpNa { get; set; }
    }

    public class FixtureSettlementFilter
    {
        [BsonId]
        public long FiId { get; set; }
        public bool IsSu { get; set; }
        public int FiStId { get; set; }
        public string FiSp { get; set; }
        public int SpId { get; set; }
    }

}
