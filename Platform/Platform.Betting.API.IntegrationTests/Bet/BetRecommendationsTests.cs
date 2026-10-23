using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Platform.Betting.API.IntegrationTests.TestSupport;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Data;
using System.Net;
using System.Text.Json;

namespace Platform.Betting.API.IntegrationTests.Bet
{
    // v1/bet/recommendations runs the GetTopMarketBets stored procedure through FromSqlRaw
    // (BettingRepository.GetBetRecommendations), then posts the ranked markets to Sportsbook.API.
    // EF InMemory cannot run the procedure, so this path had no test before DEVOPS-408. The stub
    // records what was posted, which is the procedure's output after BettingRepository maps it.
    [Collection(BettingApiCollection.Name)]
    public class BetRecommendationsTests
    {
        private const string SportsbookPath = "/v2/sportsbook/betrecommendations";

        private readonly BettingApiFixture _fixture;
        private readonly HttpClient _client;

        public BetRecommendationsTests(BettingApiFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.CreateClient();

            fixture.Stub.SetResponse(SportsbookPath, JsonSerializer.Serialize(
                new BaseResponseDTO<BetRecommendationsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new BetRecommendationsResponseDTO()
                }));
        }

        private static ClientTopMarket TopMarket(Guid clientId, string currency, long marketId, int betCount) => new()
        {
            ClientId = clientId.ToString().ToUpper(),
            ClientCode = "IT",
            ClientName = "Integration Test",
            CurrencyCode = currency,
            FixtureId = 1000 + marketId,
            MarketId = marketId,
            MarketStatusId = 1,
            BetCount = betCount
        };

        private async Task SeedAsync(params ClientTopMarket[] rows) =>
            await _fixture.WithBettingContextAsync(async context =>
            {
                context.ClientTopMarket.AddRange(rows);
                await context.SaveChangesAsync();
            });

        private BetRecommendationDetailRequestDTO SentToSportsbookFor(Guid clientId) =>
            _fixture.Stub.RequestsTo(SportsbookPath)
                .Select(r => JsonSerializer.Deserialize<BetRecommendationDetailRequestDTO>(r.Body, ApiResponse.Json)!)
                .Single(r => r.ClientId == clientId);

        [Fact]
        public async Task Recommendations_RanksClientMarketsInCurrencyByBetCount()
        {
            // Arrange: three EUR markets for this client, plus rows the procedure must leave out.
            var clientId = Guid.NewGuid();
            await SeedAsync(
                TopMarket(clientId, "EUR", marketId: 1, betCount: 5),
                TopMarket(clientId, "EUR", marketId: 2, betCount: 50),
                TopMarket(clientId, "EUR", marketId: 3, betCount: 20),
                TopMarket(clientId, "USD", marketId: 4, betCount: 999),
                TopMarket(Guid.NewGuid(), "EUR", marketId: 5, betCount: 999));

            // Act: lower case, as BettingRepository upper-cases the currency before the call.
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/recommendations", new BetRecommendationRequestDTO
            {
                ClientId = clientId,
                ClientCode = "IT",
                CurrencyCode = "eur"
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, http.StatusCode);
            Assert.Equal(StatusCode.Success, body.StatusCode);

            var markets = SentToSportsbookFor(clientId).Markets;
            Assert.Equal([2L, 3L, 1L], markets.Select(m => m.MarketId));
            Assert.Equal([1, 2, 3], markets.Select(m => m.SortOrder));
            Assert.Equal([1002L, 1003L, 1001L], markets.Select(m => m.FixtureId));
        }

        [Fact]
        public async Task Recommendations_SendsAtMostFiftyMarkets()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            await SeedAsync(Enumerable.Range(1, 51)
                .Select(i => TopMarket(clientId, "EUR", marketId: i, betCount: i))
                .ToArray());

            // Act
            var (http, _) = await ApiResponse.PostAsync(_client, "v1/bet/recommendations", new BetRecommendationRequestDTO
            {
                ClientId = clientId,
                ClientCode = "IT",
                CurrencyCode = "EUR"
            });

