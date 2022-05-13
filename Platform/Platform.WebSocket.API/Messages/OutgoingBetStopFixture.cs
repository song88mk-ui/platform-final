using System;
using System.Collections.Generic;

namespace Platform.WebSocket.API.Messages
{
    public class OutgoingBetStopFixture
    {
        public OutgoingBetStopFixture()
        {
            Gr = new List<string>();
        }
        public List<string> Gr { get; set; }
        public BetStopFixture Fi { get; set; }
    }

    public class BetStopFixture
    {
        public string FiId { get; set; }
        public bool IsBeSt { get; set; }
        public List<int> MaTeGr { get; set; }
    }
}
