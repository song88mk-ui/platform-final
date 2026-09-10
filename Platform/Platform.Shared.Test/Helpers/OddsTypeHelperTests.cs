using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using Xunit;

namespace Platform.Shared.Test.Helpers
{
    // Covers OddsTypeHelper.GetOddsByOddsType, which converts decimal odds into the display format
    // for Malaysian, Hong Kong and Indonesian odds types. The Malaysian and Indonesian branches
    // invert the odds (-1 / (odds - 1)) on opposite sides of 2.00 and truncate to 2 decimal places
    // rather than rounding, so a change in the comparison operator or the truncation would silently
    // shift the odds a player sees. These tests pin the boundary at 2.00 and the truncation
    // direction for each type.
    public class OddsTypeHelperTests
    {
        [Theory]
        [InlineData(OddsType.Decimal)]
        [InlineData(OddsType.Malaysian)]
        [InlineData(OddsType.HongKong)]
        [InlineData(OddsType.Indonesian)]
        public void GetOddsByOddsType_NotBinary_ReturnsOddsUnchanged(OddsType oddsType)
        {
            Assert.Equal(2.75m, OddsTypeHelper.GetOddsByOddsType((int)oddsType, 2.75m, isBinary: false));
        }

        [Fact]
        public void GetOddsByOddsType_DecimalType_ReturnsOddsUnchanged()
        {
            Assert.Equal(2.75m, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Decimal, 2.75m, isBinary: true));
        }

        [Fact]
        public void GetOddsByOddsType_UnknownType_ReturnsOddsUnchanged()
        {
            Assert.Equal(2.75m, OddsTypeHelper.GetOddsByOddsType(oddsTypeId: 99, 2.75m, isBinary: true));
        }

        [Theory]
        [InlineData(OddsType.Malaysian)]
        [InlineData(OddsType.HongKong)]
        [InlineData(OddsType.Indonesian)]
        public void GetOddsByOddsType_OddsOfOne_ReturnsZero(OddsType oddsType)
        {
            // Guards the -1 / (odds - 1) division by zero in the Malaysian and Indonesian branches.
            Assert.Equal(0m, OddsTypeHelper.GetOddsByOddsType((int)oddsType, 1m, isBinary: true));
        }

        [Fact]
        public void GetOddsByOddsType_HongKong_SubtractsOne()
        {
            Assert.Equal(1.5m, OddsTypeHelper.GetOddsByOddsType((int)OddsType.HongKong, 2.5m, isBinary: true));
            Assert.Equal(0.8m, OddsTypeHelper.GetOddsByOddsType((int)OddsType.HongKong, 1.8m, isBinary: true));
        }

        [Theory]
        [InlineData(1.8, 0.8)]   // below 2.00: odds - 1
        [InlineData(2.0, 1.0)]   // exactly 2.00 is inclusive on the subtract side
        public void GetOddsByOddsType_MalaysianAtOrBelowTwo_SubtractsOne(double odds, double expected)
        {
            Assert.Equal((decimal)expected, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Malaysian, (decimal)odds, isBinary: true));
        }

        [Theory]
        [InlineData(3.0, -0.5)]    // -1 / 2.0 = -0.5
        [InlineData(2.5, -0.66)]   // -1 / 1.5 = -0.6666..., truncated toward zero, not rounded to -0.67
        public void GetOddsByOddsType_MalaysianAboveTwo_InvertsAndTruncates(double odds, double expected)
        {
            Assert.Equal((decimal)expected, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Malaysian, (decimal)odds, isBinary: true));
        }

        [Theory]
        [InlineData(2.5, 1.5)]   // at or above 2.00: odds - 1
        [InlineData(2.0, 1.0)]   // exactly 2.00 is inclusive on the subtract side
        public void GetOddsByOddsType_IndonesianAtOrAboveTwo_SubtractsOne(double odds, double expected)
        {
            Assert.Equal((decimal)expected, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Indonesian, (decimal)odds, isBinary: true));
        }

        [Theory]
        [InlineData(1.5, -2.0)]    // -1 / 0.5 = -2
        [InlineData(1.8, -1.25)]   // -1 / 0.8 = -1.25
        [InlineData(1.3, -3.33)]   // -1 / 0.3 = -3.3333..., truncated toward zero, not rounded to -3.34
        public void GetOddsByOddsType_IndonesianBelowTwo_InvertsAndTruncates(double odds, double expected)
        {
            Assert.Equal((decimal)expected, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Indonesian, (decimal)odds, isBinary: true));
        }

        [Fact]
        public void GetOddsByOddsType_MalaysianAndIndonesian_InvertOnOppositeSidesOfTwo()
        {
            // The two types are mirror images: the same odds value is subtractive for one and
            // inverted for the other. This is the property most at risk from an operator flip.
            const decimal below = 1.5m;
            const decimal above = 3.0m;

            Assert.Equal(0.5m, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Malaysian, below, isBinary: true));
            Assert.Equal(-2.0m, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Indonesian, below, isBinary: true));

            Assert.Equal(-0.5m, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Malaysian, above, isBinary: true));
            Assert.Equal(2.0m, OddsTypeHelper.GetOddsByOddsType((int)OddsType.Indonesian, above, isBinary: true));
        }
    }
}
