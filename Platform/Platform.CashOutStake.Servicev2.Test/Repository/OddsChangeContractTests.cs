using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.CashOutStake.Servicev2.Repository;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;

namespace Platform.CashOutStake.Servicev2.Test.Repository
{
    // GM-902: odds-engine-2 publishes OnOddsChange on r.oe.sb.engine.onoddschange as
    // OnInternalOddsChangeMessage, which uses short property names (SpId, FiId, Ma, Se, ...).
    // CashOutStake deserialized it into a class that used the OE v1 names, and System.Text.Json is
    // case-sensitive and ignores unknown properties, so FixtureId came back 0 and Markets empty:
    // ProcessOddsChange matched no bet and no OnCashOutChange was ever published from an odds
    // change. These tests pin the wire names against a payload shaped like OE2's real one
    // (odds-engine-2 OE.Shared/Messages/OutgoingSportsbookMessages/OnInternalOddsChangeMessage.cs,
    // origin/main edd8a2f, serialized with default System.Text.Json options by OE.Shared
    // MessageBroker.Publish). The deserialize call below is the one Worker.cs makes for the
    // "OnOddsChange" message type: default options, no naming policy.
    public class OddsChangeContractTests
    {
        private static long _nextId = DateTime.UtcNow.Ticks;
        private static long NewId() => Interlocked.Increment(ref _nextId);

        // Every field OE2 puts on the wire for the fixture, its market and its selection,
        // including the ones CashOutStake does not read, so a green test also proves the unknown
        // properties are ignored. Distinct probability values catch two fields being wired to
        // each other's names.
        private static string Oe2OddsChangeJson(long fixtureId, long marketId, long selectionId, bool isSuspended = false, int fixtureTypeId = 0) => $$"""
            {
              "SpId": 1,
              "SpNa": "Soccer",
              "SegId": 10,
              "SegNa": "England",
              "LeId": 100,
              "LeNa": "Premier League",
              "FiId": {{fixtureId}},
              "PrFiId": "sr:match:55555",
              "FiNa": "Home v Away",
              "FiStDa": "2026-10-15T10:00:00Z",
              "FiStId": 2,
              "FiStNa": "Live",
              "Sc": "1-0",
              "FiSp": null,
              "IsOfLi": true,
              "HoReCa": 0,
              "AwReCa": 0,
              "CuPe": 1,
              "CuMi": 23,
              "FiTyId": {{fixtureTypeId}},
              "FiTyNa": "Match",
              "ToTi": 0,
              "Fa": "None",
              "SoOr": 1,
              "ClCo": null,
              "CoId": "corr-1",
              "IsSu": {{(isSuspended ? "true" : "false")}},
              "IsBo": null,
              "StUr": null,
              "Ma": [
                {
                  "PrFiId": "sr:match:55555",
                  "MaId": {{marketId}},
                  "MaTeId": "1",
                  "PrMaId": "sr:market:1",
                  "Li": null,
                  "IsHa": false,
                  "MaStId": 1,
                  "MaStNa": "Active",
                  "CaStId": 0,
                  "CaStNa": null,
                  "MaSp": null,
                  "MaNa": "1X2",
                  "SoOr": 1,
                  "ClCo": null,
                  "ClCoMa": null,
                  "InPl": true,
                  "MaTeTr": null,
                  "CompId": null,
                  "Se": [
                    {
                      "PrFiId": "sr:match:55555",
                      "MaTeId": "1",
                      "SeTeId": "1",
                      "MaSp": null,
                      "SeId": {{selectionId}},
                      "Od": 2.10,
                      "Pr": 0.41,
                      "WoPr": 0.31,
                      "LoPr": 0.11,
                      "VoPr": 0.07,
                      "HaWoPr": 0.09,
                      "HaLoPr": 0.05,
                      "IsAc": true,
                      "SeSp": null,
                      "SeNa": "Home",
                      "SoOr": 1,
                      "SeStId": 2000,
                      "ClOd": null,
                      "CompId": null,
                      "SeNaTr": null
                    }
                  ]
                }
              ],
              "HoId": "h1",
              "AwId": "a1",
              "CuSe": 0,
              "CuSer": 0,
              "TiSe": 0,
              "TiRe": 0,
              "SpTr": null,
              "SegTr": null,
              "LeTr": null,
              "HoTr": null,
              "AwTr": null
            }
            """;

        private static OnExternalOddsChangeMessage Deserialize(string payload) =>
            JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload)!;

