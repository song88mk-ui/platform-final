using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

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
        public double Pr { get; set; }
        public double WoPr { get; set; }
        public double LoPr { get; set; }
        public double VoPr { get; set; }
        public double HaWoPr { get; set; }
        public double HaLoPr { get; set; }
        public decimal DeHeFa { get; set; }
        public string ClBoOd { get; set; }
        public List<SelectionTranslationMDB> SeNaTr { get; set; }
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
        public string ClBoOd { get; set; }
        public List<SelectionTranslationMDB> SeNaTr { get; set; }
    }

    public class SelectionTranslationMDB
    {
        [BsonId]
        public string LaCo { get; set; }
        public string SeNa { get; set; }
    }

    public class SelectionProbabilityFilter
    {
        [BsonId]
        public long SeId { get; set; }
        public long FiId { get; set; }
        public long MaId { get; set; }
        public int MaStId { get; set; }
        public bool IsAc { get; set; }
        public double Pr { get; set; }
        public double WoPr { get; set; }
        public double LoPr { get; set; }
        public double VoPr { get; set; }
        public double HaWoPr { get; set; }
        public double HaLoPr { get; set; }
    }

    public class CashOutSelectionFilter
    {
        [BsonId]
        public long SeId { get; set; }
        public long MaId { get; set; }
        public int MaStId { get; set; }
        public bool IsAc { get; set; }
        public double Pr { get; set; }
        public double WoPr { get; set; }
        public double LoPr { get; set; }
        public double VoPr { get; set; }
        public double HaWoPr { get; set; }
        public double HaLoPr { get; set; }
    }
}
