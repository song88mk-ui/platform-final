using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.CashOutStake.Servicev2.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Utility;

namespace Platform.CashOutStake.Servicev2.Test.Repository
{
    // GM-864: PreloadActiveBets used `return` instead of `continue` inside its bet loop, so
    // hitting the first excluded bet (no selections, or a virtual-sport/outright leg) stopped
    // preloading the entire list - every running bet after it in query order got no pushed
    // cashout offer at all until it was re-added by an odds change or a new placement.
    public class PreloadActiveBetsTests
    {
        private sealed class NoOpMemCache : IMemCache
        {
            public void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout) { }
            public void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date) { }
            public T? GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class => null;
            public T? GetCache<T>(string key) where T : class => null;
            public KeyValuePair<bool, long> GetCache(string key) => default;
            public void DeleteCache(string key) { }
            public void DeleteCacheByPrefix(string prefix) { }
        }

        private static BettingRepository CreateRepository(out IServiceProvider serviceProvider, string databaseName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<BettingContext>(options => options.UseInMemoryDatabase(databaseName));
            serviceProvider = services.BuildServiceProvider();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:MongoDB-Sportsbook"] = "mongodb://localhost:27017"
                })
                .Build();

            return new BettingRepository(serviceProvider, configuration, new NoOpMemCache(), NullLogger<BettingRepository>.Instance);
        }

        private static Bet ARunningBet(Guid betId) => new Bet
        {
            BetId = betId,
            ClientCode = "CODE",
            ClientId = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            BetStatusId = (int)BetStatus.Running,
            BetTypeId = 0,
            IsFreeBet = false,
            TrueStake = 100m,
            Stake = 100m,
            StakeBonus = 0m,
            Odds = 2.0m,
            StakeTax = 0m,
            WinningsTax = 0m,
            OddsTypeId = (int)OddsType.Decimal
        };

        private static BetSelection ARunningSelection(Guid betId) => new BetSelection
        {
            BetSelectionId = Guid.NewGuid(),
            BetId = betId,
            SportId = 1,
            FixtureId = 1,
            FixtureTypeId = (int)FixtureType.Match,
            MarketId = 1,
            SelectionId = 1,
            BetSelectionStatusId = (int)BetStatus.Running,
            Odds = 2.0m,
            DeadHeatFactor = 1.0m,
            IsBinary = true
        };

        [Fact]
        public void PreloadActiveBets_ExcludedBetFirstInList_StillLoadsLaterBets()
        {
            var repository = CreateRepository(out var serviceProvider, $"preload-{Guid.NewGuid():N}");

            var excludedBetId = Guid.NewGuid();
            var validBetId = Guid.NewGuid();

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BettingContext>();

                // Excluded: no BetSelection rows at all, seeded first so it is the bet that
                // trips the `return`-vs-`continue` bug before the valid bet is even reached.
                context.Bet.Add(ARunningBet(excludedBetId));

                context.Bet.Add(ARunningBet(validBetId));
                context.BetSelection.Add(ARunningSelection(validBetId));

                context.SaveChanges();
            }

            repository.PreloadActiveBets();

            Assert.DoesNotContain(repository._bets, b => b.BetId == excludedBetId);
            Assert.Contains(repository._bets, b => b.BetId == validBetId);
        }

        [Fact]
        public void PreloadActiveBets_PartialCashoutBet_LoadsAuditHistory()
        {
            var repository = CreateRepository(out var serviceProvider, $"preload-{Guid.NewGuid():N}");

            var betId = Guid.NewGuid();

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BettingContext>();

                var bet = ARunningBet(betId);
                bet.IsPartialCashout = true;
                context.Bet.Add(bet);
                context.BetSelection.Add(ARunningSelection(betId));
                context.PartialCashoutAudit.Add(new PartialCashoutAudit
                {
                    Id = Guid.NewGuid(),
                    BetId = betId,
                    CashoutDate = DateTime.UtcNow,
                    CashoutAmount = 10m,
                    CashoutPercent = 0.1m,
                    OriginalStake = 100m,
                    PostStake = 90m,
                    PostTrueStake = 90m,
                    PreStake = 100m,
                    PreTrueStake = 100m,
                    StakeUsed = 10m,
                    TrueStake = 100m,
                    TrueStakeUsed = 10m
                });

                context.SaveChanges();
            }

            repository.PreloadActiveBets();

            var loaded = Assert.Single(repository._bets);
            Assert.True(loaded.IsPartialCashout);
            Assert.Single(loaded.PartialCashoutAudit);
        }
    }
}
