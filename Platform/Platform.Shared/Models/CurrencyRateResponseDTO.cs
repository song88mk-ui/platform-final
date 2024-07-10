using System.Collections.Generic;

namespace Platform.Shared.Models
{
    public class ExchangeRateResponseDTO
    {
        public string CurrencyCode { get; set; }
        public decimal ExchangeRate { get; set; }
    }

    public class ExchangeRatesResponseDTO
    {
        public ExchangeRatesResponseDTO()
        {
            ExchangeRates = new List<ExchangeRateResponseDTO>();
        }
        public List<ExchangeRateResponseDTO> ExchangeRates { get; set; }
    }
}
