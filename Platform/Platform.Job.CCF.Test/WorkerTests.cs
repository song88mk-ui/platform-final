using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Platform.Job.CCF.Repository;
using Xunit;

namespace Platform.Job.CCF.Test
{
    public class WorkerTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task StopAsync_DuringScheduledWait_ExitsWithoutRetrying()
        {
            // Arrange
            var repository = new FakeRepository();
            var logger = new CapturingLogger();
            var worker = new Worker(logger, repository, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await StopAsync(worker);

            // Assert
            Assert.True(worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, repository.Calls);
            Assert.Equal(0, logger.Errors);
        }

        [Fact]
        public async Task StopAsync_DuringRetryWait_ExitsWithoutRetryingOrLoggingAgain()
        {
            // Arrange
            var repository = new FakeRepository { Failure = new InvalidOperationException("boom") };
            var logger = new CapturingLogger();
            var worker = new Worker(logger, repository, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await StopAsync(worker);

            // Assert
            Assert.True(worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, repository.Calls);
            Assert.Equal(1, logger.Errors);
        }

        [Fact]
        public async Task ProcessCCFFailure_LogsOneErrorPerRetryDelayInsteadOfSpinning()
        {
            // Arrange
            var repository = new FakeRepository { Failure = new InvalidOperationException("boom") };
            var logger = new CapturingLogger();
            var worker = new Worker(logger, repository, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await Task.Delay(200);
            await StopAsync(worker);

            // Assert
            Assert.Equal(1, repository.Calls);
            Assert.Equal(1, logger.Errors);
        }

        [Fact]
        public async Task ProcessCCFFailure_WaitsRetryDelayBeforeNextAttempt()
        {
            // Arrange
            var retryDelay = TimeSpan.FromMilliseconds(300);
            var repository = new FakeRepository { Failure = new InvalidOperationException("boom") };
            var worker = new Worker(new CapturingLogger(), repository, TimeSpan.FromHours(1), retryDelay);

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(2, Timeout);
            await StopAsync(worker);

            // Assert
            var gap = repository.CallTimes[1] - repository.CallTimes[0];
            Assert.True(gap >= retryDelay - TimeSpan.FromMilliseconds(50), $"retried after {gap.TotalMilliseconds}ms");
        }

        [Fact]
        public async Task ProcessCCFSuccess_WaitsIntervalNotRetryDelay()
        {
            // Arrange
            var repository = new FakeRepository();
            var worker = new Worker(new CapturingLogger(), repository, TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await Task.Delay(200);
            await StopAsync(worker);

            // Assert
            Assert.Equal(1, repository.Calls);
        }

        [Fact]
        public async Task StopAsync_DuringProcessCCF_CancelsTheCallAndExitsWithoutLoggingAnError()
        {
            // Arrange
            var repository = new FakeRepository { BlockUntilCancelled = true };
            var logger = new CapturingLogger();
            var worker = new Worker(logger, repository, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await StopAsync(worker);

            // Assert
            Assert.True(worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, repository.Calls);
            Assert.Equal(0, logger.Errors);
        }

        // A bounded stop token turns a worker that ignores shutdown into a failed assertion instead of a hung test run.
        private static async Task StopAsync(Worker worker)
        {
            using var stopTimeout = new CancellationTokenSource(Timeout);
            await worker.StopAsync(stopTimeout.Token);
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
            private readonly object _lock = new object();
            private readonly List<DateTime> _callTimes = new List<DateTime>();

            public Exception Failure { get; set; }

            // Makes ProcessCCF behave like a long report call that honours the cancellation token.
            public bool BlockUntilCancelled { get; set; }

            public int Calls
            {
                get { lock (_lock) return _callTimes.Count; }
            }

            public DateTime[] CallTimes
            {
                get { lock (_lock) return _callTimes.ToArray(); }
            }

            public Task ProcessCCF(CancellationToken cancellationToken)
            {
                lock (_lock) _callTimes.Add(DateTime.UtcNow);

                if (BlockUntilCancelled)
                    return Task.Delay(TimeSpan.FromHours(1), cancellationToken);

                return Failure == null ? Task.CompletedTask : Task.FromException(Failure);
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
