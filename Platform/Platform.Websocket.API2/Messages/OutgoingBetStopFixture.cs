using System;
using System.Collections.Generic;

namespace Platform.WebSocket.API2.Messages
{
    public class OutgoingBetStopFixture
    {
        public OutgoingBetStopFixture()
        {
            Gr = new List<string>();
        }
        public List<string> Gr { get; set; }
        public BetStopFixture Pa { get; set; }
    }

    public class BetStopFixture
    {
        public string FiId { get; set; }
        public bool IsBeSt { get; set; }
    }
}
