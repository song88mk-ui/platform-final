using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class NoMarginClientsResponseDTO
    {
        public List<string> ClientCodes { get; set; } = new List<string>();
        public List<string> MarketTemplateIds { get; set; } = new List<string>();
    }
}
