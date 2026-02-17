using System.Collections.Generic;

namespace Platform.Shared.Models.Provider.BetGenius
{
    public class BGLiveStreamDto
    {
        public string Id { get; set; }

        public List<string> Pe { get; set; }

        public DeliveriesDto Ot { get; set; }
    }
}