            // Assert: the 50 highest BetCounts, so market 1 (BetCount 1) is the one dropped.
            Assert.Equal(HttpStatusCode.OK, http.StatusCode);
            var markets = SentToSportsbookFor(clientId).Markets;
            Assert.Equal(50, markets.Count);
            Assert.DoesNotContain(markets, m => m.MarketId == 1);
        }

        // GM-928: MarketStatusId and BetCount are NULL-able in the table but not in the entity, so the NULL row is
        // inserted with raw SQL. A NULL status or count is harmless. FixtureId and MarketId are NOT NULL since GM-929.
        [Theory]
        [InlineData("MarketStatusId")]
        [InlineData("BetCount")]
        public async Task Recommendations_RowWithNullColumn_DoesNotFail(string nullColumn)
        {
            // Arrange: one complete row (market 1) and one row with nullColumn set to NULL (market 2).
            var clientId = Guid.NewGuid();
            var clientKey = clientId.ToString().ToUpper();
            await SeedAsync(TopMarket(clientId, "EUR", marketId: 1, betCount: 5));
            SqlParameter Column(string column, SqlDbType type, object value) =>
                new("@" + column, type) { Value = column == nullColumn ? DBNull.Value : value };
            await _fixture.WithBettingContextAsync(context =>
                context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO ClientTopMarket (ClientId, ClientCode, ClientName, CurrencyCode, FixtureId, MarketId, MarketStatusId, BetCount) " +
                    "VALUES (@ClientId, 'IT', 'Integration Test', 'EUR', @FixtureId, @MarketId, @MarketStatusId, @BetCount)",
                    new SqlParameter("@ClientId", clientKey),
                    Column("FixtureId", SqlDbType.BigInt, 1002L),
                    Column("MarketId", SqlDbType.BigInt, 2L),
                    Column("MarketStatusId", SqlDbType.Int, 1),
                    Column("BetCount", SqlDbType.Int, 50)));

            // Act
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/recommendations", new BetRecommendationRequestDTO
            {
                ClientId = clientId,
                ClientCode = "IT",
                CurrencyCode = "EUR"
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, http.StatusCode);
            Assert.Equal(StatusCode.Success, body.StatusCode);

            var marketIds = SentToSportsbookFor(clientId).Markets.Select(m => m.MarketId).ToList();
            Assert.Contains(1L, marketIds);
            Assert.Contains(2L, marketIds);
        }

        private Task InsertRawAsync(string clientKey, object fixtureId, object marketId) =>
            _fixture.WithBettingContextAsync(context =>
                context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO ClientTopMarket (ClientId, ClientCode, ClientName, CurrencyCode, FixtureId, MarketId, MarketStatusId, BetCount) " +
                    "VALUES (@ClientId, 'IT', 'Integration Test', 'EUR', @FixtureId, @MarketId, 1, 50)",
                    new SqlParameter("@ClientId", clientKey),
                    new SqlParameter("@FixtureId", SqlDbType.BigInt) { Value = fixtureId },
                    new SqlParameter("@MarketId", SqlDbType.BigInt) { Value = marketId }));

        // GM-929: the table had no key, so a second row with the same (FixtureId, MarketId, ClientId, CurrencyCode)
        // was accepted and v1/bet/recommendations sent that market twice. The primary key now rejects it.
        [Fact]
        public async Task ClientTopMarket_RejectsDuplicateKey()
        {
            // Arrange: currency in another case, as the key compares case-insensitively.
            var clientId = Guid.NewGuid();
            await SeedAsync(TopMarket(clientId, "eur", marketId: 1, betCount: 5));

            // Act
            var ex = await Assert.ThrowsAsync<SqlException>(() => InsertRawAsync(clientId.ToString().ToUpper(), 1001L, 1L));

            // Assert: 2627 is a PRIMARY KEY violation.
            Assert.Equal(2627, ex.Number);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ClientTopMarket_RejectsNullFixtureOrMarket(bool nullFixture)
        {
            // Act
            var ex = await Assert.ThrowsAsync<SqlException>(() => InsertRawAsync(
                Guid.NewGuid().ToString().ToUpper(),
                nullFixture ? DBNull.Value : 1001L,
                nullFixture ? 1L : DBNull.Value));

            // Assert: 515 is "cannot insert the value NULL".
            Assert.Equal(515, ex.Number);
        }
    }
}
