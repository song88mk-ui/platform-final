using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Platform;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Job.ForexEUR.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PlatformRepository> _logger;

        public PlatformRepository(IConfiguration configuration, HttpClient httpClient, ILogger<PlatformRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://openexchangerates.org");
        }
        public async Task ProcessForex()
        {
            _logger.LogDebug("attempting to make request to [openexchangerates.org]");

            var appId = _configuration.GetValue<string>("OpenExchangeRates:AppId");
            var baseCurrency = _configuration.GetValue<string>("OpenExchangeRates:BaseCurrency", "EUR");

            var response = await _httpClient.GetAsync($"https://openexchangerates.org/api/latest.json?app_id={appId}&base={baseCurrency}&show_alternative=true");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch rates: [{code}]", response.StatusCode);
                return;
            }

            var result = JsonSerializer.Deserialize<ExchangeRate>(await response.Content.ReadAsStringAsync(), _jsonOptions);

            if (result.Rates.Count <= 0)
            {
                _logger.LogError("Failed to serialize Rates, count={count}", result.Rates.Count);
                return;
            }

            _logger.LogInformation("Updating database [ForexEUR]");

            var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

            using (var context = new PlatformContext(optionsBuilder.Options))
            {
                foreach(var rate in result.Rates)
                {
                    _logger.LogDebug($"Updating [{rate.Key}]");
                    var currency = context.ForexEUR.Find(rate.Key);

                    if (currency == null)
                    {
                        context.ForexEUR.Add(new Shared.Data.Platform.Entities.ForexEUR
                        {
                            CurrencyCode = rate.Key.ToUpper(),
                            ExchangeRate = rate.Value
                        });
                    }
                    else
                    {
                        currency.ExchangeRate = rate.Value;
                        context.ForexEUR.Update(currency);
                    }

                }

                try
                {
                    context.SaveChanges();
                    _logger.LogInformation("Update to ForexEUR table complete!");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to save changes to the table");
                }
            }
        }
    }

    public class ExchangeRate
    {
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
