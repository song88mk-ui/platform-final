using MongoDB.Bson.Serialization.Attributes;
using System;

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

        public string LiStId { get; set; }

        public string StUr { get; set; }

        public bool IsSu { get; set; }
    }
}
