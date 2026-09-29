using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.CashOut.Service.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Providers;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.CashOut.Service.Test
{
    // GM-865 — NOT part of the standard `dotnet test Platform/Platform.sln` run and
    // deliberately not added to Platform.sln: it needs a live SQL Server connection (the
    // ticket's own scope note says EF InMemory cannot reproduce this race - the guard being
    // tested IS a SQL Server row lock). Run manually against the DEV Betting database:
    //
    //   GM865_DEV_BETTING_CONNECTION_STRING="Server=<dev host>,1433;Database=Betting;User Id=sa;Password=<dev sa password>;TrustServerCertificate=true;Encrypt=false;" \
    //     dotnet test Platform/Platform.CashOut.Service.Test
    //
    // from a machine with Twingate access to the dev database NLB (host and credentials: see
    // the dev-environment-access memory / docs/05_DatabaseFlow.md - the same pair already
    // allow-listed in .claude/settings.local.json for ad-hoc sqlcmd probes). The connection
    // string is deliberately NOT hardcoded here - this file is committed to git.
    //
    // Reproduces two concurrent partial cashouts on the same Running bet - the exact scenario
    // in the ticket. Before the fix, the UPDLOCK guard only re-checked BetStatusId (partial
    // cashouts leave a bet Running, so this never trips), and the loser's settlement math ran
    // on the Bet row it read before either lock, not the winner's already-committed reduction -
    // so the loser's SaveChanges overwrote the winner's stake reduction instead of compounding
    // on top of it. The correct, order-independent result for two sequential percentage
    // reductions p1 then p2 of the same stake is S*(1-p1)*(1-p2) regardless of which commits
    // first; the bug instead left whichever transaction committed last as if it were the only
    // cashout that had happened.
    public class GM865ConcurrentPartialCashoutTest
    {
        private sealed class AlwaysAcceptsProvider : IBetProvider
        {
            public ChannelType ChannelType => ChannelType.BG;

            public Task<BetProviderResult> ValidateCashoutAsync(OnCashoutBetSettlement message, Bet bet, List<BetSelection> selections, bool isFullCashout, decimal cashoutPercent, decimal prevCashoutAmount) =>
                Task.FromResult(new BetProviderResult { Acceptance = BetAcceptance.Accepted, Reason = "test" });

            public Task<BetProviderResult> ValidateAsync(OnBetPlacement bet, CancellationToken cancellationToken) => throw new NotImplementedException();
            public void ProcessCancellation(Guid betId, string limitId, string clientCode, string correlationId, bool isBackOfficeTrigger = false) => throw new NotImplementedException();
            public Task ProcessSettlement(OnBetSettlement bet, string correlationId) => throw new NotImplementedException();
            public Task ProcessCashout(OnBetSettlement bet, CashoutType cashoutType, string correlationId) => throw new NotImplementedException();
            public Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request) => throw new NotImplementedException();
            public Task<bool> IsAvailableAsync(BetRequestDTO request) => throw new NotImplementedException();
        }

        private sealed class AlwaysAcceptsProviderFactory : IBetProviderFactory
        {
            public IBetProvider Resolve(int channelType) => new AlwaysAcceptsProvider();
        }

        private sealed class NoOpMessageBroker : IMessageBroker
        {
            public event EventHandler<BasicDeliverEventArgs>? MessageReceived { add { } remove { } }
            public void Connect() { }
            public void ExternalConnect() { }
            public void ConnectWithAutoAck() { }
            public void ConnectWithManualAck() { }
            public void Acknowledge(BasicDeliverEventArgs e) { }
            public void NAcknowledge(BasicDeliverEventArgs e) { }
            public void Publish<T>(T payload, string messageType, string routingKey, string correlationId) { }
            public void Publish<T>(T payload, string messageType, string routingKey, string correlationId, int priority) { }
            public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId) { }
            public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId, int priority) { }
            public void Publish(string payload, string messageType, string routingKey, string correlationId) { }
            public void CPublish(string payload, string messageType, string routingKey, string correlationId) { }
            public void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId) { }
            public void Dispose() { }
        }

        [Fact]
        public async Task TwoConcurrentPartialCashouts_CompoundCorrectly_NotOverwriteEachOther()
        {
            var connectionString = Environment.GetEnvironmentVariable("GM865_DEV_BETTING_CONNECTION_STRING");

            Assert.False(string.IsNullOrEmpty(connectionString),
                "Set GM865_DEV_BETTING_CONNECTION_STRING to the dev Betting SQL Server connection string to run this test - see this file's header comment.");

            var betId = Guid.NewGuid();
            var betSelectionId = Guid.NewGuid();

            const decimal originalStake = 1000m;
            const decimal percent1 = 0.5m;
            const decimal percent2 = 0.25m;
            var expectedFinalStake = originalStake * (1 - percent1) * (1 - percent2); // 375, order-independent

            var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(connectionString);

            try
            {
                using (var seed = new BettingContext(optionsBuilder.Options))
                {
                    seed.Bet.Add(new Bet
                    {
                        BetId = betId,
                        UserId = "gm865-test-user",
                        ClientId = "GM865TEST",
                        ClientCode = "GM865TEST",
                        ClientName = "GM-865 concurrency test",
                        BetStatusId = (int)BetStatus.Running,
                        BetTypeId = 0,
                        IsFreeBet = false,
                        Stake = originalStake,
                        TrueStake = originalStake,
                        StakeReal = originalStake,
                        StakeBonus = 0m,
                        StakeTax = 0m,
                        WinningsTax = 0m,
                        Odds = 2.0m,
                        OddsTypeId = (int)OddsType.Decimal,
                        OddsTypeName = OddsType.Decimal.ToString(),
                        BetStatusName = BetStatus.Running.ToString(),
                        CurrencyCode = "USD",
                        Endpoint = "http://localhost",
                        OperatorToken = "gm865-test-token"
                    });
                    seed.BetSelection.Add(new BetSelection
                    {
                        BetSelectionId = betSelectionId,
                        BetId = betId,
                        SportId = 1,
                        FixtureId = 1,
                        FixtureName = "GM-865 test fixture",
                        FixtureTypeId = (int)FixtureType.Match,
                        MarketId = 1,
                        MarketName = "GM-865 test market",
                        SelectionId = 1,
                        SelectionName = "GM-865 test selection",
                        BetSelectionStatusId = (int)BetStatus.Running,
                        Odds = 2.0m,
                        DeadHeatFactor = 1.0m,
                        IsBinary = true
                    });
                    seed.SaveChanges();
                }

                var services = new ServiceCollection();
                services.AddSingleton<IBetProviderFactory, AlwaysAcceptsProviderFactory>();
                var serviceProvider = services.BuildServiceProvider();

                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Betting"] = connectionString
                    })
                    .Build();

                var repository = new BettingRepository(configuration, NullLogger<BettingRepository>.Instance, serviceProvider.GetRequiredService<IServiceScopeFactory>());
                var messageBroker = new NoOpMessageBroker();

                var message1 = new OnCashoutBetSettlement
                {
                    BetId = betId,
                    UserId = "gm865-test-user",
                    ClientId = "GM865TEST",
                    ClientCode = "GM865TEST",
                    ChannelTypeId = (int)ChannelType.BG,
                    CashoutType = CashoutType.Partial,
                    CashOutStakePercent = percent1,
                    CashOutStake = originalStake * percent1,
                    HasLive = false
                };
                var message2 = new OnCashoutBetSettlement
                {
                    BetId = betId,
                    UserId = "gm865-test-user",
                    ClientId = "GM865TEST",
                    ClientCode = "GM865TEST",
                    ChannelTypeId = (int)ChannelType.BG,
                    CashoutType = CashoutType.Partial,
                    CashOutStakePercent = percent2,
                    CashOutStake = originalStake * percent2,
                    HasLive = false
                };

                await Task.WhenAll(
                    Task.Run(() => repository.ProcessCashOut(message1, messageBroker, Guid.NewGuid().ToString())),
                    Task.Run(() => repository.ProcessCashOut(message2, messageBroker, Guid.NewGuid().ToString())));

                using (var verify = new BettingContext(optionsBuilder.Options))
                {
                    var finalBet = verify.Bet.AsNoTracking().First(b => b.BetId == betId);
                    var auditRows = verify.PartialCashoutAudit.AsNoTracking().Where(a => a.BetId == betId).ToList();

                    Assert.Equal(2, auditRows.Count);
                    Assert.Equal(expectedFinalStake, finalBet.Stake);
                    Assert.Equal(expectedFinalStake, finalBet.TrueStake);
                }
            }
            finally
            {
                using var cleanup = new BettingContext(optionsBuilder.Options);
                cleanup.PartialCashoutAudit.RemoveRange(cleanup.PartialCashoutAudit.Where(a => a.BetId == betId));
                cleanup.BetSelection.RemoveRange(cleanup.BetSelection.Where(s => s.BetId == betId));
                cleanup.Bet.RemoveRange(cleanup.Bet.Where(b => b.BetId == betId));
                cleanup.SaveChanges();
            }
        }
    }
}
