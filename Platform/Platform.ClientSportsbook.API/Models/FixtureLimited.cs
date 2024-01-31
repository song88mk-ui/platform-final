using System.Collections.Generic;
using System;

namespace Platform.ClientSportsbook.API.Models
{
    public class FixtureLimited
    {
        public FixtureLimited()
        {
            Ma = new List<MarketLimited>();
            SpNaTr = new List<SportTranslation>();
            SegNaTr = new List<SegmentTranslation>();
            LeNaTr = new List<LeagueTranslation>();
            FiNaTr = new List<FixtureTranslation>();
        }
        public int SpId { get; set; }
        public string SpNa { get; set; }
        public int SegId { get; set; }
        public string SegNa { get; set; }
        public int LeId { get; set; }
        public string LeNa { get; set; }
        public string FiId { get; set; }
        public string PrFiId { get; set; }
        public string FiNa { get; set; }
        public string HoNa { get; set; }
        public string AwNa { get; set; }
        public DateTime FiStDa { get; set; }
        public bool IsOfLi { get; set; }
        public int FiStId { get; set; }
        public string FiStNa { get; set; }
        public List<SportTranslation> SpNaTr { get; set; }
        public List<SegmentTranslation> SegNaTr { get; set; }
        public List<LeagueTranslation> LeNaTr { get; set; }
        public List<FixtureTranslation> FiNaTr { get; set; }
        public List<MarketLimited> Ma { get; set; }
    }

    public class MarketLimited
    {
        public MarketLimited()
        {
            Se = new List<SelectionLimited>();
            MaNaTr = new List<MarketTranslation>();
        }
        public string MaTeId { get; set; }
        public string MaId { get; set; }
        public string Li { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public string MaNa { get; set; }
        public List<MarketTranslation> MaNaTr { get; set; }
        public List<SelectionLimited> Se { get; set; }
    }
    public class SelectionLimited
    {
        public SelectionLimited()
        {
            SeNaTr = new List<SelectionTranslation>();
        }
        public string SeId { get; set; }
        public string SeNa { get; set; }
        public string Od { get; set; }
        public List<SelectionTranslation> SeNaTr { get; set; }
    }

    public class MarketTranslation
    {
        public string LaCo { get; set; }
        public string MaNa { get; set; }
    }

    public class SelectionTranslation
    {
        public string LaCo { get; set; }
        public string SeNa { get; set; }
    }
    public class SportTranslation
    {
        public string LaCo { get; set; }
        public string SpNa { get; set; }
    }
    public class SegmentTranslation
    {
        public string LaCo { get; set; }
        public string SegNa { get; set; }
    }
    public class LeagueTranslation
    {
        public string LaCo { get; set; }
        public string LeNa { get; set; }
    }
    public class FixtureTranslation
    {
        public string LaCo { get; set; }
        public string FiNa { get; set; }
        public string HoNa { get; set; }
        public string AwNa { get; set; }
    }
}
