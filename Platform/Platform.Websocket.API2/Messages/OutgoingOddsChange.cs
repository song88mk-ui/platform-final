using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.WebSocket.API2.Messages
{
    public class OutgoingOddsChange
    {
        public OutgoingOddsChange()
        {
            Gr = new List<string>();
        }
        public List<string> Gr { get; set; }
        public OddsChangeFixture Pa { get; set; }
    }
    public class OddsChangeFixture
    {
        public OddsChangeFixture()
        {
            Ma = new List<OutgoingMarket>();
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
        public int FiTyId { get; set; }
        public string FiTyNa { get; set; }
        public string Sc { get; set; }
        public string CuPe { get; set; }
        public int CuMi { get; set; }
        public int SoOr { get; set; }
        public int MaCo { get; set; }
        public bool IsBeBu { get; set; }
        public List<OutgoingMarket> Ma { get; set; }
    }
    public class OutgoingMarket
    {
        public OutgoingMarket()
        {
            Se = new List<OutgoingSelection>();
        }
        public string MaTeId { get; set; }
        public string MaId { get; set; }
        public string Li { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public string MaNa { get; set; }
        public int SoOr { get; set; }
        public bool IsBeBu { get; set; }
        public List<OutgoingSelection> Se { get; set; }
    }
    public class OutgoingSelection
    {
        public string SeTeId { get; set; }
        public string SeId { get; set; }
        public string SeNa { get; set; }
        public string DeOd { get; set; }
        public string MaOd { get; set; }
        public string HoOd { get; set; }
        public string InOd { get; set; }
        public bool IsAc { get; set; }
        public int SoOr { get; set; }
    }
    public class OutgoingSportTranslation
    {
        public string LaCo { get; set; }
        public string SpNa { get; set; }
    }
    public class OutgoingSegmentTranslation
    {
        public string LaCo { get; set; }
        public string SegNa { get; set; }
    }
    public class OutgoingLeagueTranslation
    {
        public string LaCo { get; set; }
        public string LeNa { get; set; }
    }
    public class OutgoingFixtureTranslation
    {
        public string LaCo { get; set; }
        public string FiNa { get; set; }
        public string HoNa { get; set; }
        public string AwNa { get; set; }
    }
    public class OutgoingMarketTranslation
    {
        public string LaCo { get; set; }
        public string MaNa { get; set; }
    }
    public class OutgoingSelectionTranslation
    {
        public string LaCo { get; set; }
        public string SeNa { get; set; }
    }
}