        [Fact]
        public void Deserialize_Oe2Payload_ReadsTheFixtureFields()
        {
            var message = Deserialize(Oe2OddsChangeJson(fixtureId: 9876543210, marketId: 5, selectionId: 6, isSuspended: true));

            Assert.Equal(9876543210, message.FixtureId);
            Assert.Equal(1, message.SportId);
            Assert.Equal(new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc), message.FixtureStartDate.ToUniversalTime());
            Assert.Equal(2, message.FixtureStatusId);
            Assert.Equal(FixtureType.Match, message.FixtureType);
            Assert.True(message.IsSuspended);
        }

        [Fact]
        public void Deserialize_Oe2PayloadNotSuspended_ReadsIsSuspendedAsFalse()
        {
            var message = Deserialize(Oe2OddsChangeJson(fixtureId: 1, marketId: 5, selectionId: 6, isSuspended: false));

            Assert.False(message.IsSuspended);
        }

        [Fact]
        public void Deserialize_Oe2Payload_ReadsTheMarketAndSelectionFields()
        {
            var message = Deserialize(Oe2OddsChangeJson(fixtureId: 1, marketId: 5000000001, selectionId: 6000000002));

            var market = Assert.Single(message.Markets);
            Assert.Equal(5000000001, market.MarketId);
            Assert.Equal(MarketStatus.Active, market.MarketStatus);

            var selection = Assert.Single(market.Selections);
            Assert.Equal(6000000002, selection.SelectionId);
            Assert.True(selection.IsActive);
            Assert.Equal(0.41, selection.Probability);
            Assert.Equal(0.31, selection.WonProbability);
            Assert.Equal(0.11, selection.LostProbability);
            Assert.Equal(0.07, selection.VoidProbability);
            Assert.Equal(0.09, selection.HalfWonProbability);
            Assert.Equal(0.05, selection.HalfLostProbability);
        }

        [Fact]
        public void Deserialize_Oe2PayloadWithUnknownFixtureType_DoesNotThrow()
        {
            // OE2's FixtureType enum has Unknown = -1, which platform-final's FixtureType lacks.
            // System.Text.Json binds an undefined numeric value without throwing, and the
            // odds-change path never reads message.FixtureType, so the message is still processed.
            var message = Deserialize(Oe2OddsChangeJson(fixtureId: 77, marketId: 5, selectionId: 6, fixtureTypeId: -1));

            Assert.Equal(77, message.FixtureId);
            Assert.Equal((FixtureType)(-1), message.FixtureType);
        }

        [Theory]
        [InlineData(typeof(OnExternalOddsChangeMessage))]
        [InlineData(typeof(ExternalMarketMessage))]
        [InlineData(typeof(ExternalSelectionMessage))]
        public void EveryPublicProperty_HasAnExplicitWireName(Type messageType)
        {
            // A property added later without [JsonPropertyName] would silently bind to nothing
            // again, which is exactly how GM-902 went unnoticed. Force the choice to be explicit.
            var unmapped = messageType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<JsonPropertyNameAttribute>() == null)
                .Select(p => p.Name)
                .ToList();

            Assert.Empty(unmapped);
        }

        [Fact]
        public void ProcessOddsChange_Oe2PayloadForABetFixture_PublishesTheRecalculatedCashOutValue()
        {
            var fixtureId = NewId();
            var marketId = NewId();
            var selectionId = NewId();

            // Mongo supplies the first reading of the market and selection, as in production on a
            // cache miss; the next odds change is then applied from the OE2 payload.
            var repository = new MongoLookupResilienceTests.FakeLookupBettingRepository(new MemCache(), NullLogger<BettingRepository>.Instance,
                id => new CashOutMarketFilter { MaId = id, MaStId = (int)MarketStatus.Active },
                id => new CashOutSelectionFilter { SeId = id, MaId = marketId, MaStId = (int)MarketStatus.Active, SeStId = (int)BetStatus.Running, IsAc = true, Pr = 0.5, WoPr = 0.5, LoPr = 0.5 });

            var bet = ARunningBet(fixtureId, marketId, selectionId);
            repository._bets.Add(bet);
            repository._fixtureBetReference.Add(new Tuple<long, List<Guid>>(fixtureId, new List<Guid> { bet.BetId }));

            var broker = new MongoLookupResilienceTests.RecordingMessageBroker();

            repository.ProcessOddsChange(Deserialize(Oe2OddsChangeJson(fixtureId, marketId, selectionId)), broker);
            repository.ProcessOddsChange(Deserialize(Oe2OddsChangeJson(fixtureId, marketId, selectionId)), broker);

            Assert.Equal(2, broker.PublishedPayloads.Count);
            Assert.All(broker.PublishedPayloads, payload => Assert.Equal(bet.BetId.ToString().ToUpper(), Assert.IsType<OnCashOutChange>(payload).BetId));

            // Stake 100 at odds 2.0 on one selection: won 0.31 * 200 + void 0.07 * 100 +
            // half-won 0.09 * 150 + half-lost 0.05 * 50 = 85, less the 10% margin = 76.5. The
            // value moves with the pushed probabilities, which is what GM-902 stopped from happening.
            var afterOddsChange = Assert.IsType<OnCashOutChange>(broker.PublishedPayloads[1]);
            Assert.Equal(CashOutStatus.Available, afterOddsChange.CashOutStatus);
            Assert.Equal(76.5m, afterOddsChange.CashOutStake);
            Assert.NotEqual(afterOddsChange.CashOutStake, Assert.IsType<OnCashOutChange>(broker.PublishedPayloads[0]).CashOutStake);
        }

        [Fact]
        public void ProcessOddsChange_Oe2PayloadForASuspendedFixture_PublishesUnavailable()
        {
            var fixtureId = NewId();
            var marketId = NewId();
            var selectionId = NewId();

            var repository = new MongoLookupResilienceTests.FakeLookupBettingRepository(new MemCache(), NullLogger<BettingRepository>.Instance,
                id => new CashOutMarketFilter { MaId = id, MaStId = (int)MarketStatus.Active },
                id => new CashOutSelectionFilter { SeId = id, MaId = marketId, MaStId = (int)MarketStatus.Active, SeStId = (int)BetStatus.Running, IsAc = true, Pr = 0.5, WoPr = 0.5, LoPr = 0.5 });

            var bet = ARunningBet(fixtureId, marketId, selectionId);
            repository._bets.Add(bet);
            repository._fixtureBetReference.Add(new Tuple<long, List<Guid>>(fixtureId, new List<Guid> { bet.BetId }));

            var broker = new MongoLookupResilienceTests.RecordingMessageBroker();

            repository.ProcessOddsChange(Deserialize(Oe2OddsChangeJson(fixtureId, marketId, selectionId, isSuspended: true)), broker);

            var cashOutChange = Assert.IsType<OnCashOutChange>(Assert.Single(broker.PublishedPayloads));
            Assert.Equal(CashOutStatus.Unavailable, cashOutChange.CashOutStatus);
            Assert.Equal(0m, cashOutChange.CashOutStake);
        }

        [Fact]
        public void ProcessOddsChange_Oe2PayloadForAnotherFixture_PublishesNothing()
        {
            var repository = new MongoLookupResilienceTests.FakeLookupBettingRepository(new MemCache(), NullLogger<BettingRepository>.Instance,
                id => new CashOutMarketFilter { MaId = id, MaStId = (int)MarketStatus.Active },
                id => new CashOutSelectionFilter { SeId = id, IsAc = true, Pr = 0.5 });

            var betFixtureId = NewId();
            var bet = ARunningBet(betFixtureId, NewId(), NewId());
            repository._bets.Add(bet);
            repository._fixtureBetReference.Add(new Tuple<long, List<Guid>>(betFixtureId, new List<Guid> { bet.BetId }));

            var broker = new MongoLookupResilienceTests.RecordingMessageBroker();

            repository.ProcessOddsChange(Deserialize(Oe2OddsChangeJson(NewId(), NewId(), NewId())), broker);

            Assert.Empty(broker.PublishedPayloads);
        }

        // One running single at decimal odds 2.0 with a stake of 100, so the expected value in the
        // assertions above is a hand-checkable number.
        private static BetMemory ARunningBet(long fixtureId, long marketId, long selectionId)
        {
            var betId = Guid.NewGuid();

            var bet = new BetMemory
            {
                BetId = betId,
                ClientCode = "CODE",
                ClientId = Guid.NewGuid().ToString(),
                UserId = Guid.NewGuid().ToString(),
                TrueStake = 100m,
                Stake = 100m,
                Odds = 2.0m,
                OddsTypeId = (int)OddsType.Decimal
            };

            bet.BetSelections.Add(new BetSelectionMemory
            {
                BetId = betId,
                BetSelectionId = Guid.NewGuid(),
                FixtureId = fixtureId,
                MarketId = marketId,
                SelectionId = selectionId,
                BetSelectionStatusId = (int)BetStatus.Running,
                Probability = 0.5,
                Odds = 2.0m,
                DeadHeatFactor = 1.0m,
                IsBinary = true
            });

            return bet;
        }
    }
}
