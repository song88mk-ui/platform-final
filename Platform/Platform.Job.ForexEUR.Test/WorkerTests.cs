using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Job.ForexEUR.Repository;
using Xunit;

namespace Platform.Job.ForexEUR.Test
{
    public class WorkerTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task StopAsync_DuringScheduledWait_ExitsWithoutRetrying()
        {
            // Arrange
            var repository = new FakeRepository();
            var worker = new Worker(NullLogger<Worker>.Instance, repository, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await worker.StopAsync(CancellationToken.None);

            // Assert
            Assert.True(worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, repository.Calls);
        }

        [Fact]
        public async Task StopAsync_DuringRetryWait_ExitsWithoutRetrying()
        {
            // Arrange
            var repository = new FakeRepository { Failure = new InvalidOperationException("boom") };
            var worker = new Worker(NullLogger<Worker>.Instance, repository, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await worker.StopAsync(CancellationToken.None);

            // Assert
            Assert.True(worker.ExecuteTask.IsCompletedSuccessfully);
            Assert.Equal(1, repository.Calls);
        }

        [Fact]
        public async Task ProcessForexFailure_WaitsRetryDelayBeforeNextAttempt()
        {
            // Arrange
            var retryDelay = TimeSpan.FromMilliseconds(300);
            var repository = new FakeRepository { Failure = new InvalidOperationException("boom") };
            var worker = new Worker(NullLogger<Worker>.Instance, repository, TimeSpan.FromHours(1), retryDelay);

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(2, Timeout);
            await worker.StopAsync(CancellationToken.None);

            // Assert
            var gap = repository.CallTimes[1] - repository.CallTimes[0];
            Assert.True(gap >= retryDelay - TimeSpan.FromMilliseconds(50), $"retried after {gap.TotalMilliseconds}ms");
        }

        [Fact]
        public async Task ProcessForexSuccess_WaitsIntervalNotRetryDelay()
        {
            // Arrange
            var repository = new FakeRepository();
            var worker = new Worker(NullLogger<Worker>.Instance, repository, TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            await worker.StartAsync(CancellationToken.None);
            await repository.WaitForCallsAsync(1, Timeout);
            await Task.Delay(200);
            await worker.StopAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1, repository.Calls);
        }

        private sealed class FakeRepository : IPlatformRepository
        {
            private readonly object _lock = new object();
            private readonly List<DateTime> _callTimes = new List<DateTime>();

            public Exception Failure { get; set; }

            public int Calls
            {
                get { lock (_lock) return _callTimes.Count; }
            }

            public DateTime[] CallTimes
            {
                get { lock (_lock) return _callTimes.ToArray(); }
            }

            public Task ProcessForex()
            {
                lock (_lock) _callTimes.Add(DateTime.UtcNow);
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
