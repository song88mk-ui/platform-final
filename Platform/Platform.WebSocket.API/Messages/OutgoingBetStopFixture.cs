using System.Collections.Generic;

namespace Platform.WebSocket.API.Messages
{
    public class OutgoingBetStopFixture
    {
        public int SpId { get; set; }
        public string FiId { get; set; }
        public bool IsBeSt { get; set; }
        public List<int> MaTeGr { get; set; }
        public string CoId { get; set; }
    }
}
