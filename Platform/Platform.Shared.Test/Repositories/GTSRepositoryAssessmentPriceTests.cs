using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.GTS;
using Platform.Shared.Repositories;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Platform.Shared.Test.Repositories
{
    // BO-10: a boosted bet was rejected by the Genius Sports bet assessment with "Current price 1.50 differs from
    // original 1.64". The assessment compares the price of each leg with its own current price (priceChangeRule
    // AcceptNone refuses a difference), and the leg carried the boosted price the player is paid. Every call that
    // carries a leg price (max stake, assessment, bet feed) now shows a boosted bet at the ORIGINAL price
    // (BetSelection.OddsRef), as MtsBetProvider does; what the bet pays stays the boosted odds (payoutPrice).
    public class GTSRepositoryAssessmentPriceTests
    {
        private const decimal ORIGINAL_ODDS = 1.50m;
        private const decimal BOOSTED_ODDS = 1.64m;

        private sealed class CapturingHandler : HttpMessageHandler
        {
            private readonly string _responseBody;

            public List<string> Bodies { get; } = new();

            public CapturingHandler(string responseBody = "{\"isBetAllowed\":true,\"maxAllowedStake\":1000}")
            {
                _responseBody = responseBody;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Bodies.Add(request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
                };
            }
        }

        private static GTSRepository CreateRepository(CapturingHandler handler)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["API:GTSAPI"] = "https://gts.test.local/",
                    ["Custom:IsProviderEnabled"] = "True"
                })
                .Build();

            return new GTSRepository(new HttpClient(handler), configuration, NullLogger<GTSRepository>.Instance);
        }

        private static Bet CreateBet(BetType betType) => new Bet
        {
            BetId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            UserId = "user-1",
            ClientCode = "S88",
            LimitId = "700",
            BetDate = new DateTime(2026, 10, 27, 13, 22, 0, DateTimeKind.Utc),
            BetStatusId = (int)BetStatus.Running,
            Stake = 6m,
            TrueStake = 6m,
            ExchangeRate = 1m,
            BetTypeId = (int)betType
        };

        // The selection of the DEV bet that was rejected: 1.50 is the original price, 1.64 the boosted one.
        private static BetSelection CreateSelection(decimal odds = BOOSTED_ODDS, decimal oddsRef = ORIGINAL_ODDS, int statusId = 0) => new BetSelection
        {
            SelectionSpecifier = "bg:145241110:418700690",
            FixtureStatusId = 0,
            BetSelectionStatusId = statusId,
            Odds = odds,
            OddsRef = oddsRef
        };

        private static OnBetPlacement CreatePlacement(BetType betType, bool isBoosted, BetSelection selection, OddsChangeType oddsChangeType = OddsChangeType.None) => new OnBetPlacement
        {
            Bet = CreateBet(betType),
            IsBoosted = isBoosted,
            OddsChangeType = oddsChangeType,
            BetSelections = new List<BetSelectionMessageExtenstion>
            {
                new BetSelectionMessageExtenstion { BetSelection = selection }
            }
        };

        private static async Task<JsonElement> AssessAsync(OnBetPlacement placement)
        {
            var handler = new CapturingHandler();

            await CreateRepository(handler).Assessment("token", placement);

            return JsonDocument.Parse(Assert.Single(handler.Bodies)).RootElement.Clone();
        }

        private static decimal LegPrice(JsonElement payload) => payload.GetProperty("legs")[0].GetProperty("price").GetDecimal();

        [Fact]
        public async Task Assessment_BoostedBet_SendsTheOriginalPriceNotTheBoostedOne()
        {
            var payload = await AssessAsync(CreatePlacement(BetType.PriceBoost, isBoosted: true, CreateSelection()));

            Assert.Equal(ORIGINAL_ODDS, LegPrice(payload));
        }

        // Betting.API sets both (the message flag, and BetTypeId through GetBetType): either one marks the bet.
        [Fact]
        public async Task Assessment_BoostedByTheMessageFlagOnly_SendsTheOriginalPrice()
        {
            var payload = await AssessAsync(CreatePlacement(BetType.Single, isBoosted: true, CreateSelection()));

            Assert.Equal(ORIGINAL_ODDS, LegPrice(payload));
        }

        [Fact]
        public async Task Assessment_BoostedByTheBetTypeOnly_SendsTheOriginalPrice()
        {
            var payload = await AssessAsync(CreatePlacement(BetType.PriceBoost, isBoosted: false, CreateSelection()));

            Assert.Equal(ORIGINAL_ODDS, LegPrice(payload));
        }

        [Fact]
        public async Task Assessment_NormalBet_StillSendsItsOwnOddsEvenWhenAReferenceOddsIsSet()
        {
            var payload = await AssessAsync(CreatePlacement(BetType.Single, isBoosted: false, CreateSelection(odds: 1.9m, oddsRef: 1.5m)));

            Assert.Equal(1.9m, LegPrice(payload));
        }

        // A bet stored without a reference price must not be assessed at 0.
        [Fact]
        public async Task Assessment_BoostedBetWithoutAReferencePrice_FallsBackToItsOdds()
        {
            var payload = await AssessAsync(CreatePlacement(BetType.PriceBoost, isBoosted: true, CreateSelection(oddsRef: 0m)));

            Assert.Equal(BOOSTED_ODDS, LegPrice(payload));
        }

        // Only the price changes: the rule the player chose and the rest of the payload are as before.
        [Theory]
        [InlineData(OddsChangeType.None, "AcceptNone")]
        [InlineData(OddsChangeType.Any, "AcceptAny")]
        [InlineData(OddsChangeType.Higher, "AcceptHigher")]
        public async Task Assessment_BoostedBet_KeepsThePriceChangeRuleOfTheBet(OddsChangeType oddsChangeType, string expectedRule)
        {
            var payload = await AssessAsync(CreatePlacement(BetType.PriceBoost, isBoosted: true, CreateSelection(), oddsChangeType));

            Assert.Equal(expectedRule, payload.GetProperty("priceChangeRule").GetString());
        }

        [Fact]
        public async Task Assessment_BoostedBet_KeepsTheRestOfThePayload()
        {
            var payload = await AssessAsync(CreatePlacement(BetType.PriceBoost, isBoosted: true, CreateSelection()));

            Assert.Equal("11111111-2222-3333-4444-555555555555".ToUpper(), payload.GetProperty("betId").GetString());
            Assert.Equal("user-1", payload.GetProperty("playerId").GetString());
            Assert.Equal(6m, payload.GetProperty("totalStake").GetDecimal());
            Assert.Equal("EUR", payload.GetProperty("currencyCode").GetString());
            Assert.Equal("418700690", payload.GetProperty("legs")[0].GetProperty("selectionId").GetString());
            Assert.Equal("PreMatch", payload.GetProperty("legs")[0].GetProperty("gameState").GetString());
        }

        // ---- SendBet: the bet feed shows the original price too, what the bet pays is the payoutPrice ----

        private static async Task<JsonElement> SendBetAsync(BetType betType, BetSelection selection, bool isSettled = false)
        {
            var handler = new CapturingHandler(responseBody: "\"ok\"");

            await CreateRepository(handler).SendBet("token", new SendBet
            {
                Bet = CreateBet(betType),
                BetSelections = new List<BetSelection> { selection }
            }, isSettled: isSettled);

            return JsonDocument.Parse(Assert.Single(handler.Bodies)).RootElement.Clone();
        }

        [Fact]
        public async Task SendBet_BoostedBet_ReportsTheOriginalPrice()
        {
            var payload = await SendBetAsync(BetType.PriceBoost, CreateSelection());

            var leg = payload.GetProperty("legs")[0];

            Assert.Equal(ORIGINAL_ODDS, leg.GetProperty("price").GetDecimal());
            Assert.Equal(JsonValueKind.Null, leg.GetProperty("payoutPrice").ValueKind); // not settled yet
        }

        // A won boosted leg pays the boosted odds: the payout is not touched, only the price the leg was placed at.
        [Fact]
        public async Task SendBet_SettledBoostedWinner_PaysTheBoostedOddsAtTheOriginalPrice()
        {
            var payload = await SendBetAsync(BetType.PriceBoost, CreateSelection(statusId: (int)BetStatus.Won), isSettled: true);

            var leg = payload.GetProperty("legs")[0];

            Assert.Equal(ORIGINAL_ODDS, leg.GetProperty("price").GetDecimal());
            Assert.Equal(BOOSTED_ODDS, leg.GetProperty("payoutPrice").GetDecimal());
        }

        [Fact]
        public async Task SendBet_NormalBet_ReportsItsOwnOddsEvenWhenAReferenceOddsIsSet()
        {
            var payload = await SendBetAsync(BetType.Single, CreateSelection(odds: 1.9m, oddsRef: 1.5m));

            Assert.Equal(1.9m, payload.GetProperty("legs")[0].GetProperty("price").GetDecimal());
        }

        [Fact]
        public async Task SendBet_BoostedBetWithoutAReferencePrice_FallsBackToItsOdds()
        {
            var payload = await SendBetAsync(BetType.PriceBoost, CreateSelection(oddsRef: 0m));

            Assert.Equal(BOOSTED_ODDS, payload.GetProperty("legs")[0].GetProperty("price").GetDecimal());
        }

        // ---- GetMaxStake: PlaceBet asks it first, with the same price and the AcceptNone rule ----

        private static BetRequestDTO CreateMaxStakeRequest(bool isBoosted, decimal decimalOdds = BOOSTED_ODDS, decimal oddsRef = ORIGINAL_ODDS) => new BetRequestDTO
        {
            LimitId = "700",
            BetId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            UserId = "user-1",
            Stake = 6m,
            CurrencyCode = CurrencyCode.USD,
            IsBoosted = isBoosted,
            Selections = new List<BetSelectionRequestDTO>
            {
                new BetSelectionRequestDTO
                {
                    SportId = 10,
                    FixtureId = 0,
                    SelectionSpecifier = "bg:145241110:418700690",
                    DecimalOdds = decimalOdds,
                    OddsRef = oddsRef
                }
            }
        };

        private static async Task<JsonElement> MaxStakeAsync(BetRequestDTO request)
        {
            var handler = new CapturingHandler();

            await CreateRepository(handler).GetMaxStake("token", request);

            return JsonDocument.Parse(Assert.Single(handler.Bodies)).RootElement.Clone();
        }

        // Before this the max stake call was refused too (422), and the caller silently fell back to the configured
        // limit because a provider max stake of 0 means "no provider limit".
        [Fact]
        public async Task GetMaxStake_BoostedBet_SendsTheOriginalPrice()
        {
            var payload = await MaxStakeAsync(CreateMaxStakeRequest(isBoosted: true));

            Assert.Equal(ORIGINAL_ODDS, LegPrice(payload));
            Assert.Equal("AcceptNone", payload.GetProperty("priceChangeRule").GetString());
        }

        [Fact]
        public async Task GetMaxStake_NormalBet_SendsItsOwnOdds()
        {
            var payload = await MaxStakeAsync(CreateMaxStakeRequest(isBoosted: false, decimalOdds: 1.9m, oddsRef: 1.5m));

            Assert.Equal(1.9m, LegPrice(payload));
        }

        // The Max Bet button builds its request without a reference price: it keeps the odds it was given.
        [Fact]
        public async Task GetMaxStake_BoostedRequestWithoutAReferencePrice_FallsBackToItsOdds()
        {
            var payload = await MaxStakeAsync(CreateMaxStakeRequest(isBoosted: true, oddsRef: 0m));

            Assert.Equal(BOOSTED_ODDS, LegPrice(payload));
        }
    }
}
