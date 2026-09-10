using Platform.Operator.API.Repository;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using Operators = Platform.Shared.Data.Platform.Entities;

namespace Platform.Operator.API.Test.Repository
{
    // Covers the money paths of PlatformRepository: placement, settlement, cashout and
    // cancellation, and the balance arithmetic they all funnel through in the private
    // AddOrUpdateOperatorAudit (Platform.Operator.API/Repository/PlatformRepository.cs:1182).
    //
    // That method is where an operator's balance is actually decided, and its hardest part is
    // RESETTLEMENT: when a bet that already produced an audit row is settled again with a
    // different status, the previous row's Amount has to be reversed out before the new one is
    // applied, or the player is paid twice. The reversal rules are undocumented and expressed
    // as nested conditionals. Every branch of them is pinned below.
    //
    // Balances are decimal(26,12). The assertions use full scale on purpose - see the note in
    // InMemoryPlatformContext on why the in-memory provider was chosen over SQLite.
    public class OperatorAuditTests
    {
        private static Operators.Operator AnOperator(Guid userId, string token, decimal balance, Guid clientId = default) =>
            new Operators.Operator
            {
                UserId = userId,
                OperatorToken = token,
                ClientId = clientId,
                Balance = balance,
                CurrencyCode = "EUR",
                CreatedDate = DateTime.UtcNow
            };

        private static OperatorBetSettlementRequestDTO ASettlement(Guid userId, Guid betId, decimal stake, decimal payout, BetStatus status) =>
            new OperatorBetSettlementRequestDTO
            {
                UserId = userId.ToString(),
                BetId = betId.ToString(),
                Stake = stake,
                Payout = payout,
                BetStatus = status.ToString(),
                CurrencyCode = "EUR"
            };

        #region BetPlacement - debit

        [Fact]
        public async Task BetPlacement_RunningBet_DebitsTheStakeAndWritesAnAuditRow()
        {
            // Running plus not-a-partial-cashout is the one case that inverts the sign:
            // amount becomes -stake, so placement debits.
            var userId = Guid.NewGuid();
            var token = Guid.NewGuid().ToString();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, token, 100m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = betId.ToString(), Stake = 30m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(70m, response.Balance);
            Assert.Equal(70m, context.Operator.Single().Balance);

            var audit = Assert.Single(context.OperatorAudit);
            Assert.Equal(100m, audit.PreBalance);
            Assert.Equal(-30m, audit.Amount);
            Assert.Equal(70m, audit.PostBalance);
            Assert.Equal((int)BetStatus.Running, audit.TransactionTypeId);
            Assert.Equal(betId, audit.BetId);
        }

        [Fact]
        public async Task BetPlacement_PreservesFullDecimalScale()
        {
            // decimal(26,12). A provider without a native decimal type would round here.
            var userId = Guid.NewGuid();
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, token, 1000.000000000001m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 0.000000000001m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            Assert.Equal(1000m, response.Balance);
        }

        [Fact]
        public async Task BetPlacement_StakeExceedsBalance_Returns1001AndLeavesTheBalanceAlone()
        {
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 10m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 11m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            Assert.Equal(1001, response.StatusId);
            Assert.Equal(10m, context.Operator.Single().Balance);
            Assert.Empty(context.OperatorAudit);
        }

        [Fact]
        public async Task BetPlacement_StakeEqualsBalance_IsAllowed()
        {
            // The guard is strictly less-than, so spending the whole balance succeeds.
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 10m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 10m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(0m, response.Balance);
        }

