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
    public class GTSRepositoryCashoutTests
    {
        private sealed class CapturingHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _responseBody;

            public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

            public CapturingHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string responseBody = "")
            {
                _statusCode = statusCode;
                _responseBody = responseBody;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request, body));

                return new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
                };
            }
        }

        private static GTSRepository CreateRepository(CapturingHandler handler, bool providerEnabled = true)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["API:GTSAPI"] = "https://gts.test.local/",
                    ["Custom:IsProviderEnabled"] = providerEnabled.ToString()
                })
                .Build();

            return new GTSRepository(new HttpClient(handler), configuration, NullLogger<GTSRepository>.Instance);
        }

        private static Bet CreateCashedOutBet() => new Bet
        {
            BetId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            UserId = "user-1",
            ClientCode = "S88",
            LimitId = "700",
            BetDate = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
            BetStatusId = (int)BetStatus.CashedOut,
            Stake = 100m,
            TrueStake = 100m,
            Payout = 80m,       // cashout amount, set by Platform.CashOut.Service
            ExchangeRate = 2m,  // bet currency -> EUR divisor
            BetTypeId = 0,
            OddsTypeId = 0
        };

        private static BetSelection CreateSelection(int statusId, int fixtureStatusId = 1, string specifier = "sr:market:sel-1", decimal odds = 1.9m) => new BetSelection
        {
            BetSelectionStatusId = statusId,
            FixtureStatusId = fixtureStatusId,
            SelectionSpecifier = specifier,
            Odds = odds
        };

        [Fact]
        public async Task SendBet_FullCashout_SendsSettledStatusWithCashedOutFields()
        {
            var handler = new CapturingHandler();
            var repository = CreateRepository(handler);
            var bet = CreateCashedOutBet();

            var result = await repository.SendBet("token", new SendBet
            {
                Bet = bet,
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.CashedOut) }
            }, isSettled: true, cashout: new GtsCashout
            {
                CashedOutTotalStake = bet.TrueStake,
                CashedOutAmount = bet.Payout
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);

            var request = Assert.Single(handler.Requests);
            Assert.Equal("https://gts.test.local/v1/platforms/song88/bookmakers/700/bets", request.Request.RequestUri!.ToString());

            using var json = JsonDocument.Parse(request.Body);
            var root = json.RootElement;

            Assert.Equal("Settled", root.GetProperty("status").GetString());
            Assert.Equal(50m, root.GetProperty("totalStake").GetDecimal());              // 100 / 2 (EUR)
            Assert.Equal(50m, root.GetProperty("cashedOutTotalStake").GetDecimal());     // 100 / 2 (EUR)
            Assert.Equal(40m, root.GetProperty("cashedOutAmount").GetDecimal());         // 80 / 2 (EUR)
            Assert.Equal(40m, root.GetProperty("payout").GetDecimal());                  // 80 / 2 (EUR)

            // Per Genius Sports guidance: cashed-out legs are not updated — they stay Open
            // with payoutPrice null; only the bet-level values carry the cashout.
            var leg = Assert.Single(root.GetProperty("legs").EnumerateArray());
            Assert.Equal("Open", leg.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, leg.GetProperty("payoutPrice").ValueKind);
            Assert.Equal("sel-1", leg.GetProperty("betGeniusContent").GetProperty("selectionId").GetString());
        }

        [Fact]
        public async Task SendBet_WithoutCashout_DoesNotAddCashedOutFields()
        {
            var handler = new CapturingHandler();
            var repository = CreateRepository(handler);
            var bet = CreateCashedOutBet();
            bet.BetStatusId = (int)BetStatus.Running;

            await repository.SendBet("token", new SendBet
            {
                Bet = bet,
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.Running) }
            });

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);
            var root = json.RootElement;

            Assert.Equal("Open", root.GetProperty("status").GetString());
            Assert.False(root.TryGetProperty("cashedOutTotalStake", out _));
            Assert.False(root.TryGetProperty("cashedOutAmount", out _));
        }

        [Fact]
        public async Task SendBet_FullCashout_LegsSettledBeforeCashoutKeepTheirValues()
        {
            var handler = new CapturingHandler();
            var repository = CreateRepository(handler);
            var bet = CreateCashedOutBet();

            await repository.SendBet("token", new SendBet
            {
                Bet = bet,
                BetSelections = new List<BetSelection>
                {
                    CreateSelection((int)BetStatus.Won, specifier: "sr:market:won-leg", odds: 2.5m),
                    CreateSelection((int)BetStatus.CashedOut, specifier: "sr:market:cashed-leg")
                }
            }, isSettled: true, cashout: new GtsCashout
            {
                CashedOutTotalStake = bet.TrueStake,
                CashedOutAmount = bet.Payout
            });

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);
            var legs = json.RootElement.GetProperty("legs").EnumerateArray().ToList();

            // A leg settled before the cashout keeps its real settled status and payoutPrice.
            Assert.Equal("Settled", legs[0].GetProperty("status").GetString());
            Assert.Equal(2.5m, legs[0].GetProperty("payoutPrice").GetDecimal());

            // The cashed-out leg is left Open with payoutPrice null.
            Assert.Equal("Open", legs[1].GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, legs[1].GetProperty("payoutPrice").ValueKind);
        }

        [Fact]
        public async Task CashoutAssessment_PrefixesBetIdAndUsesUnitStake()
        {
            var handler = new CapturingHandler(HttpStatusCode.OK, "{\"isBetAllowed\":true,\"maxAllowedStake\":100}");
            var repository = CreateRepository(handler);
            var bet = CreateCashedOutBet();

            var result = await repository.CashoutAssessment("token", new MessageModels.OnBetSettlement
            {
                Bet = bet,
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.CashedOut) }
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.True(result.Result.IsBetAllowed);

            var request = Assert.Single(handler.Requests);
            Assert.Equal("https://gts.test.local/v1/platforms/song88/bookmakers/700/assessment", request.Request.RequestUri!.ToString());

            using var json = JsonDocument.Parse(request.Body);
            var root = json.RootElement;

            Assert.Equal($"cashout-{bet.BetId.ToString().ToUpper()}", root.GetProperty("betId").GetString());
            Assert.Equal(1m, root.GetProperty("totalStake").GetDecimal());
            Assert.Equal("AcceptAny", root.GetProperty("priceChangeRule").GetString());

            var leg = Assert.Single(root.GetProperty("legs").EnumerateArray());
            Assert.Equal("InPlay", leg.GetProperty("gameState").GetString());
            Assert.Equal("sel-1", leg.GetProperty("selectionId").GetString());
        }

        [Fact]
        public async Task CashoutAssessment_InPlayOverridesStalePlacementGameState()
        {
            var handler = new CapturingHandler(HttpStatusCode.OK, "{\"isBetAllowed\":true}");
            var repository = CreateRepository(handler);

            // Stored FixtureStatusId says PreMatch (placement-time), but the cashout happens in-play.
            await repository.CashoutAssessment("token", new MessageModels.OnBetSettlement
            {
                Bet = CreateCashedOutBet(),
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.Running, fixtureStatusId: 0) }
            }, isInPlay: true);

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);
            var leg = Assert.Single(json.RootElement.GetProperty("legs").EnumerateArray());
            Assert.Equal("InPlay", leg.GetProperty("gameState").GetString());
        }

        [Fact]
        public async Task CashoutAssessment_MixedParlay_UsesPerLegLivenessFromQuoteProbabilities()
        {
            var handler = new CapturingHandler(HttpStatusCode.OK, "{\"isBetAllowed\":true}");
            var repository = CreateRepository(handler);

            var liveLeg = CreateSelection((int)BetStatus.CashedOut, fixtureStatusId: 0, specifier: "sr:market:live-leg");
            liveLeg.BetSelectionId = Guid.NewGuid();
            var prematchLeg = CreateSelection((int)BetStatus.CashedOut, fixtureStatusId: 0, specifier: "sr:market:prematch-leg");
            prematchLeg.BetSelectionId = Guid.NewGuid();

            // Bet-level HasLive=true would mark BOTH legs InPlay; the quote-time per-leg
            // probabilities must win: only the genuinely live leg is InPlay.
            await repository.CashoutAssessment("token", new MessageModels.OnBetSettlement
            {
                Bet = CreateCashedOutBet(),
                BetSelections = new List<BetSelection> { liveLeg, prematchLeg }
            }, isInPlay: true, probabilities: new List<MessageModels.CashedOutProbabilities>
            {
                new MessageModels.CashedOutProbabilities { BetSelectionId = liveLeg.BetSelectionId, IsLive = true },
                new MessageModels.CashedOutProbabilities { BetSelectionId = prematchLeg.BetSelectionId, IsLive = false }
            });

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);
            var legs = json.RootElement.GetProperty("legs").EnumerateArray().ToList();

            Assert.Equal("InPlay", legs[0].GetProperty("gameState").GetString());
            Assert.Equal("PreMatch", legs[1].GetProperty("gameState").GetString());
        }

        [Fact]
        public async Task CashoutAssessment_ExcludesLegsSettledBeforeTheCashout()
        {
            var handler = new CapturingHandler(HttpStatusCode.OK, "{\"isBetAllowed\":true}");
            var repository = CreateRepository(handler);

            await repository.CashoutAssessment("token", new MessageModels.OnBetSettlement
            {
                Bet = CreateCashedOutBet(),
                BetSelections = new List<BetSelection>
                {
                    CreateSelection((int)BetStatus.Won, specifier: "sr:market:won-leg"),
                    CreateSelection((int)BetStatus.Lost, specifier: "sr:market:lost-leg"),
                    CreateSelection((int)BetStatus.CashedOut, specifier: "sr:market:active-leg"),
                    CreateSelection((int)BetStatus.Running, specifier: "sr:market:running-leg")
                }
            });

            var request = Assert.Single(handler.Requests);
            using var json = JsonDocument.Parse(request.Body);

            var selectionIds = json.RootElement.GetProperty("legs").EnumerateArray()
                .Select(l => l.GetProperty("selectionId").GetString())
                .ToList();

            Assert.Equal(new[] { "active-leg", "running-leg" }, selectionIds);
        }

        [Fact]
        public async Task CashoutAssessment_ProviderDisabled_AllowsWithoutHttpCall()
        {
            var handler = new CapturingHandler();
            var repository = CreateRepository(handler, providerEnabled: false);

            var result = await repository.CashoutAssessment("token", new MessageModels.OnBetSettlement
            {
                Bet = CreateCashedOutBet(),
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.CashedOut) }
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.True(result.Result.IsBetAllowed);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CashoutAssessment_GtsError_ReturnsErrorStatus()
        {
            var handler = new CapturingHandler(HttpStatusCode.UnprocessableEntity, "{\"error\":\"already confirmed\"}");
            var repository = CreateRepository(handler);

            var result = await repository.CashoutAssessment("token", new MessageModels.OnBetSettlement
            {
                Bet = CreateCashedOutBet(),
                BetSelections = new List<BetSelection> { CreateSelection((int)BetStatus.CashedOut) }
            });

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }
    }
}
