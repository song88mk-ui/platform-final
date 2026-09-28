using Microsoft.EntityFrameworkCore;
using Platform.Betting.API.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;

namespace Platform.Betting.API.Test.Repository
{
    // GM-861: CashOutBet and GetCashOut loaded a bet by BetId only, with nothing checking it
    // belongs to the caller - a player authenticated on any client could cash out (and be
    // credited for) a bet placed by another player by supplying a leaked or observed BetId.
    // Both now reject a UserId/ClientId mismatch with Bet_Not_Found before doing anything else,
    // so an unauthorized caller cannot even tell the bet exists.
    public class CashoutOwnershipTests
    {
        private static BettingContext CreateContext() =>
            new BettingContext(new DbContextOptionsBuilder<BettingContext>()
                .UseInMemoryDatabase($"betting-{Guid.NewGuid():N}")
                .Options);

        private static BettingRepository CreateRepository(BettingContext context) =>
            new BettingRepository(
                bettingContext: context,
                configuration: null!,
                memCache: null!,
                httpClientFactory: null!,
                providerFactory: null!,
                sportsbookRepository: null!,
                rabbitMQRepository: null!,
                logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<BettingRepository>.Instance);

        private const string OwnerUserId = "user-owner";
        private const string OwnerClientId = "CLIENT-A";

        private static Bet AnOwnedRunningBet(Guid betId) => new Bet
        {
            BetId = betId,
            UserId = OwnerUserId,
            ClientId = OwnerClientId,
            BetStatusId = (int)BetStatus.Running,
            BetTypeId = 0
        };

        [Fact]
        public async Task CashOutBet_UserIdDoesNotMatch_ReturnsBetNotFound()
        {
            var betId = Guid.NewGuid();
            using var context = CreateContext();
            context.Bet.Add(AnOwnedRunningBet(betId));
            context.SaveChanges();

            var repository = CreateRepository(context);

            var response = await repository.CashOutBet(new CashoutBetRequestDTO
            {
                BetId = betId,
                UserId = "someone-else",
                ClientId = OwnerClientId,
                CashOutStakePercent = 1
            });

            Assert.Equal(StatusCode.Bet_Not_Found, response.StatusCode);
        }

        [Fact]
        public async Task CashOutBet_ClientIdDoesNotMatch_ReturnsBetNotFound()
        {
            var betId = Guid.NewGuid();
            using var context = CreateContext();
            context.Bet.Add(AnOwnedRunningBet(betId));
            context.SaveChanges();

            var repository = CreateRepository(context);

            var response = await repository.CashOutBet(new CashoutBetRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = "SOME-OTHER-CLIENT",
                CashOutStakePercent = 1
            });

            Assert.Equal(StatusCode.Bet_Not_Found, response.StatusCode);
        }

        [Fact]
        public async Task CashOutBet_MatchingOwner_PassesOwnershipCheck()
        {
            var betId = Guid.NewGuid();
            using var context = CreateContext();
            // No BetSelection rows seeded - reaches CashOutBet's own
            // "no selections" Unavailable branch without needing a working
            // ISportsbookRepository, while still proving the ownership check let a matching
            // caller through instead of rejecting with Bet_Not_Found.
            context.Bet.Add(AnOwnedRunningBet(betId));
            context.SaveChanges();

            var repository = CreateRepository(context);

            var response = await repository.CashOutBet(new CashoutBetRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = OwnerClientId,
                CashOutStakePercent = 1
            });

            Assert.Equal(StatusCode.CashOut_Is_Unavailable, response.StatusCode);
        }

        [Fact]
        public async Task GetCashOut_UserIdDoesNotMatch_ReturnsBetNotFound()
        {
            var betId = Guid.NewGuid();
            using var context = CreateContext();
            context.Bet.Add(AnOwnedRunningBet(betId));
            context.SaveChanges();

            var repository = CreateRepository(context);

            var response = await repository.GetCashOut(new CashoutStateRequestDTO
            {
                BetId = betId,
                UserId = "someone-else",
                ClientId = OwnerClientId
            });

            Assert.Equal(StatusCode.Bet_Not_Found, response.StatusCode);
        }

        [Fact]
        public async Task GetCashOut_ClientIdDoesNotMatch_ReturnsBetNotFound()
        {
            var betId = Guid.NewGuid();
            using var context = CreateContext();
            context.Bet.Add(AnOwnedRunningBet(betId));
            context.SaveChanges();

            var repository = CreateRepository(context);

            var response = await repository.GetCashOut(new CashoutStateRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = "SOME-OTHER-CLIENT"
            });

            Assert.Equal(StatusCode.Bet_Not_Found, response.StatusCode);
        }

        [Fact]
        public async Task GetCashOut_MatchingOwner_PassesOwnershipCheck()
        {
            var betId = Guid.NewGuid();
            using var context = CreateContext();
            context.Bet.Add(AnOwnedRunningBet(betId));
            context.SaveChanges();

            var repository = CreateRepository(context);

            var response = await repository.GetCashOut(new CashoutStateRequestDTO
            {
                BetId = betId,
                UserId = OwnerUserId,
                ClientId = OwnerClientId
            });

            Assert.Equal(StatusCode.CashOut_Is_Unavailable, response.StatusCode);
        }
    }
}