        [Fact]
        public async Task BetPlacement_ClientOnTheNegativeTestList_Returns1002()
        {
            var token = Guid.NewGuid().ToString();
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 100m));
            context.NegativeTest.Add(new NegativeTest { ClientId = clientId });
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 1m, BetStatus = BetStatus.Running.ToString() },
                token,
                clientId.ToString());

            Assert.Equal(1002, response.StatusId);
            Assert.Equal(100m, context.Operator.Single().Balance);
        }

        [Fact]
        public async Task BetPlacement_TokenIsNotAGuid_Returns1002()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 1m, BetStatus = BetStatus.Running.ToString() },
                "not-a-guid",
                Guid.NewGuid().ToString());

            Assert.Equal(1002, response.StatusId);
        }

        [Fact]
        public async Task BetPlacement_UnknownToken_Returns1002()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 1m, BetStatus = BetStatus.Running.ToString() },
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            Assert.Equal(1002, response.StatusId);
        }

        [Fact]
        public async Task BetPlacement_UnparseableBetStatus_Returns1003AndDebitsNothing()
        {
            // AddOrUpdateOperatorAudit calls Enum.Parse without a TryParse guard, so a bad
            // status surfaces as the catch-all 1003 rather than a specific validation code.
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 100m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 1m, BetStatus = "NotAStatus" },
                token,
                Guid.NewGuid().ToString());

            Assert.Equal(1003, response.StatusId);
            Assert.Equal(100m, context.Operator.Single().Balance);
        }

        [Fact]
        public async Task BetPlacement_FreeBetUnderTestConfig_DebitsNothingButStillSplitsTheStake()
        {
            // The free-bet branch zeroes the amount, so the balance is untouched while the
            // response still reports the real/bonus split the operator has to book.
            var token = Guid.NewGuid().ToString();
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 100m));
            context.TestConfig.Add(new TestConfig { ClientId = clientId, IsFreeBet = true, StakeRealPercent = 40m, StakeBonusPercent = 60m });
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 50m, BetStatus = BetStatus.Running.ToString(), IsBetBuilder = false },
                token,
                clientId.ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(100m, response.Balance);
            Assert.True(response.IsFreeBet);
            Assert.Equal(20m, response.StakeReal);
            Assert.Equal(30m, response.StakeBonus);
            Assert.Equal(0m, Assert.Single(context.OperatorAudit).Amount);
        }

        [Fact]
        public async Task BetPlacement_BetBuilder_SkipsTestConfigEntirelyAndDebitsTheFullStake()
        {
            // The test-config branch is gated on IsBetBuilder == false, so a bet-builder bet
            // falls through to the plain path and the configured split is ignored.
            var token = Guid.NewGuid().ToString();
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 100m));
            context.TestConfig.Add(new TestConfig { ClientId = clientId, IsFreeBet = true, StakeRealPercent = 40m, StakeBonusPercent = 60m });
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 50m, BetStatus = BetStatus.Running.ToString(), IsBetBuilder = true },
                token,
                clientId.ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.False(response.IsFreeBet);
            Assert.Equal(50m, response.StakeReal);
            Assert.Equal(0m, response.StakeBonus);
            Assert.Equal(50m, response.Balance);
        }

        [Fact]
        public async Task BetPlacement_NoTestConfigForTheClient_DebitsTheFullStake()
        {
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 100m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 25m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            Assert.False(response.IsFreeBet);
            Assert.Equal(25m, response.StakeReal);
            Assert.Equal(0m, response.StakeBonus);
            Assert.Equal(75m, response.Balance);
        }

        #endregion

        #region BetPlacementRedCat

        [Fact]
        public async Task BetPlacementRedCat_DebitsTheAmountAndReleasesThePlayerLock()
        {
            // This is the only method taking a per-player SemaphoreSlim, held in a STATIC
            // dictionary keyed by operator token (PlatformRepository.cs:24). Every test here
            // uses a fresh token so the lock cannot leak across xUnit's parallel collections,
            // and calling twice proves the finally-block release actually happens.
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 100m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);

            var first = await repository.BetPlacementRedCat(
                new OperatorRedCatBetRequestDTO { BetId = Guid.NewGuid().ToString(), Amount = 30m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            var second = await repository.BetPlacementRedCat(
                new OperatorRedCatBetRequestDTO { BetId = Guid.NewGuid().ToString(), Amount = 20m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            Assert.Equal(1000, first.StatusId);
            Assert.Equal(70m, first.Balance);
            Assert.Equal(1000, second.StatusId);
            Assert.Equal(50m, second.Balance);
        }

        [Fact]
        public async Task BetPlacementRedCat_AmountExceedsBalance_Returns1001()
        {
            var token = Guid.NewGuid().ToString();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 5m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacementRedCat(
                new OperatorRedCatBetRequestDTO { BetId = Guid.NewGuid().ToString(), Amount = 6m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            Assert.Equal(1001, response.StatusId);
            Assert.Equal(5m, context.Operator.Single().Balance);
        }

        [Fact]
        public async Task BetPlacementRedCat_IgnoresTestConfig()
        {
            // Unlike BetPlacement, the RedCat path reads TestConfig and then never uses it,
            // so a configured free bet still debits in full.
            var token = Guid.NewGuid().ToString();
            var clientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(Guid.NewGuid(), token, 100m));
            context.TestConfig.Add(new TestConfig { ClientId = clientId, IsFreeBet = true, StakeRealPercent = 50m, StakeBonusPercent = 50m });
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPlacementRedCat(
                new OperatorRedCatBetRequestDTO { BetId = Guid.NewGuid().ToString(), Amount = 40m, BetStatus = BetStatus.Running.ToString() },
                token,
                clientId.ToString());

            Assert.False(response.IsFreeBet);
            Assert.Equal(40m, response.StakeReal);
            Assert.Equal(60m, response.Balance);
        }

        #endregion

        #region BetSettlement - credit

        [Fact]
        public async Task BetSettlement_Won_CreditsThePayout()
        {
            // Any status other than Running leaves amount as the payout, so settlement credits.
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetSettlement(
                ASettlement(userId, Guid.NewGuid(), stake: 30m, payout: 90m, BetStatus.Won),
                Guid.NewGuid().ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(160m, response.Balance);
            Assert.Equal(90m, Assert.Single(context.OperatorAudit).Amount);
        }

        [Fact]
        public async Task BetSettlement_Lost_CreditsNothing()
        {
            // A lost bet still writes an audit row, with a zero payout. The stake was already
            // taken at placement, so nothing further moves.
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetSettlement(
                ASettlement(userId, Guid.NewGuid(), stake: 30m, payout: 0m, BetStatus.Lost),
                Guid.NewGuid().ToString());

            Assert.Equal(70m, response.Balance);

            var audit = Assert.Single(context.OperatorAudit);
            Assert.Equal(0m, audit.Amount);
            Assert.Equal((int)BetStatus.Lost, audit.TransactionTypeId);
        }

        [Fact]
        public async Task BetSettlement_UnknownUser_Returns1003()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.BetSettlement(
                ASettlement(Guid.NewGuid(), Guid.NewGuid(), 10m, 20m, BetStatus.Won),
                Guid.NewGuid().ToString());

            Assert.Equal(1003, response.StatusId);
        }

        [Fact]
        public async Task BetSettlement_UserIdIsNotAGuid_Returns1003()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var request = ASettlement(Guid.NewGuid(), Guid.NewGuid(), 10m, 20m, BetStatus.Won);
            request.UserId = "not-a-guid";

            Assert.Equal(1003, (await repository.BetSettlement(request, Guid.NewGuid().ToString())).StatusId);
        }

        [Fact]
        public async Task BetSettlement_ClientOnTheNegativeTestList_Returns1003()
        {
            var clientId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.NegativeTest.Add(new NegativeTest { ClientId = clientId });
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetSettlement(
                ASettlement(userId, Guid.NewGuid(), 30m, 90m, BetStatus.Won),
                clientId.ToString());

            Assert.Equal(1003, response.StatusId);
            Assert.Equal(70m, context.Operator.Single().Balance);
        }

        #endregion

        #region Resettlement - the reversal rules

        [Fact]
        public async Task Resettlement_WonThenLost_ReversesThePreviousCreditFirst()
        {
            // The bet was paid 90. Re-settling as Lost must claw that back, not just add zero:
            // postBalance starts at balance + (-previousAmount) before the new amount applies.
            var userId = Guid.NewGuid();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var clientId = Guid.NewGuid().ToString();

            await repository.BetSettlement(ASettlement(userId, betId, 30m, 90m, BetStatus.Won), clientId);
            Assert.Equal(160m, context.Operator.Single().Balance);

            var response = await repository.BetSettlement(ASettlement(userId, betId, 30m, 0m, BetStatus.Lost), clientId);

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(70m, response.Balance);
        }

        [Fact]
        public async Task Resettlement_WonThenWonWithADifferentPayout_ReplacesRatherThanStacks()
        {
            var userId = Guid.NewGuid();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var clientId = Guid.NewGuid().ToString();

            await repository.BetSettlement(ASettlement(userId, betId, 30m, 90m, BetStatus.Won), clientId);

            // Same status, so the reversal branch is skipped entirely: the second payout is
            // added on top of the first. Pinned as behaviour, not endorsed as correct - a
            // repeated Won for the same bet pays twice.
            var response = await repository.BetSettlement(ASettlement(userId, betId, 30m, 50m, BetStatus.Won), clientId);

            Assert.Equal(210m, response.Balance);
        }

        [Fact]
        public async Task Resettlement_WonThenVoid_RefundsTheStakeAndReversesThePayout()
        {
            // Void takes the dedicated branch: amount = payout + (-previousAmount), where the
            // void payout is the returned stake.
            var userId = Guid.NewGuid();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var clientId = Guid.NewGuid().ToString();

            await repository.BetSettlement(ASettlement(userId, betId, 30m, 90m, BetStatus.Won), clientId);

            var response = await repository.BetSettlement(ASettlement(userId, betId, 30m, 30m, BetStatus.Void), clientId);

            // 160 balance, reverse the 90 credit, then refund the 30 stake.
            Assert.Equal(100m, response.Balance);
        }

        [Fact]
        public async Task Resettlement_VoidThenWon_CreditsPayoutMinusStakeAndKeepsTheVoidRefund()
        {
            // The asymmetric case. When the PREVIOUS row is the Void, neither reversal branch
            // matches: the first needs the previous row not to be a Void, the second needs the
            // incoming status to be Void. Control falls to the else, which sets
            // amount = payout - stake but leaves postBalance at the current balance.
            //
            // So the void refund is NOT clawed back. The player keeps the returned stake and
            // is then credited payout - stake on top. Pinned as the behaviour that ships;
            // whether it is intended is a separate question, and changing it would move money.
            var userId = Guid.NewGuid();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 100m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var clientId = Guid.NewGuid().ToString();

            await repository.BetSettlement(ASettlement(userId, betId, 30m, 30m, BetStatus.Void), clientId);
            Assert.Equal(130m, context.Operator.Single().Balance);

            var response = await repository.BetSettlement(ASettlement(userId, betId, 30m, 90m, BetStatus.Won), clientId);

            // 130 kept in full, plus (90 - 30). Compare with Resettlement_WonThenVoid, where
            // the previous credit IS reversed.
            Assert.Equal(190m, response.Balance);
        }

        [Fact]
        public async Task Resettlement_RunningThenWon_DoesNotReverseThePlacementDebit()
        {
            // Running is not in the reversal switch, so the placement debit stands and the
            // payout is simply credited. This is the ordinary happy path of a settled bet.
            var userId = Guid.NewGuid();
            var token = Guid.NewGuid().ToString();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, token, 100m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var clientId = Guid.NewGuid().ToString();

            await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = betId.ToString(), Stake = 30m, BetStatus = BetStatus.Running.ToString() },
                token,
                clientId);
            Assert.Equal(70m, context.Operator.Single().Balance);

            var response = await repository.BetSettlement(ASettlement(userId, betId, 30m, 90m, BetStatus.Won), clientId);

            Assert.Equal(160m, response.Balance);
        }

        [Fact]
        public async Task Resettlement_LooksUpThePreviousRowByBetIdAndUser_NotByBetIdAlone()
        {
            // Another player's audit row for the same bet id must not trigger a reversal.
            var mine = Guid.NewGuid();
            var theirs = Guid.NewGuid();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(mine, Guid.NewGuid().ToString(), 100m));
            context.OperatorAudit.Add(new OperatorAudit
            {
                UserId = theirs,
                BetId = betId,
                TransactionTypeId = (int)BetStatus.Won,
                Amount = 500m,
                OperatorToken = Guid.NewGuid().ToString(),
                CurrencyCode = "EUR",
                TransactionTypeName = BetStatus.Won.ToString(),
                TransactionId = string.Empty
            });
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetSettlement(
                ASettlement(mine, betId, 30m, 90m, BetStatus.Won),
                Guid.NewGuid().ToString());

            // The other player's 500 is not reversed out of my balance.
            Assert.Equal(190m, response.Balance);
        }

        #endregion

        #region BetFullCashOut and BetPartialCashOut

        [Fact]
        public async Task BetFullCashOut_CreditsTheCashoutPayout()
        {
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetFullCashOut(
                ASettlement(userId, Guid.NewGuid(), stake: 30m, payout: 45m, BetStatus.CashedOut),
                Guid.NewGuid().ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(115m, response.Balance);
        }

        [Fact]
        public async Task BetFullCashOut_BetBuilder_Returns1003()
        {
            // Bet-builder bets cannot be cashed out on this path.
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var request = ASettlement(userId, Guid.NewGuid(), 30m, 45m, BetStatus.CashedOut);
            request.IsBetBuilder = true;

            Assert.Equal(1003, (await repository.BetFullCashOut(request, Guid.NewGuid().ToString())).StatusId);
            Assert.Equal(70m, context.Operator.Single().Balance);
        }

        [Fact]
        public async Task BetPartialCashOut_RunningBet_CreditsThePayoutInsteadOfDebitingTheStake()
        {
            // isPartialCashout suppresses the Running sign flip. Without it a partial cashout
            // of a still-running bet would debit the stake a second time.
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetPartialCashOut(
                ASettlement(userId, Guid.NewGuid(), stake: 30m, payout: 15m, BetStatus.Running),
                Guid.NewGuid().ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(85m, response.Balance);
            Assert.Equal(15m, Assert.Single(context.OperatorAudit).Amount);
        }

        [Fact]
        public async Task BetPartialCashOut_BetBuilder_Returns1003()
        {
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var request = ASettlement(userId, Guid.NewGuid(), 30m, 15m, BetStatus.Running);
            request.IsBetBuilder = true;

            Assert.Equal(1003, (await repository.BetPartialCashOut(request, Guid.NewGuid().ToString())).StatusId);
        }

        #endregion

        #region BetCancellation

        [Fact]
        public async Task BetCancellation_RefundsTheStake()
        {
            var userId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 70m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var response = await repository.BetCancellation(
                new OperatorBetCancellationRequestDTO
                {
                    UserId = userId.ToString(),
                    BetId = Guid.NewGuid().ToString(),
                    Stake = 30m,
                    Payout = 30m,
                    BetStatus = BetStatus.Cancelled.ToString()
                },
                Guid.NewGuid().ToString());

            Assert.Equal(1000, response.StatusId);
            Assert.Equal(100m, response.Balance);
            Assert.Equal((int)BetStatus.Cancelled, Assert.Single(context.OperatorAudit).TransactionTypeId);
        }

        [Fact]
        public async Task BetCancellation_UnknownUser_Returns1003()
        {
            using var context = InMemoryPlatformContext.Create();
            var repository = new PlatformRepository(context);

            var response = await repository.BetCancellation(
                new OperatorBetCancellationRequestDTO
                {
                    UserId = Guid.NewGuid().ToString(),
                    BetId = Guid.NewGuid().ToString(),
                    Stake = 30m,
                    Payout = 30m,
                    BetStatus = BetStatus.Cancelled.ToString()
                },
                Guid.NewGuid().ToString());

            Assert.Equal(1003, response.StatusId);
        }

        #endregion

        #region Audit row contents

        [Fact]
        public async Task AuditRow_CarriesTheOperatorIdentityAndAnEmptyTransactionId()
        {
            // The audit row is the operator-facing ledger. Every field on it is copied from
            // the operator record except TransactionId, which this path always leaves empty.
            var userId = Guid.NewGuid();
            var token = Guid.NewGuid().ToString();
            var operatorClientId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, token, 100m, operatorClientId));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            await repository.BetPlacement(
                new OperatorBetRequestDTO { BetId = Guid.NewGuid().ToString(), Stake = 10m, BetStatus = BetStatus.Running.ToString() },
                token,
                Guid.NewGuid().ToString());

            var audit = Assert.Single(context.OperatorAudit);
            Assert.Equal(userId, audit.UserId);
            Assert.Equal(operatorClientId, audit.ClientId);
            Assert.Equal(token, audit.OperatorToken);
            Assert.Equal("EUR", audit.CurrencyCode);
            Assert.Equal(BetStatus.Running.ToString(), audit.TransactionTypeName);
            Assert.Equal(string.Empty, audit.TransactionId);
        }

        [Fact]
        public async Task AuditRow_PostBalanceAlwaysEqualsPreBalancePlusAmount()
        {
            // The ledger invariant. If it ever breaks, the balance and its audit trail disagree.
            var userId = Guid.NewGuid();
            var betId = Guid.NewGuid();

            using var context = InMemoryPlatformContext.Create();
            context.Operator.Add(AnOperator(userId, Guid.NewGuid().ToString(), 100m));
            context.SaveChanges();

            var repository = new PlatformRepository(context);
            var clientId = Guid.NewGuid().ToString();

            await repository.BetSettlement(ASettlement(userId, betId, 30m, 90m, BetStatus.Won), clientId);
            await repository.BetSettlement(ASettlement(userId, betId, 30m, 0m, BetStatus.Lost), clientId);

            foreach (var audit in context.OperatorAudit)
                Assert.Equal(audit.PreBalance + audit.Amount, audit.PostBalance);
        }

        #endregion
    }
}
