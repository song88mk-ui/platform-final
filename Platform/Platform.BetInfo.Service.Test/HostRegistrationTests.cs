using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.BetInfo.Service.Repository;
using Platform.Shared.Data.Betting;

namespace Platform.BetInfo.Service.Test
{
    // Builds the real host from Program.CreateHostBuilder and resolves out of it.
    //
    // GM-814 moved this service from constructing its own DbContextOptions to taking
    // IDbContextFactory<BettingContext>, which only works if Program.cs actually registers
    // the factory. A missing AddDbContextFactory would compile fine and fail at startup, in
    // a worker with no HTTP surface to notice it. These tests close that gap.
    //
    // They resolve individual services rather than starting the host: StartAsync would run
    // Worker, which connects to RabbitMQ. Resolution is enough to prove the container can
    // satisfy every constructor.
    public class HostRegistrationTests
    {
        [Fact]
        public void Host_ResolvesTheBettingContextFactory()
        {
            using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();

            Assert.NotNull(host.Services.GetRequiredService<IDbContextFactory<BettingContext>>());
        }

        [Fact]
        public void Host_ResolvesTheRepositoryWithItsFactoryInjected()
        {
            // The constructor changed shape in GM-814. This proves the container can still
            // build it, which is what would break if the registration were forgotten.
            using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();

            Assert.NotNull(host.Services.GetRequiredService<IBettingRepository>());
        }

        [Fact]
        public void Host_RegistersTheRepositoryAsASingleton()
        {
            // A singleton repository is only safe because the factory hands out a fresh
            // context per call. If the repository ever becomes scoped, or the factory is
            // swapped for a scoped DbContext, this pairing needs revisiting (TD-13).
            using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();

            Assert.Same(
                host.Services.GetRequiredService<IBettingRepository>(),
                host.Services.GetRequiredService<IBettingRepository>());
        }

        [Fact]
        public void Host_TheContextFactoryIsNotScoped()
        {
            // AddDbContextFactory registers the factory as a singleton, which is what makes
            // it injectable into the singleton repository above.
            using var host = Program.CreateHostBuilder(Array.Empty<string>()).Build();

            Assert.Same(
                host.Services.GetRequiredService<IDbContextFactory<BettingContext>>(),
                host.Services.GetRequiredService<IDbContextFactory<BettingContext>>());
        }
    }
}
