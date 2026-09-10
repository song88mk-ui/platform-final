using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting;

namespace Platform.BetInfo.Service.Test.Repository
{
    // Stands in for the IDbContextFactory<BettingContext> that BettingRepository now takes,
    // handing out contexts backed by the EF in-memory provider. Hand-written, no mocking
    // framework, matching Platform.Shared.Test/Providers/GtsFakes.cs.
    //
    // Every context it creates shares one set of options, so they share one in-memory
    // database: writes made through an earlier context are visible to a later one, the way a
    // real database behaves. The database NAME is unique per factory instance, because
    // in-memory databases are process-global and keyed by name while xUnit runs collections
    // in parallel.
    //
    // Created counts calls so a test can prove the repository asks for a fresh context per
    // call rather than holding one. That is the property the seam has to preserve: the
    // repository is registered as a singleton, so a captured context would outlive its
    // transaction.
    //
    // On what this proves: the in-memory provider is LINQ-to-Objects and does not translate
    // to SQL, so these tests cover the repository's logic and not whether its queries would
    // run against SQL Server.
    internal sealed class InMemoryBettingContextFactory : IDbContextFactory<BettingContext>
    {
        private readonly DbContextOptions<BettingContext> _options;

        internal InMemoryBettingContextFactory()
        {
            _options = new DbContextOptionsBuilder<BettingContext>()
                .UseInMemoryDatabase($"betting-{Guid.NewGuid():N}")
                .Options;
        }

        internal int Created { get; private set; }

        public BettingContext CreateDbContext()
        {
            Created++;
            return new BettingContext(_options);
        }
    }
}
