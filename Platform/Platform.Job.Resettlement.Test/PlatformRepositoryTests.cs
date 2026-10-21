using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Platform.Job.Resettlement.Repository;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.MessageModels;
using Xunit;

namespace Platform.Job.Resettlement.Test
{
    public class PlatformRepositoryTests
    {
        [Fact]
        public void ProcessBets_ReadableRow_PublishesTheSettlementAsARetry()
        {
            // Arrange
            var broker = new FakeMessageBroker();
            var logger = new CapturingLogger();
            var row = Row(ValidInfo());

            // Act
            new PlatformRepository(logger).ProcessBets(new List<OperatorSettlementRetry> { row }, broker, CancellationToken.None);

            // Assert
            var published = Assert.Single(broker.Published);
            Assert.True(Assert.IsType<OnBetSettlement>(published.Payload).IsRetry);
            Assert.Equal(row.MessageType, published.MessageType);
            Assert.Equal("r.bet.operator", published.RoutingKey);
            Assert.Equal(row.CorrelationId, published.CorrelationId);
            Assert.Empty(logger.Entries);
        }

        [Fact]
        public void ProcessBets_RowWithABetIdRef_IsNotPublished()
        {
            // Arrange
            var broker = new FakeMessageBroker();
            var rows = new List<OperatorSettlementRetry> { Row(ValidInfo(betIdRef: "already-sent")) };

            // Act
            new PlatformRepository(new CapturingLogger()).ProcessBets(rows, broker, CancellationToken.None);

            // Assert
            Assert.Empty(broker.Published);
        }

        [Fact]
        public void ProcessBets_UnreadableRow_IsSkippedAndTheRestOfTheBatchIsPublished()
        {
            // Arrange
            var broker = new FakeMessageBroker();
            var logger = new CapturingLogger();
            var first = Row(ValidInfo());
            var notJson = Row("{ this is not json");
            var noInfo = Row(null);
            var jsonNull = Row("null");
            var noBet = Row("{}");
            var last = Row(ValidInfo());
            var rows = new List<OperatorSettlementRetry> { first, notJson, noInfo, jsonNull, noBet, last };

            // Act
            new PlatformRepository(logger).ProcessBets(rows, broker, CancellationToken.None);

            // Assert
            Assert.Equal(new[] { first.CorrelationId, last.CorrelationId }, broker.Published.Select(p => p.CorrelationId));

            Assert.Equal(4, logger.Entries.Count);
            Assert.All(logger.Entries, e => Assert.Equal(LogLevel.Error, e.Level));
            Assert.IsAssignableFrom<JsonException>(logger.Entries[0].Exception);
            Assert.IsType<ArgumentNullException>(logger.Entries[1].Exception);
            Assert.Null(logger.Entries[2].Exception);
            Assert.Null(logger.Entries[3].Exception);

            var skipped = new[] { notJson, noInfo, jsonNull, noBet };
            for (var i = 0; i < skipped.Length; i++)
            {
                Assert.Contains(skipped[i].BetId.ToString(), logger.Entries[i].Message);
                Assert.Contains(skipped[i].CorrelationId, logger.Entries[i].Message);
            }

            Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("this is not json"));
        }

        [Fact]
        public void ProcessBets_CancelledBeforeTheFirstRow_PublishesNothing()
        {
            // Arrange
            var broker = new FakeMessageBroker();
            var rows = new List<OperatorSettlementRetry> { Row(ValidInfo()), Row(ValidInfo()) };
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            void Act() => new PlatformRepository(new CapturingLogger()).ProcessBets(rows, broker, cts.Token);

            // Assert
            Assert.Throws<OperationCanceledException>(Act);
            Assert.Empty(broker.Published);
        }

        [Fact]
        public void ProcessBets_CancelledBetweenRows_StopsBeforeTheNextRow()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var broker = new FakeMessageBroker { OnPublish = cts.Cancel };
            var rows = new List<OperatorSettlementRetry> { Row(ValidInfo()), Row(ValidInfo()), Row(ValidInfo()) };

            // Act
            void Act() => new PlatformRepository(new CapturingLogger()).ProcessBets(rows, broker, cts.Token);

            // Assert
            Assert.Throws<OperationCanceledException>(Act);
            Assert.Single(broker.Published);
        }

        // Info is built the way Platform.Operator.Servicev2 writes it: the settlement serialized with its own type.
        private static string ValidInfo(string betIdRef = null)
        {
            var settlement = new OnBetSettlement { Bet = new Bet { BetId = Guid.NewGuid(), BetIdRef = betIdRef } };
            return JsonSerializer.Serialize(settlement, settlement.GetType());
        }

        private static OperatorSettlementRetry Row(string info) => new OperatorSettlementRetry
        {
            BetId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString(),
            MessageType = "OnBetSettlement",
            Info = info,
            IsRetry = true
        };

        private sealed record LogEntry(LogLevel Level, Exception Exception, string Message);

        private sealed class CapturingLogger : ILogger<PlatformRepository>
        {
            public List<LogEntry> Entries { get; } = new List<LogEntry>();

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
            }
        }
    }
}
