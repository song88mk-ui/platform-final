using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Platform;

namespace Platform.API.Test.Repository
{
    // Builds a PlatformContext backed by the EF in-memory provider, mirroring
    // Platform.Operator.API.Test/Repository/InMemoryPlatformContext.cs - see that file for why
    // in-memory (not SQLite) is the right provider for this DbContext.
    internal static class InMemoryPlatformContext
    {
        internal static PlatformContext Create() =>
            new PlatformContext(
                new DbContextOptionsBuilder<PlatformContext>()
                    .UseInMemoryDatabase($"platform-api-{Guid.NewGuid():N}")
                    .Options);
    }
}
