using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Operator.Servicev2.Repository;
using Platform.Shared.Data.Platform;

namespace Platform.Operator.Servicev2.Test
{
    // Builds the real host from Program.CreateHostBuilder and resolves out of it.
    //
    // GM-862 changed BettingRepository to take IDbContextFactory<PlatformContext>, which only
    // works if Program.cs registers the factory. A missing AddDbContextFactory would compile and
    // then fail at startup, in a worker with no HTTP surface to notice it. Same checks as
    // BetInfo.Service.Test's HostRegistrationTests.
    //
    // They resolve services rather than starting the host: StartAsync would run Worker, which
    // connects to RabbitMQ.
    public class HostRegistrationTests
    {
        [Fact]
        public void Host_ResolvesThePlatformContextFactoryAsASingleton()
        {
            using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();

            Assert.Same(
                host.Services.GetRequiredService<IDbContextFactory<PlatformContext>>(),
                host.Services.GetRequiredService<IDbContextFactory<PlatformContext>>());
        }

        [Fact]
        public void Host_ResolvesTheRepositoryAsASingleton()
        {
            // A singleton repository is only safe because the factory hands out a fresh context
            // per call (TD-13).
            using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();

            Assert.Same(
                host.Services.GetRequiredService<IBettingRepository>(),
                host.Services.GetRequiredService<IBettingRepository>());
        }
    }
}
