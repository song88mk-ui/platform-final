using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Platform;

namespace Platform.Operator.API.Test.Repository
{
    // Builds a PlatformContext backed by the EF in-memory provider.
    //
    // This is possible without touching production code because PlatformContext takes its
    // DbContextOptions from outside and never overrides OnConfiguring, so the provider is
    // entirely the caller's choice (Platform.Shared/Data/Platform/PlatformContext.cs:9).
    //
    // WHAT THESE TESTS PROVE, AND WHAT THEY DO NOT:
    // The in-memory provider is LINQ-to-Objects. It executes any expression the C# compiler
    // accepts, including ones SQL Server could not translate. So a passing test here proves
    // the repository's LOGIC - branch selection, status codes, balance arithmetic - and says
    // nothing about whether the query would run against the real database. Query translation
    // stays uncovered; only an integration test against SQL Server could close that.
    //
    // It was chosen over the SQLite provider on purpose. This model carries 193
    // decimal(26,12) columns and the repositories aggregate them, and SQLite has no native
    // decimal type. Money arithmetic here has to be exact, and in-memory keeps a real
    // System.Decimal. The usual argument for SQLite - that it enforces relational
    // constraints - does not apply: PlatformContext.OnModelCreating declares primary keys
    // and nothing else, so there are no constraints for a relational provider to enforce.
    internal static class InMemoryPlatformContext
    {
        // A distinct database name per call. In-memory databases are process-global and keyed
        // by name, while xUnit runs test collections in parallel, so a shared name would let
        // one test observe another's rows.
        internal static PlatformContext Create() =>
            new PlatformContext(
                new DbContextOptionsBuilder<PlatformContext>()
                    .UseInMemoryDatabase($"platform-{Guid.NewGuid():N}")
                    .Options);
    }
}
