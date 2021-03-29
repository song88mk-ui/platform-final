using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Platform.Entities
{
    public class ForexEUR
    {
        public string CurrencyCode { get; set; }

        [Column(TypeName = "decimal(26,12)")]
        public decimal ExchangeRate { get; set; }
    }
}
