using Platform.Shared.Models;

namespace Platform.API.Models
{
    public class OGClientRequest : OGRequestDTO
    {
        public string PlayerCode { get; set; }
        public int PoolId { get; set; }
        public int BetLimit { get; set; }
        public string Nickname { get; set; }
        public string Signature { get; set; }
        public bool IsSBLaunch { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageCode { get; set; }
    }
}
