using System;
using System.Collections.Generic;

namespace Platform.WebSocket.API.Messages
{
    public class OutgoingBetStopFixture
    {
        public int SpId { get; set; }
        public int SegId { get; set; }
        public int LeId { get; set; }
        public string FiId { get; set; }
        public int FiTyId { get; set; }
        public DateTime FiStDa { get; set; }
        public int FiStId { get; set; }
        public bool IsBeSt { get; set; }
        public List<int> MaTeGr { get; set; }
    }
}
