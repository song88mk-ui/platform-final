using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Betting;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.ClientTopMarket.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BettingRepository> _logger;
        public BettingRepository(IConfiguration configuration, ILogger<BettingRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ProcessBetPlacement(OnClientTopMarketBetPlacement message)
        {
            try
            {
                try
                {
                    await CountBetPlacement(message);
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
                {
                    // GM-929: another consumer inserted this key between FindAsync and SaveChanges, and the primary key
                    // rejected the second insert. The row exists now, so the retry finds it and counts the bet there.
                    await CountBetPlacement(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        }

        private async Task CountBetPlacement(OnClientTopMarketBetPlacement message)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

            using (var context = new BettingContext(optionsBuilder.Options))
            {
                var market = await context.ClientTopMarket.FindAsync(message.FixtureId, message.MarketId, message.ClientId, message.CurrencyCode);

                if (market != null)
                {
                    market.BetCount += 1;
                    context.Update(market);
                }
                else
                {
                    market = new Shared.Data.Betting.Entities.ClientTopMarket()
                    {
                        ClientId = message.ClientId,
                        ClientCode = message.ClientCode,
                        ClientName = message.ClientName,
                        CurrencyCode = message.CurrencyCode.ToUpper(),
                        FixtureId = message.FixtureId,
                        FixtureName = message.FixtureName,
                        MarketId = message.MarketId,
                        MarketName = message.MarketName,
                        MarketStatusId = message.MarketStatusId,
                        MarketStatusName = message.MarketStatusName,
                        BetCount = 1
                    };

                    context.Add(market);
                }
                context.SaveChanges();
            }
        }

        public Task ProcessBetSettlement(OnClientTopMarketBetSettlement message)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    var markets = context.ClientTopMarket.Where(c => c.FixtureId == message.FixtureId && c.MarketId == message.MarketId && c.MarketStatusId != message.MarketStatusId);

                    if (markets != null && markets.Any())
                    {
                        foreach (var market in markets)
                        {
                            market.MarketStatusId = message.MarketStatusId;
                            market.MarketStatusName = message.MarketStatusName;
                            context.Update(market);
                        }

                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }

            return Task.CompletedTask;
        }
    }
}
