using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Job.Resettlement.Repository;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Utility;
using Xunit;

namespace Platform.Job.Resettlement.Test
{
    public class WorkerTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task StopAsync_DuringScheduledWait_ExitsWithoutRetrying()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.FromHours(1));

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.WaitForQueriesAsync(1, Timeout);
            await StopAsync(fixture.Worker);

            // Assert
            Assert.True(fixture.Worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, fixture.Queries);
            Assert.Equal(0, fixture.Logger.Errors);
        }

        [Fact]
        public async Task StopAsync_DuringRetryWait_ExitsWithoutRetryingOrLoggingAgain()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.FromHours(1)) { Failure = new InvalidOperationException("db down") };

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.WaitForQueriesAsync(1, Timeout);
            await StopAsync(fixture.Worker);

            // Assert
            Assert.True(fixture.Worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, fixture.Queries);
            Assert.Equal(1, fixture.Logger.Errors);
        }

        [Fact]
        public async Task DatabaseFailure_LogsOneErrorPerRetryDelayInsteadOfSpinning()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.FromHours(1)) { Failure = new InvalidOperationException("db down") };

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.WaitForQueriesAsync(1, Timeout);
            await Task.Delay(200);
            await StopAsync(fixture.Worker);

            // Assert
            Assert.Equal(1, fixture.Queries);
            Assert.Equal(1, fixture.Logger.Errors);
        }

        [Fact]
        public async Task DatabaseFailure_WaitsRetryDelayBeforeNextAttempt()
        {
            // Arrange
            var retryDelay = TimeSpan.FromMilliseconds(300);
            using var fixture = new Fixture(retryDelay) { Failure = new InvalidOperationException("db down") };

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.WaitForQueriesAsync(2, Timeout);
            await StopAsync(fixture.Worker);

            // Assert
            var times = fixture.QueryTimes;
            var gap = times[1] - times[0];
            Assert.True(gap >= retryDelay - TimeSpan.FromMilliseconds(50), $"retried after {gap.TotalMilliseconds}ms");
        }

        [Fact]
        public async Task DatabaseRecovers_NextAttemptProcessesTheRetryBets()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.FromMilliseconds(300)) { Failure = new InvalidOperationException("db down") };
            fixture.SeedRetryBet();

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.WaitForQueriesAsync(1, Timeout);
            fixture.Failure = null;
            await fixture.WaitForQueriesAsync(2, Timeout);
            await fixture.Repository.WaitForCallsAsync(1, Timeout);
            await StopAsync(fixture.Worker);

            // Assert
            Assert.Equal(1, fixture.Repository.Calls);
            Assert.Equal(1, fixture.Logger.Errors);
        }

        [Fact]
        public async Task BetsFound_WaitsBackoffNotRetryDelay()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.Zero);
            fixture.SeedRetryBet();

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.Repository.WaitForCallsAsync(1, Timeout);
            await Task.Delay(200);
            await StopAsync(fixture.Worker);

            // Assert
            Assert.Equal(1, fixture.Queries);
            Assert.Equal(1, fixture.Repository.Calls);
        }

        [Fact]
        public async Task NoBetsFound_WaitsBackoffNotRetryDelay()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.Zero);

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.WaitForQueriesAsync(1, Timeout);
            await Task.Delay(200);
            await StopAsync(fixture.Worker);

            // Assert
            Assert.Equal(1, fixture.Queries);
            Assert.Equal(0, fixture.Repository.Calls);
        }

        [Fact]
        public async Task StopAsync_DuringProcessBets_CancelsTheBatchAndExitsWithoutLoggingAnError()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.FromHours(1));
            fixture.Repository.BlockUntilCancelled = true;
            fixture.SeedRetryBet();

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.Repository.WaitForCallsAsync(1, Timeout);
            await StopAsync(fixture.Worker);

            // Assert
            Assert.True(fixture.Worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, fixture.Queries);
            Assert.Equal(0, fixture.Logger.Errors);
        }

        [Fact]
        public async Task StopAsync_DuringProcessBets_DisposesTheBrokerOnlyAfterTheLoopHasStopped()
        {
            // Arrange
            using var fixture = new Fixture(TimeSpan.FromHours(1));
            using var gate = new ManualResetEventSlim();
            fixture.Repository.Gate = gate;
            fixture.SeedRetryBet();

            // Act
            await fixture.Worker.StartAsync(CancellationToken.None);
            await fixture.Repository.WaitForCallsAsync(1, Timeout);
            var stopping = StopAsync(fixture.Worker);
            await Task.Delay(200);
            var disposedWhileTheBatchRuns = fixture.Broker.Disposed;
            gate.Set();
            await stopping;

            // Assert
            Assert.False(disposedWhileTheBatchRuns);
            Assert.True(fixture.Broker.Disposed);
            Assert.True(fixture.Worker.ExecuteTask.IsCompletedSuccessfully);
        }

        // A bounded stop token turns a worker that ignores shutdown into a failed assertion instead of a hung test run.
        private static async Task StopAsync(Worker worker)
        {
            using var stopTimeout = new CancellationTokenSource(Timeout);
            await worker.StopAsync(stopTimeout.Token);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly object _lock = new object();
            private readonly List<DateTime> _queryTimes = new List<DateTime>();
            private readonly DbContextOptions<PlatformContext> _options;
            private readonly ServiceProvider _provider;

            public Fixture(TimeSpan retryDelay)
            {
                _options = new DbContextOptionsBuilder<PlatformContext>()
                    .UseInMemoryDatabase($"resettlement-{Guid.NewGuid():N}")
                    .Options;

                var services = new ServiceCollection();
                services.AddScoped(_ => OpenContext());
                _provider = services.BuildServiceProvider();

                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { ["Custom:Fibonnaci"] = "2,4,6" })
                    .Build();

                Logger = new CapturingLogger();
                Repository = new FakeRepository();
                Broker = new FakeMessageBroker();
                Worker = new Worker(Logger, _provider, Broker, Repository, configuration, retryDelay);
            }

            public Worker Worker { get; }

            public FakeMessageBroker Broker { get; }

            public CapturingLogger Logger { get; }

            public FakeRepository Repository { get; }

            // Set to make every PlatformContext resolution throw, as an unreachable database does.
            public Exception Failure { get; set; }

            public int Queries
            {
                get { lock (_lock) return _queryTimes.Count; }
            }

            public DateTime[] QueryTimes
            {
                get { lock (_lock) return _queryTimes.ToArray(); }
            }

            public void SeedRetryBet()
            {
                using var context = new PlatformContext(_options);
                context.OperatorSettlementRetry.Add(new OperatorSettlementRetry
                {
                    BetId = Guid.NewGuid(),
                    CorrelationId = Guid.NewGuid().ToString(),
                    ClientId = "client",
                    ClientCode = "G00",
                    Endpoint = "https://operator.test",
                    MessageType = "OnBetSettlement",
                    Info = "{}",
                    OperatorToken = "token",
                    RetryCount = 0,
                    IsRetry = true,
                    Timestamp = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            public async Task WaitForQueriesAsync(int count, TimeSpan timeout)
            {
                var deadline = DateTime.UtcNow + timeout;
                while (Queries < count)
                {
                    if (DateTime.UtcNow > deadline)
                        throw new TimeoutException($"expected {count} queries, saw {Queries}");
                    await Task.Delay(10);
                }
            }

            public void Dispose() => _provider.Dispose();

            private PlatformContext OpenContext()
            {
                var failure = Failure;
                lock (_lock) _queryTimes.Add(DateTime.UtcNow);

                if (failure != null)
                    throw failure;

                return new PlatformContext(_options);
            }
        }

        private sealed class CapturingLogger : ILogger<Worker>
        {
            private int _errors;

            public int Errors => Volatile.Read(ref _errors);

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Error)
                    Interlocked.Increment(ref _errors);
            }
        }

        private sealed class FakeRepository : IPlatformRepository
        {
            private int _calls;

            public int Calls => Volatile.Read(ref _calls);

            // Makes ProcessBets behave like a long batch that honours the stopping token.
            public bool BlockUntilCancelled { get; set; }

            // Makes ProcessBets behave like a publish that is already in flight and ignores the token.
            public ManualResetEventSlim Gate { get; set; }

            public void ProcessBets(List<OperatorSettlementRetry> bets, IMessageBroker messageBroker, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _calls);

                Gate?.Wait(Timeout);

                if (BlockUntilCancelled)
                {
                    cancellationToken.WaitHandle.WaitOne(Timeout);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            public async Task WaitForCallsAsync(int count, TimeSpan timeout)
            {
                var deadline = DateTime.UtcNow + timeout;
                while (Calls < count)
                {
                    if (DateTime.UtcNow > deadline)
                        throw new TimeoutException($"expected {count} calls, saw {Calls}");
                    await Task.Delay(10);
                }
            }
        }
    }
}
