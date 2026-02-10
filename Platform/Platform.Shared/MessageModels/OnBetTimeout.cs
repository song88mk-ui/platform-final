using Platform.Shared.Enums;
using System;

namespace Platform.Shared.MessageModels
{
    public class OnBetTimeout
    {
        public Guid BetId { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public int ChannelTypeId { get; set; } = (int)ChannelType.BG;
    }
}
