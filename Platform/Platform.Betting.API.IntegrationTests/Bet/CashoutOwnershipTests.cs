using Platform.Betting.API.IntegrationTests.TestSupport;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Net;
using BetEntity = Platform.Shared.Data.Betting.Entities.Bet;

namespace Platform.Betting.API.IntegrationTests.Bet
{
    // GM-861 ownership checks on v1/bet/cashout and v1/bet/cashout/state, end to end: HTTP request,
    // model binding, BettingRepository, and a Bet row in a real Betting database. The unit tests in
    // Platform.Betting.API.Test cover the same rules against EF InMemory.
    [Collection(BettingApiCollection.Name)]
    public class CashoutOwnershipTests
    {
        private const string OwnerUserId = "user-owner";

        private readonly BettingApiFixture _fixture;
        private readonly HttpClient _client;

        public CashoutOwnershipTests(BettingApiFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.CreateClient();
        }

        // The Bet table has ten NOT NULL columns; EF InMemory needs none of them set.
        private async Task<(Guid BetId, string ClientId)> SeedRunningBetAsync()
        {
            var bet = new BetEntity
            {
                BetId = Guid.NewGuid(),
                UserId = OwnerUserId,
                ClientId = Guid.NewGuid().ToString().ToUpper(),
                BetStatusId = (int)BetStatus.Running,
                BetStatusName = BetStatus.Running.ToString(),
                BetTypeId = 0,
                OddsTypeName = "Decimal",
                CurrencyCode = "EUR"
            };

            await _fixture.WithBettingContextAsync(async context =>
            {
                context.Bet.Add(bet);
                await context.SaveChangesAsync();
            });

            return (bet.BetId, bet.ClientId);
        }

        [Fact]
        public async Task Cashout_UserIdDoesNotMatch_ReturnsBetNotFound()
        {
            // Arrange
            var (betId, clientId) = await SeedRunningBetAsync();

            // Act
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/cashout", new CashoutBetRequestDTO
            {
                BetId = betId,
                UserId = "someone-else",
                ClientId = clientId,
                CashOutStakePercent = 1
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
            Assert.Equal(StatusCode.Bet_Not_Found, body.StatusCode);
        }

        [Fact]
        public async Task Cashout_ClientIdDoesNotMatch_ReturnsBetNotFound()
        {
            // Arrange
            var (betId, _) = await SeedRunningBetAsync();

            // Act
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/cashout", new CashoutBetRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = "SOME-OTHER-CLIENT",
                CashOutStakePercent = 1
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
            Assert.Equal(StatusCode.Bet_Not_Found, body.StatusCode);
        }

        [Fact]
        public async Task Cashout_MatchingOwner_PassesOwnershipCheck()
        {
            // Arrange: no BetSelection rows, so CashOutBet stops at its own "no selections" branch.
            var (betId, clientId) = await SeedRunningBetAsync();

            // Act
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/cashout", new CashoutBetRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = clientId,
                CashOutStakePercent = 1
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
            Assert.Equal(StatusCode.CashOut_Is_Unavailable, body.StatusCode);
        }

        [Fact]
        public async Task CashoutState_UserIdDoesNotMatch_ReturnsBetNotFound()
        {
            // Arrange
            var (betId, clientId) = await SeedRunningBetAsync();

            // Act
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/cashout/state", new CashoutStateRequestDTO
            {
                BetId = betId,
                UserId = "someone-else",
                ClientId = clientId
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
            Assert.Equal(StatusCode.Bet_Not_Found, body.StatusCode);
        }

        [Fact]
        public async Task CashoutState_ClientIdDoesNotMatch_ReturnsBetNotFound()
        {
            // Arrange
            var (betId, _) = await SeedRunningBetAsync();

            // Act
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/cashout/state", new CashoutStateRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = "SOME-OTHER-CLIENT"
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
            Assert.Equal(StatusCode.Bet_Not_Found, body.StatusCode);
        }

        [Fact]
        public async Task CashoutState_MatchingOwner_PassesOwnershipCheck()
        {
            // Arrange
            var (betId, clientId) = await SeedRunningBetAsync();

            // Act
            var (http, body) = await ApiResponse.PostAsync(_client, "v1/bet/cashout/state", new CashoutStateRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = clientId
            });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, http.StatusCode);
            Assert.Equal(StatusCode.CashOut_Is_Unavailable, body.StatusCode);
        }
    }
}
