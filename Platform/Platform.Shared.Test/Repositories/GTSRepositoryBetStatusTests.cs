using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models.Provider.GTS;
using Platform.Shared.Repositories;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Platform.Shared.Test.Repositories
{
    // GM-816: a Void bet was being sent to GTS with status "Cancelled", which triggered GTS's
    // "Cancelled bets must have a Payout equal to TotalStake" validation and rejected the sync
    // (e.g. bet FCC1ACA7-83AB-4B11-7D88-08DEECEF4B3B). GTS should receive Void as "Settled".
    public class GTSRepositoryBetStatusTests
    {
        private sealed class CapturingHandler : HttpMessageHandler
        {
            public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request, body));

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
                };
            }
        }

        private static GTSRepository CreateRepository(CapturingHandler handler)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["API:GTSAPI"] = "https://gts.test.local/",
                    ["Custom:IsProviderEnabled"] = "true"
                })
                .Build();

            return new GTSRepository(new HttpClient(handler), configuration, NullLogger<GTSRepository>.Instance);
        }

        private static Bet CreateBet(int betStatusId) => new Bet
        {
            BetId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            UserId = "user-1",
            ClientCode = "S88",
            LimitId = "700",
            BetDate = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
            BetStatusId = betStatusId,
            Stake = 1m,
            TrueStake = 1m,
            Payout = 1m,
            ExchangeRate = 1m,
            BetTypeId = 0,
            OddsTypeId = 0
        };

        private static BetSelection CreateSelection(int statusId) => new BetSelection
        {
            BetSelectionStatusId = statusId,
            FixtureStatusId = 0,
            SelectionSpecifier = "sr:market:sel-1",
            Odds = 1.5m
        };

        [Fact]
        public async Task SendBet_VoidBet_SendsSettledStatusNotCancelled()
        {
            var handler = new CapturingHandler();
            var repository = CreateRepository(handler);
            var bet = CreateBet((int)BetStatus.Void);

            var result = await repository.SendBet("token", new SendBet
            {
                Bet = bet,
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.Void) }
            }, isSettled: true);

            Assert.Equal(StatusCode.Success, result.StatusCode);

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);
            var root = json.RootElement;

            Assert.Equal("Settled", root.GetProperty("status").GetString());

            var leg = Assert.Single(root.GetProperty("legs").EnumerateArray());
            Assert.Equal("Settled", leg.GetProperty("status").GetString());
        }

        [Fact]
        public async Task SendBet_CancelledBet_StillSendsCancelledStatus()
        {
            var handler = new CapturingHandler();
            var repository = CreateRepository(handler);
            var bet = CreateBet((int)BetStatus.Cancelled);

            var result = await repository.SendBet("token", new SendBet
            {
                Bet = bet,
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.Cancelled) }
            }, isSettled: true);

            Assert.Equal(StatusCode.Success, result.StatusCode);

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);

            Assert.Equal("Cancelled", json.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task SendBet_RejectedBet_StillSendsCancelledStatus()
        {
            var handler = new CapturingHandler();
            var repository = CreateRepository(handler);
            var bet = CreateBet((int)BetStatus.Rejected);

            var result = await repository.SendBet("token", new SendBet
            {
                Bet = bet,
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.Rejected) }
            }, isSettled: true);

            Assert.Equal(StatusCode.Success, result.StatusCode);

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);

            Assert.Equal("Cancelled", json.RootElement.GetProperty("status").GetString());
        }
    }
}
