using System.Collections.Generic;
using System.Linq;

namespace Platform.Shared.Models
{
    public class OrientalGamingConfigDTO
    {
        public string VendorName { get; set; }
        public string PublicAPIKey { get; set; }
        public string PrivateAPIKey { get; set; }
        public string ClientIds { get; set; }
        public string HighCurrenciesAsString { get; set; }
        public List<string> HighCurrencies => HighCurrenciesAsString?.Replace(" ", "").Split(",").ToList();
    }
}
