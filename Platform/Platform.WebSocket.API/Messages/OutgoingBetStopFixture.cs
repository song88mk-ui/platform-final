using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Platform.WebSocket.API.Messages
{
    public class OutgoingBetStopFixture
    {
        public ReadOnlyCollection<string> Gr { get; set; }
        public BetStopFixture Pa { get; set; }
    }

    public class BetStopFixture
    {
        public string FiId { get; set; }
        public bool IsBeSt { get; set; }
        public List<int> MaTeGr { get; set; }
    }
}
