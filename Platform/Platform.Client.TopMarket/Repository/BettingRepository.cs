using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.MessageModels;
using System;
using System.Threading.Tasks;

namespace Platform.Client.TopMarket.Repository
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
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    var market = await context.ClientTopMarket.FindAsync(message.ClientId, message.FixtureId, message.MarketId);

                    if (market != null)
                    {
                        market.BetCount += 1;
                        context.Update(market);
                    }
                    else
                    {
                        market = new ClientTopMarket()
                        {
                            ClientId = message.ClientId,
                            ClientCode = message.ClientCode,
                            ClientName = message.ClientName,
                            SportId = message.SportId,
                            SportName = message.SportName,
                            SegmentId = message.SegmentId,
                            SegmentName = message.SegmentName,
                            LeagueId = message.LeagueId,
                            LeagueName = message.LeagueName,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        }

        public async Task ProcessBetSettlement(OnClientTopMarketBetSettlement message)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    var market = await context.ClientTopMarket.FirstOrDefaultAsync(c => c.FixtureId == message.FixtureId && c.MarketId == message.MarketId);

                    if (market != null)
                    {
                        market.MarketStatusId = message.MarketStatusId;
                        market.MarketStatusName = message.MarketStatusName;
                        context.Update(market);

                        context.SaveChanges();
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}