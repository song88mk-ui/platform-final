using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Sportsbook.API.Repository;
using Platform.Sportsbook.API.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // BO-10: a boosted bet was refused with "approve any odds change" and handed back the original odds. The
    // slip sends the boosted price, GetBetFixtures2 priced the selection at its original odds (the boost was only
    // applied inside the IsOEEnable branch, which is hardcoded off), and a boosted bet is checked with
    // OddsChangeType.None. The selections of a boosted bet are now priced at the client's boosted odds; the
    // ClBoOd format ("CODE:odds|CODE:odds") is written by odds-engine-2 (OE.Sportsbook.Service PriceBoostMerger).
    public class BoostedOddsPricingTests
    {
        private static SelectionExtension Selection(long id, string clBoOd, decimal odds = 1.5m) => new()
        {
            SeId = id,
            Od = odds,
            OddsRef = odds,
            ClBoOd = clBoOd
        };

        // ---- TryGetClientBoostedOdds ----

        [Fact]
        public void TryGet_ClientWithAnEntry_ReturnsItsBoostedOdds()
        {
            Assert.True(BoostedOddsPricing.TryGetClientBoostedOdds("G01:1.64", "G01", out var odds));
            Assert.Equal(1.64m, odds);
        }

        [Fact]
        public void TryGet_SeveralClients_ReturnsTheOddsOfTheRightOne()
        {
            Assert.True(BoostedOddsPricing.TryGetClientBoostedOdds("G01:1.64|G02:1.70|G03:1.55", "G02", out var odds));
            Assert.Equal(1.70m, odds);
        }

        // The old match was `Contains(clientCode)`: a client whose code is part of another's read that client's boost.
        [Theory]
        [InlineData("G01:1.64", "G0")]
        [InlineData("XG02:1.64", "G02")]
        [InlineData("G022:1.64", "G02")]
        public void TryGet_ClientCodeThatOnlyAppearsInsideAnotherCode_HasNoBoost(string clientBoostedOdds, string clientCode)
        {
            Assert.False(BoostedOddsPricing.TryGetClientBoostedOdds(clientBoostedOdds, clientCode, out var odds));
            Assert.Equal(0m, odds);
        }

        [Fact]
        public void TryGet_WholeCodeAmongLongerOnes_ReturnsTheEntryOfThatClient()
        {
            Assert.True(BoostedOddsPricing.TryGetClientBoostedOdds("G022:1.80|G02:1.70|XG02:1.90", "G02", out var odds));
            Assert.Equal(1.70m, odds);
        }

        [Fact]
        public void TryGet_ClientCodeIgnoresCase()
        {
            Assert.True(BoostedOddsPricing.TryGetClientBoostedOdds("g01:1.64", "G01", out var odds));
            Assert.Equal(1.64m, odds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("G02:1.70")]
        public void TryGet_NoBoostForTheClient_ReturnsFalse(string clientBoostedOdds)
        {
            Assert.False(BoostedOddsPricing.TryGetClientBoostedOdds(clientBoostedOdds, "G01", out var odds));
            Assert.Equal(0m, odds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryGet_NoClientCode_ReturnsFalse(string clientCode)
        {
            Assert.False(BoostedOddsPricing.TryGetClientBoostedOdds("G01:1.64", clientCode, out _));
        }

        // The old code threw on these (Split(":")[1], Convert.ToDecimal) and failed the whole request.
        [Theory]
        [InlineData("G01")]
        [InlineData("G01:")]
        [InlineData("G01:abc")]
        [InlineData("G01:0")]
        [InlineData("G01:-1.5")]
        [InlineData(":1.64")]
        public void TryGet_MalformedOrNonPositiveEntry_IsNoBoostAndDoesNotThrow(string clientBoostedOdds)
        {
            Assert.False(BoostedOddsPricing.TryGetClientBoostedOdds(clientBoostedOdds, "G01", out var odds));
            Assert.Equal(0m, odds);
        }

        // odds-engine-2 writes the odds with the invariant culture; the reader must not depend on the server's.
        [Fact]
        public void TryGet_ReadsTheOddsWithTheInvariantCulture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                Assert.True(BoostedOddsPricing.TryGetClientBoostedOdds("G01:1.64", "G01", out var odds));
                Assert.Equal(1.64m, odds);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        // ---- GetClientBoostedOdds (the string contract the fixture lists have always used) ----

        [Fact]
        public void GetClientBoostedOdds_NotABoostedRequest_ReturnsTheOddsItWasGiven()
        {
            Assert.Equal("1.50", BoostedOddsPricing.GetClientBoostedOdds("G01:1.64", "G01", isBoosted: false, "1.50"));
        }

        [Fact]
        public void GetClientBoostedOdds_BoostedRequestWithoutABoostForTheClient_ReturnsTheOddsItWasGiven()
        {
            Assert.Equal("1.50", BoostedOddsPricing.GetClientBoostedOdds("G02:1.64", "G01", isBoosted: true, "1.50"));
            Assert.Equal("1.50", BoostedOddsPricing.GetClientBoostedOdds(null, "G01", isBoosted: true, "1.50"));
        }

        [Fact]
        public void GetClientBoostedOdds_BoostedRequestWithABoost_ReturnsTheBoostedOddsWithTwoDecimals()
        {
            Assert.Equal("1.64", BoostedOddsPricing.GetClientBoostedOdds("G01:1.64", "G01", isBoosted: true, "1.50"));
            Assert.Equal("1.60", BoostedOddsPricing.GetClientBoostedOdds("G01:1.6", "G01", isBoosted: true, "1.50"));
        }

        // ---- Apply ----

        [Fact]
        public void Apply_PricesTheBoostedSelectionAtTheClientsBoostedOdds_AndKeepsTheOriginalOddsAsReference()
        {
            var boosted = Selection(1, "G01:1.64");

            BoostedOddsPricing.Apply(new[] { boosted }, "G01");

            Assert.Equal(1.64m, boosted.Od);
            Assert.Equal(1.5m, boosted.OddsRef);
        }

        [Fact]
        public void Apply_LeavesASelectionTheClientHasNoBoostOnAtItsOddsSoTheStaleOddsCheckCatchesARemovedBoost()
        {
            var notBoosted = Selection(1, null);
            var boostedForAnotherClient = Selection(2, "G02:1.70");

            BoostedOddsPricing.Apply(new List<SelectionExtension> { notBoosted, boostedForAnotherClient }, "G01");

            Assert.Equal(1.5m, notBoosted.Od);
            Assert.Equal(1.5m, boostedForAnotherClient.Od);
        }

        [Fact]
        public void Apply_ASelectionBoostedForSeveralClients_TakesTheOddsOfTheRequestingOne()
        {
            var selection = Selection(1, "G01:1.64|G02:1.70");

            BoostedOddsPricing.Apply(new[] { selection }, "G02");

            Assert.Equal(1.70m, selection.Od);
        }

        // ---- Tripwire ----

        // ORIGINAL ODDS vs CLIENT ODDS. Boosts replace the ORIGINAL odds of a selection (BoostedOddsPricing), which
        // holds because IsOEEnable is hardcoded off: clients are priced with the original odds and no margin.
        // This test fails the day clients are priced with CLIENT odds (IsOEEnable can return true), on purpose:
        // see the comment in SportsbookRepository.GetBetFixtures2 and in BoostedOddsPricing before changing it.
        // Backoffice's boosted odds, the OE.Job.BoostedOddsCap comparison (it recomputes client odds from the
        // market's ClCoMa) and the stale-odds check of a boosted bet all have to be re-checked against client odds.
        // Then update this test. (It reads the private method by reflection: IsOEEnable is used in 100+ places.)
        [Theory]
        [InlineData("G01")]
        [InlineData("G02")]
        [InlineData("TTS")]
        [InlineData("BGC")]
        public void ClientOddsTripwire_ClientsArePricedWithTheOriginalOdds(string clientCode)
        {
            var method = typeof(SportsbookRepository).GetMethod("IsOEEnable", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.True(method != null,
                "BOOSTED ODDS TRIPWIRE: SportsbookRepository.IsOEEnable was renamed or removed. It decides whether clients are priced "
                + "with CLIENT odds (margin) or the ORIGINAL odds, and boosts (BoostedOddsPricing) assume the original odds. "
                + "Re-check how a boost is priced, then update this test.");

            var repository = RuntimeHelpers.GetUninitializedObject(typeof(SportsbookRepository));
            var usesClientOdds = (bool)method!.Invoke(repository, new object[] { clientCode })!;

            Assert.False(usesClientOdds,
                $"BOOSTED ODDS TRIPWIRE: client {clientCode} is now priced with CLIENT odds (IsOEEnable returns true) instead of the "
                + "ORIGINAL odds. Boosted prices replace the original odds today (BoostedOddsPricing.Apply in GetBetFixtures2). "
                + "Before this ships, decide what a boost means against a client price and re-check: the boosted odds set in Backoffice, "
                + "the OE.Job.BoostedOddsCap comparison (it recomputes client odds from ClCoMa), the stale-odds check of a boosted bet, "
                + "and the price Genius Sports is shown for a boosted bet (GTSRepository.GetProviderPrice sends OddsRef in the max stake, "
                + "assessment and bet feed calls: the ORIGINAL odds today, but the client's odds once IsOEEnable is on). "
                + "Then update this test.");
        }
    }
}
