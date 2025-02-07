using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ForexResponseDTO
    {
        public ForexResponseDTO()
        {
            Values = new List<Forex>();
        }
        public List<Forex> Values { get; set; }
    }

    public class Forex
    {
        public string CurrCode { get; set; }
        public decimal ExchangeRate { get; set; }
    }
}
