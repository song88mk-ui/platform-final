using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Platform.Shared.Data.Sportsbook.Entities
{
    public class Selection
    {
        [BsonId]
        public long SeId { get; set; }
        public int SpId { get; set; }
        public int SegId { get; set; }
        public int LeId { get; set; }
        public long FiId { get; set; }
        public long MaId { get; set; }
        public string MaTeId { get; set; }
        public string SeTeId { get; set; }
        public string SeNa { get; set; }
        public decimal Od { get; set; }
        public bool IsAc { get; set; }
        public int SeSoOr { get; set; }
        public int SeStId { get; set; }
        public string SeStNa { get; set; }
        public string SeSp { get; set; }
        public DateTime FiStDa { get; set; }
        public int FiStId { get; set; }
        public int MaStId { get; set; }
        public string CoId { get; set; }
    }
    public class SelectionFilter
    {
        [BsonId]
        public long SeId { get; set; }
        public long FiId { get; set; }
        public long MaId { get; set; }
        public string SeTeId { get; set; }
        public string SeNa { get; set; }
        public decimal Od { get; set; }
        public bool IsAc { get; set; }
        public int SeSoOr { get; set; }
        public string CoId { get; set; }
    }
}
