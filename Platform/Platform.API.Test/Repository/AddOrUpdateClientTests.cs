using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.API.Repository;
using Platform.API.Telemetry;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;

namespace Platform.API.Test.Repository
{
    // GM-1001: PlatformRepository.AddOrUpdateClient (Platform.API/Repository/PlatformRepository.cs)
    // must stamp CreatedDate on insert only, honour a caller-supplied ClientId for a new client,
    // reject a ClientId that already exists, and never change an existing client's ClientId or
    // CreatedDate. Backoffice's CSV import relies on all four.
    public class AddOrUpdateClientTests
    {
        // AddOrUpdateClient touches only the PlatformContext; every other dependency stays unused.
        private static PlatformRepository CreateRepository(PlatformContext context) =>
            new PlatformRepository(
                operatorRepository: null!,
                bettingRepository: null!,
                sportsbookRepository: null!,
                casinoRepository: null!,
                webPropsRepository: null!,
                platformContext: context,
                memCache: null!,
                configuration: new ConfigurationBuilder().Build(),
                messageBroker: null!,
                instrumentation: new Instrumentation(),
                logger: NullLogger<PlatformRepository>.Instance);

        private static ClientRequestDTO NewClientRequest(int clientIdRef, Guid clientId = default) => new ClientRequestDTO
        {
            ClientId = clientId,
            ClientCode = $"C{clientIdRef}",
            ClientName = $"Client {clientIdRef}",
            ClientIdRef = clientIdRef,
            IsActive = true,
            LimitId = "10939",
            Endpoint = "https://operator.example.com",
            MonthlyFee = 500m,
            CurrentCompanyShare = 0.1m
        };

        [Fact]
        public async Task NewClient_WithoutClientId_GetsAGeneratedIdAndCreatedDate()
        {
            var context = InMemoryPlatformContext.Create();
            var before = DateTime.UtcNow;

            var result = await CreateRepository(context).AddOrUpdateClient(NewClientRequest(1));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.NotEqual(Guid.Empty, result.Result.ClientId);
            var saved = context.Client.Single();
            Assert.Equal(result.Result.ClientId, saved.ClientId);
            Assert.NotNull(saved.CreatedDate);
            Assert.True(saved.CreatedDate >= before);
        }

        [Fact]
        public async Task NewClient_WithClientId_UsesThatId()
        {
            var context = InMemoryPlatformContext.Create();
            var chosen = Guid.Parse("6989B427-4234-4AC9-4791-08DEFEC92A2C");

            var result = await CreateRepository(context).AddOrUpdateClient(NewClientRequest(1, chosen));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(chosen, result.Result.ClientId);
            Assert.Equal(chosen, context.Client.Single().ClientId);
        }

        [Fact]
        public async Task NewClient_WithClientIdAlreadyUsed_IsRejectedAndNothingIsInserted()
        {
            var context = InMemoryPlatformContext.Create();
            var taken = Guid.NewGuid();
            context.Client.Add(new Client { ClientId = taken, ClientCode = "OLD", ClientName = "Old", ClientIdRef = 1, LimitId = "1", Endpoint = "" });
            context.SaveChanges();

            var result = await CreateRepository(context).AddOrUpdateClient(NewClientRequest(2, taken));

            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.Contains(taken.ToString(), result.Message);
            Assert.Single(context.Client);
            Assert.Equal("OLD", context.Client.Single().ClientCode);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExistingClient_KeepsItsClientIdAndCreatedDate(bool hadCreatedDate)
        {
            var context = InMemoryPlatformContext.Create();
            var originalId = Guid.NewGuid();
            DateTime? originalCreated = hadCreatedDate ? new DateTime(2026, 3, 1, 1, 54, 43, DateTimeKind.Utc) : null;
            context.Client.Add(new Client { ClientId = originalId, ClientCode = "C1", ClientName = "Client 1", ClientIdRef = 1, LimitId = "10939", Endpoint = "", CreatedDate = originalCreated, EffectiveDate = DateTime.UtcNow.AddYears(-1) });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            // An update that even tries to send a different ClientId.
            var request = NewClientRequest(1, Guid.NewGuid());
            request.ClientName = "Client 1 Renamed";
            var result = await CreateRepository(context).AddOrUpdateClient(request);

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(originalId, result.Result.ClientId);
            context.ChangeTracker.Clear();
            var saved = context.Client.Single();
            Assert.Equal(originalId, saved.ClientId);
            Assert.Equal("Client 1 Renamed", saved.ClientName);
            Assert.Equal(originalCreated, saved.CreatedDate);
        }
    }
}
