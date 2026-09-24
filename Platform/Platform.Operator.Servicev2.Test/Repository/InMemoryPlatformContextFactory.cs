using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Platform;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // Stands in for the IDbContextFactory<PlatformContext> that BettingRepository takes, handing
    // out contexts backed by the EF in-memory provider. Same shape as BetInfo.Service.Test's
    // InMemoryBettingContextFactory.
    //
    // Every context shares one set of options, so they share one in-memory database: a retry row
    // written through one context is visible to the next, as with a real database. The database
    // name is unique per factory because in-memory databases are process-global and xUnit runs
    // test classes in parallel.
    //
    // The in-memory provider does not translate to SQL, so these tests cover the repository's
    // logic, not whether its queries run against SQL Server.
    internal sealed class InMemoryPlatformContextFactory : IDbContextFactory<PlatformContext>
    {
        private readonly DbContextOptions<PlatformContext> _options;

        internal InMemoryPlatformContextFactory()
        {
            _options = new DbContextOptionsBuilder<PlatformContext>()
                .UseInMemoryDatabase($"platform-{Guid.NewGuid():N}")
                .Options;
        }

        public PlatformContext CreateDbContext() => new PlatformContext(_options);
    }
}
