using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Data.Betting;
using Testcontainers.MsSql;

namespace Platform.Betting.API.IntegrationTests.TestSupport
{
    /// <summary>
    /// One SQL Server container with the full dev schema, one stub for outbound HTTP, and
    /// Platform.Betting.API booted in process against both. Shared by every test in the assembly.
    /// </summary>
    /// <remarks>
    /// Starting the container and replaying the migrations takes tens of seconds, so it happens
    /// once. Tests isolate themselves with fresh BetIds and ClientIds rather than by resetting the
    /// database.
    /// </remarks>
    public sealed class BettingApiFixture : IAsyncLifetime
    {
        // The API URL keys BettingRepository and the MTS/GTS providers read (Platform.Betting.API
        // and Platform.Shared). Each one goes to the stub, so no test can reach a real service.
        private static readonly string[] ApiUrlKeys =
        [
            "API:MTSAuthAPI", "API:MTSMaxStakeAPI", "API:MTSProbabilityAPI",
            "API:GTSAuthAPI", "API:GTSAPI", "API:RabbitMQAPI", "API:PlatformSportsbookAPI"
        ];

        private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

        public StubHttpServer Stub { get; } = new();

        public WebApplicationFactory<Startup> Factory { get; private set; } = null!;

        public string BettingConnectionString { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await _sql.StartAsync();
            await FlywayMigrations.ApplyAsync(_sql.GetConnectionString());

            BettingConnectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString())
            {
                InitialCatalog = "Betting"
            }.ConnectionString;

            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Betting"] = BettingConnectionString,
                ["Custom:IsProviderEnabled"] = "false"
            };
            foreach (var key in ApiUrlKeys)
                settings[key] = Stub.BaseUrl;

            Factory = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
                // Added last, so it wins over appsettings.json and environment variables.
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings)));
        }

        public HttpClient CreateClient() => Factory.CreateClient();

        /// <summary>Runs <paramref name="action"/> with a BettingContext on the container, for seeding rows.</summary>
        public async Task WithBettingContextAsync(Func<BettingContext, Task> action)
        {
            using var scope = Factory.Services.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<BettingContext>());
        }

        public async Task DisposeAsync()
        {
            if (Factory != null)
                await Factory.DisposeAsync();
            Stub.Dispose();
            await _sql.DisposeAsync();
        }
    }

    [CollectionDefinition(Name)]
    public sealed class BettingApiCollection : ICollectionFixture<BettingApiFixture>
    {
        public const string Name = "Betting API against SQL Server";
    }
}
