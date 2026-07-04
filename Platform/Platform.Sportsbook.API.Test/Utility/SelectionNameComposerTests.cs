using Platform.Sportsbook.API.Utility;
using Xunit;

namespace Platform.Sportsbook.API.Test.Utility
{
    // Template-id semantics (SeTeId "30"/"31" = Over/Under, "1"/"4" = sign-flip handicap sides) are
    // verified against bet-genius/BG.Receiver.Service/Repositories/OddsFeedRepository.cs and live
    // Mongo Selection documents -- not guessed. Real examples used below:
    //   SeTeId "30"/"31" -> SeNa "Over"/"Under" (multiple live Selection docs).
    //   MaId 1479518625876697091 "Match Handicap +23.50" (Li "23.50"): SeTeId "4" "Australia 7s Women"
    //   (favorite, sign-flipped) vs SeTeId "5" "Fiji 7s Women" (underdog, not sign-flipped).
    public class SelectionNameComposerTests
    {
        [Fact]
        public void Compose_NotHandicap_ReturnsNameUnchanged()
        {
            var result = SelectionNameComposer.Compose("44.50", "Over", "30", isHandicap: false);

            Assert.Equal("Over", result);
        }

        [Theory]
        [InlineData("30", "Over")]
        [InlineData("31", "Under")]
        public void Compose_OverUnder_AppendsLine(string selectionTemplateId, string selectionName)
        {
            var result = SelectionNameComposer.Compose("44.50", selectionName, selectionTemplateId, isHandicap: true);

            Assert.Equal($"{selectionName} 44.50", result);
        }

        [Fact]
        public void Compose_SignFlipTemplate_PositiveLine_FlipsToNegative()
        {
            // Real market 1479518625876697091 "Match Handicap +23.50": the SeTeId "4" side
            // ("Australia 7s Women") is the favorite and must show the flipped (negative) line.
            var result = SelectionNameComposer.Compose("23.50", "Australia 7s Women", "4", isHandicap: true);

            Assert.Equal("Australia 7s Women(-23.50)", result);
        }

        [Fact]
        public void Compose_NonSignFlipTemplate_PositiveLine_KeepsPositive()
        {
            // Same market, SeTeId "5" side ("Fiji 7s Women") is the underdog and keeps the "+" line.
            var result = SelectionNameComposer.Compose("23.50", "Fiji 7s Women", "5", isHandicap: true);

            Assert.Equal("Fiji 7s Women(+23.50)", result);
        }

        [Fact]
        public void Compose_SignFlipTemplate_NegativeLine_FlipsToPositive()
        {
            var result = SelectionNameComposer.Compose("-1.50", "Team A", "1", isHandicap: true);

            Assert.Equal("Team A(+1.50)", result);
        }

        [Fact]
        public void Compose_NonSignFlipTemplate_NegativeLine_KeepsNegative()
        {
            var result = SelectionNameComposer.Compose("-1.50", "Team B", "2", isHandicap: true);

            Assert.Equal("Team B(-1.50)", result);
        }

        [Fact]
        public void Compose_ZeroLine_AppendsZeroRegardlessOfTemplate()
        {
            var result = SelectionNameComposer.Compose("0", "Team A", "1", isHandicap: true);

            Assert.Equal("Team A(0)", result);
        }

        [Fact]
        public void Compose_MalformedLine_ReturnsNameUnchanged()
        {
            var result = SelectionNameComposer.Compose("not-a-number", "Team A", "1", isHandicap: true);

            Assert.Equal("Team A", result);
        }
    }
}
