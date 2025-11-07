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
            var response = await _httpClient.GetAsync("https://openexchangerates.org/api/latest.json?app_id=b2f778983d0d4b11a98e3cf2b4849c01&base=EUR&show_alternative=true");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("success! parsing response");
                var result = JsonSerializer.Deserialize<ExchangeRate>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                foreach (var rate in result.Rates)
                {
                    try
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                        using (var context = new PlatformContext(optionsBuilder.Options))
                        {
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

                            context.SaveChanges();
                            _logger.LogInformation($"update to ForexEUR [{rate.Key}] table complete!");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"error processing forex: {ex.Message}");
                    }
                }
            }
        }
    }

    public class ExchangeRate
    {
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
