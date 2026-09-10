using Microsoft.EntityFrameworkCore;
using Platform.BetInfo.Service.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.MessageModels;

namespace Platform.BetInfo.Service.Test.Repository
{
    // Covers BettingRepository.ProcessBetPlacement, the single method of this worker: it
    // records the user agent that placed a bet, consumed from OnBetPlacement off RabbitMQ.
    //
    // The repository became testable in GM-814, when it stopped building its own
    // DbContextOptions with a hardcoded UseSqlServer and started taking
    // IDbContextFactory<BettingContext>. These tests exist as much to hold that seam in place
    // as to cover the two lines of logic behind it.
    public class BettingRepositoryTests
    {
        private static OnBetPlacement APlacement(Guid betId, string userAgent) =>
            new OnBetPlacement
            {
                Bet = new Bet { BetId = betId },
                UserAgent = userAgent
            };

        [Fact]
        public void ProcessBetPlacement_WritesTheBetIdAndUserAgent()
        {
            var betId = Guid.NewGuid();
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            repository.ProcessBetPlacement(APlacement(betId, "Mozilla/5.0 (iPhone)"));

            using var context = factory.CreateDbContext();
            var stored = Assert.Single(context.BetInfo);
            Assert.Equal(betId, stored.BetId);
            Assert.Equal("Mozilla/5.0 (iPhone)", stored.UserAgent);
        }

        [Fact]
        public void ProcessBetPlacement_CommitsBeforeReturning()
        {
            // scope.Complete() runs inside the context's using block, so the row has to be
            // visible to a context created afterwards. If the TransactionScope were completed
            // in the wrong place, or not at all, this is what would catch it.
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            repository.ProcessBetPlacement(APlacement(Guid.NewGuid(), "agent"));

            using var context = factory.CreateDbContext();
            Assert.Single(context.BetInfo);
        }

        [Fact]
        public void ProcessBetPlacement_TakesAFreshContextPerCall()
        {
            // The seam's contract. This repository is registered as a singleton
            // (Platform.BetInfo.Service/Program.cs), so holding one context would outlive the
            // transaction it was created for. Each call must ask the factory again.
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            repository.ProcessBetPlacement(APlacement(Guid.NewGuid(), "first"));
            repository.ProcessBetPlacement(APlacement(Guid.NewGuid(), "second"));

            Assert.Equal(2, factory.Created);
        }

        [Fact]
        public void ProcessBetPlacement_CalledTwice_KeepsBothRows()
        {
            // Two contexts, one database. Proves the seam did not silently isolate each call
            // into its own store, which would look like a passing test but lose data.
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            repository.ProcessBetPlacement(APlacement(first, "first"));
            repository.ProcessBetPlacement(APlacement(second, "second"));

            using var context = factory.CreateDbContext();
            Assert.Equal(2, context.BetInfo.Count());
            Assert.Contains(context.BetInfo, b => b.BetId == first && b.UserAgent == "first");
            Assert.Contains(context.BetInfo, b => b.BetId == second && b.UserAgent == "second");
        }

        [Fact]
        public void ProcessBetPlacement_SameBetTwice_WritesTwoRows()
        {
            // BetInfo's key is Id, not BetId, so a redelivered message inserts again rather
            // than failing or updating. The consumer uses manual ack (Worker.cs), which makes
            // redelivery a real possibility. Pinned as current behaviour.
            var betId = Guid.NewGuid();
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            repository.ProcessBetPlacement(APlacement(betId, "agent"));
            repository.ProcessBetPlacement(APlacement(betId, "agent"));

            using var context = factory.CreateDbContext();
            Assert.Equal(2, context.BetInfo.Count());
        }

        [Fact]
        public void ProcessBetPlacement_AssignsGeneratedIds()
        {
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            repository.ProcessBetPlacement(APlacement(Guid.NewGuid(), "first"));
            repository.ProcessBetPlacement(APlacement(Guid.NewGuid(), "second"));

            using var context = factory.CreateDbContext();
            var ids = context.BetInfo.Select(b => b.Id).ToList();
            Assert.Equal(2, ids.Distinct().Count());
            Assert.DoesNotContain(0L, ids);
        }

        [Fact]
        public void ProcessBetPlacement_EmptyUserAgent_IsStoredAsIs()
        {
            // OnBetPlacement.UserAgent defaults to string.Empty and the repository does not
            // guard it, so a placement with no agent still produces a row.
            var betId = Guid.NewGuid();
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            repository.ProcessBetPlacement(APlacement(betId, string.Empty));

            using var context = factory.CreateDbContext();
            Assert.Equal(string.Empty, Assert.Single(context.BetInfo).UserAgent);
        }

        [Fact]
        public void ProcessBetPlacement_NullBet_ThrowsAndWritesNothing()
        {
            // The repository dereferences message.Bet without a guard. Worker.cs catches and
            // logs the exception rather than acking, so the message is not lost. Pinned so
            // that adding a guard is a deliberate change with a visible test.
            var factory = new InMemoryBettingContextFactory();
            var repository = new BettingRepository(factory);

            Assert.Throws<NullReferenceException>(
                () => repository.ProcessBetPlacement(new OnBetPlacement { Bet = null, UserAgent = "agent" }));

            using var context = factory.CreateDbContext();
            Assert.Empty(context.BetInfo);
        }
    }
}
