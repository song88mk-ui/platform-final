using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // Stands in for the IDbContextFactory<BettingContext> that BettingRepository takes, handing
    // out contexts backed by the EF in-memory provider. Same shape as BetInfo.Service.Test's
    // InMemoryBettingContextFactory and this project's InMemoryPlatformContextFactory.
    //
    // Every context shares one set of options, so they share one in-memory database: a bet seeded
    // through one context is visible to CancelBet or AcceptBet through the next, as with a real
    // database. The database name is unique per factory because in-memory databases are
    // process-global and xUnit runs test classes in parallel.
    //
    // The in-memory provider does not translate to SQL, so these tests cover the repository's
    // logic, not whether its queries run against SQL Server.
    internal sealed class InMemoryBettingContextFactory : IDbContextFactory<BettingContext>
    {
        private readonly DbContextOptions<BettingContext> _options;

        internal InMemoryBettingContextFactory()
        {
            _options = new DbContextOptionsBuilder<BettingContext>()
                .UseInMemoryDatabase($"betting-{Guid.NewGuid():N}")
                .Options;
        }

        public BettingContext CreateDbContext() => new BettingContext(_options);
    }
}
