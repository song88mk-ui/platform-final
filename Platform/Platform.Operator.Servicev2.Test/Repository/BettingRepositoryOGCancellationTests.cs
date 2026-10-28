using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Operator.Servicev2.Repository;
using Platform.Operator.Servicev2.Telemetry;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using System.Net;
using System.Text.Json;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // GM-879: both CancelBet call sites in ProcesssBetPlacementOG passed isOGClient: true without
    // the OG parameters, so ProcesssBetCancellationOG dereferenced a null ogParam and threw a
    // NullReferenceException before the cancellation ever reached OG. These tests exercise
    // ProcesssBetCancellationOG directly, the method both call sites feed into.
    // BettingRepositoryOGPlacementCancelTests drives the two call sites themselves, through
    // ProcesssBetPlacementOG and CancelBet.
    //
    // The rollback tests below were added from a Stryker run on BettingRepository.cs: each one
    // kills mutants on ProcesssBetCancellationOG that survived the original two tests.
    public class BettingRepositoryOGCancellationTests
    {
        private const string ThreadId = "1";
        private const string DebitPath = "/api/v2/platform/transaction/bulk/debit";

        private readonly InMemoryPlatformContextFactory _factory = new InMemoryPlatformContextFactory();
        private readonly InMemoryBettingContextFactory _bettingFactory = new InMemoryBettingContextFactory();
        private readonly FakeMessageBroker _broker = new FakeMessageBroker();

        // VND is a high currency, so OG amounts are divided by 1000. The bet's own CurrencyCode is
        // USD, so a test that sees VND and the divided amount knows both came from ogParam.
        private static readonly OGRequestDTO OgParam = new OGRequestDTO { OGCurrencyCode = "VND", GameCode = "GAME01" };

        private BettingRepository CreateRepository() => new BettingRepository(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Custom:OrientalGaming:PublicAPIKey"] = "test-public-key",
                ["Custom:OrientalGaming:PrivateAPIKey"] = "test-private-key",
                ["Custom:OrientalGaming:VendorName"] = "test-vendor",
                ["Custom:OrientalGaming:HighCurrenciesAsString"] = "VND"
            }).Build(),
            _factory,
            _bettingFactory,
            new Instrumentation(),
            NullLogger<BettingRepository>.Instance);

        private static BetSelection Selection(long selectionId) => new BetSelection
        {
            BetSelectionId = Guid.NewGuid(),
            SportId = 1,
            SportName = "Soccer",
            LeagueName = "Premier League",
            FixtureId = 1001,
            FixtureName = "Home FC vs. Away FC",
            FixtureTypeId = (int)FixtureType.Match,
            FixtureStartDate = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc),
            MarketId = 2001,
            MarketName = "Match Result",
            SelectionId = selectionId,
            SelectionName = "Home FC",
            BetSelectionStatusName = BetStatus.Cancelled.ToString(),
            Odds = 2.5m
        };

        private static OnBetSettlement Cancellation(string endpoint, int selectionCount = 1, bool isAgent = false) => new OnBetSettlement
        {
            Bet = new Bet
            {
                BetId = Guid.NewGuid(),
                UserId = "user-1",
                ClientId = "0a1b2c3d-0000-0000-0000-000000000001",
                ClientCode = "T01",
                Endpoint = endpoint,
                OperatorToken = "operator-token|VND|GAME01",
                CurrencyCode = "USD",
                Stake = 50000m,
                TrueStake = 50000m,
                Odds = 2.5m,
                IsAgent = isAgent,
                BetStatusId = (int)BetStatus.Cancelled,
                BetStatusName = BetStatus.Cancelled.ToString()
            },
            BetSelections = Enumerable.Range(1, selectionCount).Select(i => Selection(3000 + i)).ToList(),
            Message = "Operator Error"
        };

        private void Cancel(OnBetSettlement message, string correlationId = "corr-1") =>
            CreateRepository().ProcesssBetCancellationOG(message, _broker, correlationId, ThreadId, OgParam);

        private static JsonElement RollbackBody(OperatorStubServer og)
        {
            var request = Assert.Single(og.Requests, r => r.Path == DebitPath);
            return JsonDocument.Parse(request.Body).RootElement;
        }

        private static IEnumerable<JsonElement> BetList(JsonElement rollback) =>
            Assert.Single(rollback.GetProperty("records").EnumerateArray())
                .GetProperty("other_info").GetProperty("bet_list").EnumerateArray();

        private OperatorSettlementRetry StoredRetry(Guid betId)
        {
            using var context = _factory.CreateDbContext();
            return context.OperatorSettlementRetry.SingleOrDefault(r => r.BetId == betId);
        }

        [Fact]
        public void NullOgParam_DoesNotThrowAndSendsNothingToOG()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\"}");
            var message = Cancellation(og.Endpoint);

            var exception = Record.Exception(() =>
                CreateRepository().ProcesssBetCancellationOG(message, _broker, Guid.NewGuid().ToString(), ThreadId, null));

            Assert.Null(exception);
            Assert.Empty(og.Requests);
            Assert.Empty(_broker.Published);
        }

        [Fact]
        public void WithOgParam_SendsARollbackInTheOgCurrencyAndGame()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);

            Cancel(message);

            var body = RollbackBody(og);
            Assert.Equal("rollback", body.GetProperty("transaction_type").GetString());
            Assert.Equal("VND", body.GetProperty("currency").GetString());
            Assert.Equal("GAME01", body.GetProperty("game_code").GetString());
            Assert.Equal("na", body.GetProperty("round_id").GetString());
            Assert.Equal("user-1", body.GetProperty("player_code").GetString());
            Assert.Equal(50m, body.GetProperty("total_amount").GetDecimal());
            Assert.False(string.IsNullOrEmpty(body.GetProperty("signature").GetString()));

            var record = Assert.Single(body.GetProperty("records").EnumerateArray());
            Assert.Equal("single", record.GetProperty("bet_place").GetString());
            Assert.Equal(message.Bet.BetId.ToString("N"), record.GetProperty("transaction_id").GetString());
        }

        [Fact]
        public void MatchSelection_SendsHomeAndAwayTeams()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");

            Cancel(Cancellation(og.Endpoint));

            var selection = Assert.Single(BetList(RollbackBody(og)));
            Assert.Equal("Home FC", selection.GetProperty("home_team").GetProperty("en").GetString());
            Assert.Equal("Away FC", selection.GetProperty("away_team").GetProperty("en").GetString());
            Assert.Equal("2026-10-02T12:00:00.0000000", selection.GetProperty("FixtureStartDate").GetString());
        }

        [Fact]
        public void OutrightSelection_SendsNoTeams()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);
            message.BetSelections[0].FixtureTypeId = (int)FixtureType.Outright;
            message.BetSelections[0].FixtureName = "Premier League Winner";

            Cancel(message);

            var selection = Assert.Single(BetList(RollbackBody(og)));
            Assert.Equal(JsonValueKind.Null, selection.GetProperty("home_team").GetProperty("en").ValueKind);
            Assert.Equal(JsonValueKind.Null, selection.GetProperty("away_team").GetProperty("en").ValueKind);
        }

        [Fact]
        public void MultipleSelections_AreSentAsAMultipleBet()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");

            Cancel(Cancellation(og.Endpoint, selectionCount: 2));

            var record = Assert.Single(RollbackBody(og).GetProperty("records").EnumerateArray());
            Assert.Equal("multiple", record.GetProperty("bet_place").GetString());
        }

        [Fact]
        public void WithOgParam_TellsThePlayerTheBetIsCancelled()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);

            Cancel(message);

            var published = Assert.Single(_broker.Published, p => p.MessageType == "OnBetStatus");
            Assert.Equal("r.bet.betstatus.t01", published.RoutingKey);
            var status = Assert.IsType<OnBetStatus>(published.Payload);
            Assert.Equal(message.Bet.BetId.ToString().ToUpper(), status.BetId);
            Assert.Equal(message.Bet.ClientId.ToUpper(), status.ClientId);
            Assert.Equal("user-1", status.UserId);
            Assert.Equal(BetStatus.Cancelled, status.BetStatus);
            Assert.Equal("Operator Error", status.Message);
        }

        [Theory]
        [InlineData(false, "r.bet.reports")]
        [InlineData(true, "r.bet.reports.agent")]
        public void WithOgParam_ReportsTheCancellationOnTheAgentOrPlayerRoute(bool isAgent, string routingKey)
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");

            Cancel(Cancellation(og.Endpoint, isAgent: isAgent));

            var published = Assert.Single(_broker.Published, p => p.MessageType == "OnBetCancellation");
            Assert.Equal(routingKey, published.RoutingKey);
        }

        [Fact]
        public void WithOgParam_NotifiesBackOfficeOfTheCancellation()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);

            Cancel(message);

            // Provider.Service is the r.bet.cancellation consumer and switches on "OnBetBackOfficeCancellation".
            var published = Assert.Single(_broker.Published, p => p.MessageType == "OnBetBackOfficeCancellation");
            Assert.Equal("r.bet.cancellation", published.RoutingKey);
            Assert.Equal(message.Bet.BetId, Assert.IsType<OnBetBackofficeCancellation>(published.Payload).BetId);
        }

        [Fact]
        public void OgAcceptsRollback_PublishesTheUpdatedBalance()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);

            Cancel(message);

            var published = Assert.Single(_broker.Published, p => p.MessageType == "OnWalletChange");
            Assert.Equal("r.wallet.t01", published.RoutingKey);
            var wallet = Assert.IsType<OnWalletChange>(published.Payload);
            Assert.Equal(540m, wallet.Balance);
            Assert.Equal(message.Bet.ClientId.ToUpper(), wallet.ClientId);
            Assert.Equal("user-1", wallet.UserId);
        }

        [Fact]
        public void OgAcceptsRollback_MarksAPendingRetryAsSucceeded()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);
            using (var context = _factory.CreateDbContext())
            {
                context.OperatorSettlementRetry.Add(new OperatorSettlementRetry
                {
                    BetId = message.Bet.BetId,
                    CorrelationId = "corr-1",
                    MessageType = "Pending",
                    IsRetry = true
                });
                context.SaveChanges();
            }

            Cancel(message);

            var retry = StoredRetry(message.Bet.BetId);
            Assert.Equal("OnBetCancellation", retry.MessageType);
            Assert.True(retry.IsSuccess);
            Assert.False(retry.IsRetry);
        }

        [Fact]
        public void WithOgParam_LogsTheRollbackRequestAndResponse()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"updated_balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);

            Cancel(message);

            using var context = _factory.CreateDbContext();
            var logs = context.OperatorLog.Where(l => l.BetId == message.Bet.BetId.ToString().ToUpper()).ToList();
            var request = Assert.Single(logs, l => l.Direction == "REQUEST");
            var response = Assert.Single(logs, l => l.Direction == "RESPONSE");
            Assert.All(new[] { request, response }, l =>
            {
                Assert.Equal($"{og.Endpoint}{DebitPath}", l.URL);
                Assert.Equal("T01", l.ClientCode);
                Assert.Equal("corr-1", l.CorrelationId);
            });
            Assert.Contains("\"transaction_type\":\"rollback\"", request.Info);
        }

        // loggedResponse is what each branch writes to the RESPONSE OperatorLog: the parsed OG
        // result on 200 and 400, the reason phrase otherwise.
        public static IEnumerable<object[]> RejectedRollbacks => new List<object[]>
        {
            new object[] { HttpStatusCode.OK, "{\"rs_code\":\"E-000\",\"rs_message\":\"rejected\"}", "E-000" },
            new object[] { HttpStatusCode.BadRequest, "{\"rs_code\":\"E-000\",\"rs_message\":\"rejected\"}", "E-000" },
            new object[] { HttpStatusCode.InternalServerError, "", "Internal Server Error" }
        };

        [Theory]
        [MemberData(nameof(RejectedRollbacks))]
        public void OgRejectsRollback_StoresARetryForTheCancellation(HttpStatusCode status, string body, string loggedResponse)
        {
            using var og = new OperatorStubServer(status, body);
            var message = Cancellation(og.Endpoint);

            Cancel(message);

            var retry = StoredRetry(message.Bet.BetId);
            Assert.NotNull(retry);
            Assert.Equal("OnBetCancellation", retry.MessageType);
            Assert.Equal("corr-1", retry.CorrelationId);
            Assert.False(retry.IsSuccess);
            Assert.DoesNotContain(_broker.Published, p => p.MessageType == "OnWalletChange");

            // The retry is written by the branch itself, not by the catch block, which would first
            // ask OG for the transaction status.
            Assert.Single(og.Requests);

            // Every failure branch publishes a second back-office cancellation for the same bet,
            // with the type Provider.Service switches on.
            var backOffice = _broker.Published.Where(p => p.RoutingKey == "r.bet.cancellation").ToList();
            Assert.Equal(2, backOffice.Count);
            Assert.All(backOffice, p => Assert.Equal("OnBetBackOfficeCancellation", p.MessageType));
            Assert.All(backOffice, p => Assert.Equal(message.Bet.BetId, Assert.IsType<OnBetBackofficeCancellation>(p.Payload).BetId));

            using var context = _factory.CreateDbContext();
            var response = Assert.Single(context.OperatorLog, l => l.BetId == message.Bet.BetId.ToString().ToUpper() && l.Direction == "RESPONSE");
            Assert.Equal($"{og.Endpoint}{DebitPath}", response.URL);
            Assert.Equal("T01", response.ClientCode);
            Assert.Contains(loggedResponse, response.Info);
        }

        // GM-892: when the rollback call itself throws, the catch block asks OG for the transaction
        // status, which throws against the same unreachable endpoint. That failure is treated as
        // "status unknown", so the retry row is still stored and nothing escapes.
        [Fact]
        public void OgUnreachable_StoresARetryForTheCancellationAndDoesNotThrow()
        {
            var endpoint = OperatorStubServer.UnreachableEndpoint();
            var message = Cancellation(endpoint);

            var exception = Record.Exception(() => Cancel(message));

            Assert.Null(exception);
            var retry = StoredRetry(message.Bet.BetId);
            Assert.NotNull(retry);
            Assert.Equal("OnBetCancellation", retry.MessageType);
            Assert.Equal("corr-1", retry.CorrelationId);
            Assert.False(retry.IsSuccess);
            Assert.DoesNotContain(_broker.Published, p => p.MessageType == "OnWalletChange");
            Assert.Equal(2, BackOfficeCancellations(message));
            using var context = _factory.CreateDbContext();
            var response = Assert.Single(context.OperatorLog, l => l.BetId == message.Bet.BetId.ToString().ToUpper() && l.Direction == "RESPONSE");
            Assert.Equal($"{endpoint}{DebitPath}", response.URL);
        }

        // GM-892: the catch block's recovery calls. The debit answers 200 with a body that cannot be
        // parsed, which throws inside the try block and lands in the catch block. Each test then
        // sets what OG answers on the transaction status and wallet balance paths.
        private const string StatusPath = "/api/v2/platform/vendor/transaction-status";
        private const string WalletPath = "/api/v2/platform/player/balance";

        private static OperatorStubServer OgWhoseDebitCannotBeRead() => new OperatorStubServer(HttpStatusCode.OK, "not json");

        private static int RequestsTo(OperatorStubServer og, string path) => og.Requests.Count(r => r.Path == path);

        // The method publishes one back-office cancellation before it calls OG. Every failure
        // fallback publishes a second one, so 2 means the fallback ran and 1 means it did not. Only
        // messages Provider.Service would handle are counted: the right type on the right route.
        private int BackOfficeCancellations(OnBetSettlement message) =>
            _broker.Published.Count(p => p.MessageType == "OnBetBackOfficeCancellation"
                && p.RoutingKey == "r.bet.cancellation"
                && p.Payload is OnBetBackofficeCancellation c && c.BetId == message.Bet.BetId);

        private void SeedPendingRetry(OnBetSettlement message, string correlationId = "corr-1")
        {
            using var context = _factory.CreateDbContext();
            context.OperatorSettlementRetry.Add(new OperatorSettlementRetry
            {
                BetId = message.Bet.BetId,
                CorrelationId = correlationId,
                MessageType = "OnBetCancellation",
                IsRetry = true
            });
            context.SaveChanges();
        }

        private void AssertRetryStoredForReplay(OnBetSettlement message)
        {
            var retry = StoredRetry(message.Bet.BetId);
            Assert.NotNull(retry);
            Assert.Equal("OnBetCancellation", retry.MessageType);
            Assert.Equal("corr-1", retry.CorrelationId);
            Assert.True(retry.IsRetry);
            Assert.False(retry.IsSuccess);
            Assert.Equal(0, retry.RetryCount);
            Assert.Equal(2, BackOfficeCancellations(message));
            Assert.DoesNotContain(_broker.Published, p => p.MessageType == "OnWalletChange");
        }

        [Fact]
        public void DebitThrows_StatusLookupCannotBeRead_StoresARetryAndDoesNotThrow()
        {
            using var og = OgWhoseDebitCannotBeRead();
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "not json");
            var message = Cancellation(og.Endpoint);

            var exception = Record.Exception(() => Cancel(message));

            Assert.Null(exception);
            AssertRetryStoredForReplay(message);
            Assert.Equal(1, RequestsTo(og, DebitPath));
            Assert.Equal(1, RequestsTo(og, StatusPath));
            Assert.Equal(0, RequestsTo(og, WalletPath));
        }

        [Fact]
        public void DebitThrows_StatusLookupFails_ReplaysTheRetryOnTheNextAttempt()
        {
            using var og = OgWhoseDebitCannotBeRead();
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "not json");
            var message = Cancellation(og.Endpoint);
            Cancel(message);

            // The stored row is exactly what Platform.Job.Resettlement selects: IsRetry and under
            // the retry cap. Its Info must round-trip into the message the job republishes.
            using var context = _factory.CreateDbContext();
            var retry = context.OperatorSettlementRetry.Single(r => r.IsRetry && r.RetryCount < 8);
            Assert.Equal(message.Bet.BetId, retry.BetId);
            var replayed = JsonSerializer.Deserialize<OnBetSettlement>(retry.Info);
            Assert.Equal(message.Bet.BetId, replayed.Bet.BetId);
            Assert.Equal(message.Bet.Endpoint, replayed.Bet.Endpoint);
        }

        [Fact]
        public void DebitThrows_StatusLookupGetsAServerError_StoresARetryAndDoesNotThrow()
        {
            using var og = OgWhoseDebitCannotBeRead();
            og.PathResponses[StatusPath] = (HttpStatusCode.InternalServerError, "");
            var message = Cancellation(og.Endpoint);

            var exception = Record.Exception(() => Cancel(message));

            Assert.Null(exception);
            AssertRetryStoredForReplay(message);
            Assert.Equal(1, RequestsTo(og, StatusPath));
        }

        [Fact]
        public void DebitThrows_OgReportsAnotherStatus_StoresARetryWithoutReadingTheWallet()
        {
            using var og = OgWhoseDebitCannotBeRead();
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"transaction_status\":\"success\"}");
            var message = Cancellation(og.Endpoint);

            var exception = Record.Exception(() => Cancel(message));

            Assert.Null(exception);
            AssertRetryStoredForReplay(message);
            Assert.Equal(0, RequestsTo(og, WalletPath));
        }

        [Fact]
        public void DebitThrows_OgConfirmsTheRollback_PublishesTheBalanceAndClosesThePendingRetry()
        {
            using var og = OgWhoseDebitCannotBeRead();
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"transaction_status\":\"rollback\"}");
            og.PathResponses[WalletPath] = (HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);
            SeedPendingRetry(message);

            var exception = Record.Exception(() => Cancel(message));

            Assert.Null(exception);
            var wallet = Assert.IsType<OnWalletChange>(Assert.Single(_broker.Published, p => p.MessageType == "OnWalletChange").Payload);
            Assert.Equal(540m, wallet.Balance);
            Assert.Equal("user-1", wallet.UserId);
            var retry = StoredRetry(message.Bet.BetId);
            Assert.True(retry.IsSuccess);
            Assert.False(retry.IsRetry);
            Assert.Equal(1, BackOfficeCancellations(message));
            Assert.Equal(1, RequestsTo(og, WalletPath));
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, "not json")]
        [InlineData(HttpStatusCode.OK, "")]
        [InlineData(HttpStatusCode.InternalServerError, "")]
        public void DebitThrows_OgConfirmsTheRollback_WalletRefreshFails_StillClosesThePendingRetry(HttpStatusCode walletStatus, string walletBody)
        {
            using var og = OgWhoseDebitCannotBeRead();
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"transaction_status\":\"rollback\"}");
            og.PathResponses[WalletPath] = (walletStatus, walletBody);
            var message = Cancellation(og.Endpoint);
            SeedPendingRetry(message);

            var exception = Record.Exception(() => Cancel(message));

            // The rollback is confirmed at OG, so a failed balance refresh must not leave the
            // retry open: the job would replay a cancellation OG has already rolled back.
            Assert.Null(exception);
            var retry = StoredRetry(message.Bet.BetId);
            Assert.True(retry.IsSuccess);
            Assert.False(retry.IsRetry);
            Assert.DoesNotContain(_broker.Published, p => p.MessageType == "OnWalletChange");
            Assert.Equal(1, BackOfficeCancellations(message));
            Assert.Equal(1, RequestsTo(og, WalletPath));
        }

        [Fact]
        public void DebitThrows_OgConfirmsTheRollback_WalletAnswersAnErrorCode_ClosesThePendingRetryWithoutPublishing()
        {
            using var og = OgWhoseDebitCannotBeRead();
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"transaction_status\":\"rollback\"}");
            og.PathResponses[WalletPath] = (HttpStatusCode.OK, "{\"rs_code\":\"E-000\",\"balance\":\"540\"}");
            var message = Cancellation(og.Endpoint);
            SeedPendingRetry(message);

            var exception = Record.Exception(() => Cancel(message));

            Assert.Null(exception);
            Assert.DoesNotContain(_broker.Published, p => p.MessageType == "OnWalletChange");
            var retry = StoredRetry(message.Bet.BetId);
            Assert.True(retry.IsSuccess);
            Assert.False(retry.IsRetry);
        }

        // The S-101 branch asks for the transaction status inside the try block. A failure there
        // lands in the catch block, which asks again; neither failure may escape.
        [Fact]
        public void DebitAnswersS101_StatusLookupCannotBeRead_StoresARetryAndDoesNotThrow()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-101\"}");
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "not json");
            var message = Cancellation(og.Endpoint);

            var exception = Record.Exception(() => Cancel(message));

            Assert.Null(exception);
            AssertRetryStoredForReplay(message);
            Assert.Equal(1, RequestsTo(og, DebitPath));
            Assert.Equal(2, RequestsTo(og, StatusPath));
        }

        [Fact]
        public void DebitAnswersS101_OgConfirmsTheRollback_WalletRefreshFails_StillClosesThePendingRetry()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"S-101\"}");
            og.PathResponses[StatusPath] = (HttpStatusCode.OK, "{\"rs_code\":\"S-100\",\"transaction_status\":\"rollback\"}");
            og.PathResponses[WalletPath] = (HttpStatusCode.OK, "not json");
            var message = Cancellation(og.Endpoint);
            SeedPendingRetry(message);

            var exception = Record.Exception(() => Cancel(message));

            // The failed balance read in the S-101 branch throws into the catch block, which
            // repeats the status and balance calls. The second balance failure is the guarded one.
            Assert.Null(exception);
            var retry = StoredRetry(message.Bet.BetId);
            Assert.True(retry.IsSuccess);
            Assert.False(retry.IsRetry);
            Assert.Equal(2, RequestsTo(og, WalletPath));
        }

        // Regression guard: an OG client that is reachable but answers the debit with an error must
        // not take the catch block's recovery path at all.
        [Fact]
        public void DebitRejectedByOg_DoesNotAskForTheTransactionStatus()
        {
            using var og = new OperatorStubServer(HttpStatusCode.OK, "{\"rs_code\":\"E-000\",\"rs_message\":\"rejected\"}");
            var message = Cancellation(og.Endpoint);

            Cancel(message);

            Assert.Equal(0, RequestsTo(og, StatusPath));
            Assert.Equal(0, RequestsTo(og, WalletPath));
        }
    }
}
