using Platform.Shared.Enums;
using System;
using System.Collections.Generic;

namespace Platform.WebSocket.API.Messages
{
    public class OutgoingOddsChangeDelta
    {
        public OutgoingOddsChangeDelta()
        {
            Ma = new List<OutgoingMarket>();
        }
        public int SpId { get; set; }
        public int SegId { get; set; }
        public int LeId { get; set; }
        public string FiId { get; set; }
        public DateTime FiStDa { get; set; }
        public int FiStId { get; set; }
        public string FiStNa { get; set; }
        public int FiTyId { get; set; }
        public string FiTyNa { get; set; }
        public string Sc { get; set; }
        public string CuPe { get; set; }
        public int CuMi { get; set; }
        public List<OutgoingMarket> Ma { get; set; }
    }
    public class OutgoingMarketDelta
    {
        public OutgoingMarketDelta()
        {
            Se = new List<OutgoingSelectionDelta>();
        }
        public string MaId { get; set; }
        public int MaStId { get; set; }
        public string MaStNa { get; set; }
        public List<OutgoingSelectionDelta> Se { get; set; }
    }
    public class OutgoingSelectionDelta
    {
        public string SeId { get; set; }
        public decimal DeOd { get; set; }
        public bool IsAc { get; set; }
    }
}
